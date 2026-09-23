using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.Web.WebView2.Core;
using Windows.Foundation;
using Microsoft.UI.Xaml.Automation;
using UnboundOS.App.Services;
using UnboundOS.Core.Abstractions;
using UnboundOS.Core.Models;
using UnboundOS.Core.Navigation;

namespace UnboundOS.App.Views;

public sealed partial class NavigationCubeView : UserControl
{
    private IUiMotionPolicy? _motion;
    private CubePose _pose = CubePose.Home;
    private CubeDestination _announced = CubeDestination.Session;
    private CubeDestination? _pendingOpen;
    private bool _sceneReady;
    private bool _wired;
    private bool _opening;
    private bool _browsing;
    private int _openToken;
    private int _focus;
    private IReadOnlyList<LibraryGame> _games = [];
    private IReadOnlyList<DesktopTool> _tools = [];
    private IReadOnlyList<ModGame> _mods = [];
    private IReadOnlyList<CubeBrowseItem> _items = [];

    public NavigationCubeView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
        GotFocus += (_, _) => FocusRing.Opacity = 1;
        LostFocus += (_, _) => FocusRing.Opacity = 0;
    }

    public CubePose Pose => _pose;

    public event EventHandler<CubeDestination>? FaceActivated;

    public event EventHandler<CubeDestination>? FrontChanged;

    public event EventHandler<string>? Notice;

    public event EventHandler? SettingsRequested;

    /// <summary>Last Options row id, set before <see cref="SettingsRequested"/>.</summary>
    public string? OptionsGroupId { get; private set; }

    /// <summary>True while a category list is open over the galaxy.</summary>
    public bool OverlayOpen => _browsing;

    public event EventHandler<bool>? OverlayChanged;

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (_wired)
        {
            return;
        }

        _wired = true;
        _motion = TryMotion();
        if (_motion is not null)
        {
            _motion.Changed += OnMotionChanged;
        }

        Announce(_pose.Front);
        await LoadCatalogsAsync();
        await StartSceneAsync();
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        if (_motion is not null)
        {
            _motion.Changed -= OnMotionChanged;
            _motion = null;
        }

        if (CubeWeb.CoreWebView2 is not null)
        {
            CubeWeb.CoreWebView2.WebMessageReceived -= OnWebMessage;
        }

        _sceneReady = false;
        _wired = false;
        _opening = false;
        SetBrowsing(false);
        _pendingOpen = null;
    }

    private async Task StartSceneAsync()
    {
        try
        {
            CubeWeb.DefaultBackgroundColor = Windows.UI.Color.FromArgb(255, 0, 0, 0);
            await CubeWeb.EnsureCoreWebView2Async();
            var core = CubeWeb.CoreWebView2;
            if (core is null)
            {
                ShowFallback();
                return;
            }

            core.Settings.AreDefaultContextMenusEnabled = false;
            core.Settings.AreDevToolsEnabled = false;
            core.Settings.IsStatusBarEnabled = false;
            core.Settings.IsZoomControlEnabled = false;
            core.Settings.IsSwipeNavigationEnabled = false;
            core.Settings.AreBrowserAcceleratorKeysEnabled = false;
            core.Settings.IsWebMessageEnabled = true;

            var assets = Path.Combine(AppContext.BaseDirectory, CubeBridge.AssetFolder);
            if (!Directory.Exists(Path.Combine(assets, "Cube")))
            {
                ShowFallback();
                return;
            }

            core.SetVirtualHostNameToFolderMapping(
                CubeBridge.VirtualHost,
                assets,
                CoreWebView2HostResourceAccessKind.Allow);
            core.WebMessageReceived += OnWebMessage;
            core.Navigate(CubeBridge.IndexUrl);
        }
        catch (Exception)
        {
            ShowFallback();
        }
    }

    private void ShowFallback()
    {
        CubeWeb.Visibility = Visibility.Collapsed;
        FallbackCard.Visibility = Visibility.Visible;
        SyncFallback();
    }

    private void OnMotionChanged(object? sender, EventArgs e) =>
        _ = DispatcherQueue.TryEnqueue(() => PushState(burst: false));

    private void OnWebMessage(CoreWebView2 sender, CoreWebView2WebMessageReceivedEventArgs args)
    {
        var json = args.TryGetWebMessageAsString();
        if (!CubeBridge.TryRead(json, out var message))
        {
            return;
        }

        _ = DispatcherQueue.TryEnqueue(() => HandleHostMessage(message));
    }

    private void HandleHostMessage(CubeHostMessage message)
    {
        switch (message.Type.ToLowerInvariant())
        {
            case "ready":
                _sceneReady = true;
                PushState(burst: false);
                break;
            case "activate":
                if (_browsing)
                {
                    ActivateFocused();
                    break;
                }

                ActivateFront();
                break;
            case "opened":
                break;
            case "cycle":
                if (_browsing)
                {
                    Cycle(message.Index, fromScene: true);
                }

                break;
            case "select":
                if (_browsing)
                {
                    if (message.Index >= 0)
                    {
                        _focus = CubeBrowse.Wrap(message.Index, _items.Count);
                    }

                    ActivateFocused();
                }

                break;
            case "back":
                CloseOverlay();
                break;
            case "turn":
                if (_opening || _browsing)
                {
                    break;
                }

                if (CubeBridge.ParseTurn(message.Turn) is { } turn)
                {
                    ApplyIdleTurn(turn);
                }

                break;
            case "pick":
                if (_opening || _browsing)
                {
                    break;
                }

                if (CubeBridge.ParseFace(message.Face) is { } face)
                {
                    if (face == _pose.Front)
                    {
                        ActivateFront();
                    }
                    else
                    {
                        SetPose(CubeAtmosphere.AimedAt(face, _pose));
                    }
                }

                break;
            case "dragend":
                if (_opening || _browsing)
                {
                    break;
                }

                FinishDrag(message.Dx, message.Dy, message.Vx, message.Vy);
                break;
        }
    }

    private void OnKeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (_opening)
        {
            e.Handled = true;
            return;
        }

        var name = e.Key.ToString();
        if (_browsing)
        {
            if (CubeInput.IsBackKey(name) || e.Key is Windows.System.VirtualKey.Escape)
            {
                CloseOverlay();
                e.Handled = true;
                return;
            }

            if (CubeInput.IsActivateKey(name) || e.Key is Windows.System.VirtualKey.Enter or Windows.System.VirtualKey.Space)
            {
                ActivateFocused();
                e.Handled = true;
                return;
            }

            var browseTurn = CubeInput.FromKey(name);
            if (browseTurn is CubeTurn.Up)
            {
                Cycle(_focus - 1, fromScene: false);
                e.Handled = true;
                return;
            }

            if (browseTurn is CubeTurn.Down)
            {
                Cycle(_focus + 1, fromScene: false);
                e.Handled = true;
            }

            return;
        }

        if (CubeInput.IsActivateKey(name) || e.Key is Windows.System.VirtualKey.Enter or Windows.System.VirtualKey.Space)
        {
            ActivateFront();
            e.Handled = true;
            return;
        }

        var turn = CubeInput.FromKey(name);
        if (turn is null)
        {
            return;
        }

        ApplyIdleTurn(turn.Value);
        e.Handled = true;
    }

    private void OnFallbackPressed(object sender, PointerRoutedEventArgs e)
    {
        Focus(FocusState.Pointer);
        ActivateFront();
        e.Handled = true;
    }

    public void Rotate(CubeTurn turn) => ApplyIdleTurn(turn);

    public void ApplyIdleTurn(CubeTurn turn)
    {
        if (_opening || _browsing)
        {
            return;
        }

        switch (HomeGalaxy.FromTurn(turn))
        {
            case HomeIdleAction.PanLeft:
                PanNode(-1);
                break;
            case HomeIdleAction.PanRight:
                PanNode(1);
                break;
            case HomeIdleAction.OpenListFromBottom:
                OpenCategoryList(fromBottom: true);
                break;
            case HomeIdleAction.OpenListFromTop:
                OpenCategoryList(fromBottom: false);
                break;
        }
    }

    public void PanNode(int delta)
    {
        if (_opening || _browsing)
        {
            return;
        }

        var next = HomeGalaxy.Neighbor(_pose.Front, delta);
        SetPose(CubeAtmosphere.AimedAt(next, _pose));
    }

    public void OpenCategoryList(bool fromBottom)
    {
        if (_opening || _browsing)
        {
            return;
        }

        var destination = _pose.Front;
        _items = ItemsFor(destination);
        if (_items.Count == 0)
        {
            FaceActivated?.Invoke(this, destination);
            return;
        }

        _focus = HomeGalaxy.ListStartIndex(_items.Count, fromBottom);
        if (!_sceneReady || CubeWeb.CoreWebView2 is null)
        {
            FaceActivated?.Invoke(this, destination);
            return;
        }

        var origin = fromBottom ? "bottom" : "top";
        CubeWeb.CoreWebView2.PostWebMessageAsJson(
            CubeBridge.ToJson(CubeBridge.Open(destination, AllowMotion, _items, _focus, origin)));
        EnterBrowse(destination);
    }

    public void ActivateFront() => OpenCategoryList(fromBottom: false);

    public void OpenOptionsList(bool fromBottom = false)
    {
        if (_opening)
        {
            return;
        }

        if (_browsing)
        {
            ResetScene();
        }

        OptionsGroupId = null;
        _items = CubeBrowse.Options();
        _focus = HomeGalaxy.ListStartIndex(_items.Count, fromBottom);
        if (!_sceneReady || CubeWeb.CoreWebView2 is null)
        {
            SettingsRequested?.Invoke(this, EventArgs.Empty);
            return;
        }

        var origin = fromBottom ? "bottom" : "top";
        CubeWeb.CoreWebView2.PostWebMessageAsJson(
            CubeBridge.ToJson(CubeBridge.OpenOptions(AllowMotion, _items, _focus, origin)));
        EnterBrowse(_pose.Front);
    }

    public void CloseOverlay()
    {
        _opening = false;
        SetBrowsing(false);
        _pendingOpen = null;
        _items = [];
        _focus = 0;
        _openToken++;
        if (_sceneReady && CubeWeb.CoreWebView2 is not null)
        {
            CubeWeb.CoreWebView2.PostWebMessageAsJson(CubeBridge.ToJson(CubeBridge.Close(AllowMotion)));
        }

        Announce(_pose.Front);
    }

    public void ResetScene()
    {
        _opening = false;
        SetBrowsing(false);
        _pendingOpen = null;
        _items = [];
        _focus = 0;
        _openToken++;
        if (_sceneReady && CubeWeb.CoreWebView2 is not null)
        {
            CubeWeb.CoreWebView2.PostWebMessageAsJson(CubeBridge.ToJson(CubeBridge.Reset(_pose.Front, AllowMotion)));
            PushState(burst: false);
        }

        Announce(_pose.Front);
    }

    private void SetBrowsing(bool value)
    {
        if (_browsing == value)
        {
            return;
        }

        _browsing = value;
        OverlayChanged?.Invoke(this, value);
    }

    private async Task CompleteActivationAfterAsync(int token)
    {
        await Task.Delay(CubeAtmosphere.OpenDurationMs + 220);
        if (token != _openToken)
        {
            return;
        }

        _ = DispatcherQueue.TryEnqueue(CompleteActivation);
    }

    private void CompleteActivation()
    {
        if (_pendingOpen is not { } destination)
        {
            return;
        }

        _pendingOpen = null;
        _opening = false;
        if (CubeCatalog.StaysInCube(destination) && _items.Count > 0)
        {
            EnterBrowse(destination);
            return;
        }

        FaceActivated?.Invoke(this, destination);
    }

    private void EnterBrowse(CubeDestination destination)
    {
        _opening = false;
        SetBrowsing(true);
        _pendingOpen = null;
        if (_items.Count == 0)
        {
            FaceActivated?.Invoke(this, destination);
            SetBrowsing(false);
            return;
        }

        _focus = CubeBrowse.Wrap(_focus, _items.Count);
        AnnounceItem();
        FocusScene();
    }

    private void Cycle(int index, bool fromScene)
    {
        if (_items.Count == 0)
        {
            return;
        }

        _focus = CubeBrowse.Wrap(index, _items.Count);
        AnnounceItem();
        if (!fromScene)
        {
            FocusScene();
        }
    }

    private void FocusScene()
    {
        if (_sceneReady && CubeWeb.CoreWebView2 is not null)
        {
            CubeWeb.CoreWebView2.PostWebMessageAsJson(CubeBridge.ToJson(CubeBridge.Focus(_focus)));
        }
    }

    private async void ActivateFocused()
    {
        if (_items.Count == 0)
        {
            return;
        }

        var item = _items[CubeBrowse.Wrap(_focus, _items.Count)];
        switch (item.Kind)
        {
            case "game":
                await LaunchGameAsync(item.Id);
                break;
            case "tool":
                await LaunchToolAsync(item.Id);
                break;
            case "mod":
                ResetScene();
                FaceActivated?.Invoke(this, CubeDestination.Mods);
                break;
            case "settings":
                OptionsGroupId = item.Id;
                ResetScene();
                SettingsRequested?.Invoke(this, EventArgs.Empty);
                break;
            case "page":
                ResetScene();
                FaceActivated?.Invoke(this, _pose.Front);
                break;
            default:
                ResetScene();
                FaceActivated?.Invoke(this, _pose.Front);
                break;
        }
    }

    private async Task LaunchGameAsync(string id)
    {
        var game = _games.FirstOrDefault(candidate =>
            string.Equals(candidate.Id, id, StringComparison.OrdinalIgnoreCase));
        var launcher = TryGet<ILibraryLaunchService>();
        var profiles = TryGet<IProfileStore>();
        if (game is null || launcher is null || profiles is null)
        {
            ResetScene();
            FaceActivated?.Invoke(this, CubeDestination.Session);
            return;
        }

        var profile = (await profiles.LoadAsync()).FirstOrDefault();
        if (profile is null)
        {
            Notice?.Invoke(this, "No session profile. Open Profiles first.");
            return;
        }

        var result = await launcher.LaunchAsync(game, profile);
        Notice?.Invoke(this, result.Message);
        if (!result.Succeeded)
        {
            ResetScene();
            FaceActivated?.Invoke(this, CubeDestination.Session);
        }
    }

    private async Task LaunchToolAsync(string id)
    {
        var tool = _tools.FirstOrDefault(candidate =>
            string.Equals(candidate.Id, id, StringComparison.OrdinalIgnoreCase));
        var launcher = TryGet<IDesktopToolLauncher>();
        if (tool is null || launcher is null)
        {
            ResetScene();
            FaceActivated?.Invoke(this, CubeDestination.Tools);
            return;
        }

        var result = tool.IsInstalled
            ? await launcher.LaunchAsync(tool)
            : await launcher.OpenGetPathAsync(tool);
        Notice?.Invoke(this, result.Message);
    }

    private async Task LoadCatalogsAsync()
    {
        try
        {
            var games = TryGet<IGameLibraryCatalog>();
            if (games is not null)
            {
                _games = await games.DiscoverAsync();
            }

            var tools = TryGet<IDesktopToolCatalog>();
            if (tools is not null)
            {
                _tools = await tools.DiscoverAsync();
            }

            var mods = TryGet<IModCatalogService>();
            if (mods is not null)
            {
                _mods = await mods.DiscoverAsync();
            }
        }
        catch (Exception)
        {
            // Discovery is best-effort. Stubs from CubeBrowse still populate Games.
        }
    }

    private IReadOnlyList<CubeBrowseItem> ItemsFor(CubeDestination destination) =>
        CubeBrowse.ForNode(destination, _games, _tools, _mods);

    private void FinishDrag(float dx, float dy, float vx, float vy)
    {
        var flick = CubeInput.FlickTurn(vx, vy);
        if (flick is not null)
        {
            ApplyIdleTurn(flick.Value);
            return;
        }

        if (Math.Abs(dx) >= Math.Abs(dy) && Math.Abs(dx) > 48)
        {
            ApplyIdleTurn(dx < 0 ? CubeTurn.Right : CubeTurn.Left);
            return;
        }

        if (Math.Abs(dy) > 48)
        {
            ApplyIdleTurn(dy > 0 ? CubeTurn.Down : CubeTurn.Up);
        }
    }

    private void SetPose(CubePose pose)
    {
        var changed = pose.YawSteps != _pose.YawSteps || pose.PitchSteps != _pose.PitchSteps;
        _pose = pose;
        Announce(pose.Front);
        PushState(burst: changed && AllowMotion);
        SyncFallback();
    }

    private void PushState(bool burst)
    {
        if (!_sceneReady || CubeWeb.CoreWebView2 is null || _browsing || _opening)
        {
            return;
        }

        var json = CubeBridge.ToJson(
            CubeBridge.State(_pose, _pose.YawDegrees, _pose.PitchDegrees, AllowMotion, burst));
        CubeWeb.CoreWebView2.PostWebMessageAsJson(json);
    }

    private void Announce(CubeDestination front)
    {
        var info = CubeCatalog.Info(front);
        FrontCaption.Text = info.Title;
        AutomationProperties.SetName(this, CubeCatalog.Announce(front));
        AutomationProperties.SetName(FrontCaption, $"Focused node {info.Title}");
        if (_announced != front)
        {
            _announced = front;
            FrontChanged?.Invoke(this, front);
        }
    }

    private void AnnounceItem()
    {
        if (_items.Count == 0)
        {
            return;
        }

        var item = _items[CubeBrowse.Wrap(_focus, _items.Count)];
        FrontCaption.Text = item.Title;
        AutomationProperties.SetName(this, CubeCatalog.AnnounceItem(item, _focus, _items.Count));
        Notice?.Invoke(this, $"{item.Title} · {item.Meta}");
    }

    private void SyncFallback()
    {
        if (FallbackCard.Visibility != Visibility.Visible)
        {
            return;
        }

        var info = CubeCatalog.Info(_pose.Front);
        FallbackKicker.Text = info.Kicker;
        FallbackMonogram.Text = info.Monogram;
        FallbackTitle.Text = info.Title;
    }

    private bool AllowMotion => _motion?.AllowMotion ?? true;

    private static IUiMotionPolicy? TryMotion() => TryGet<IUiMotionPolicy>();

    private static T? TryGet<T>() where T : class
    {
        try
        {
            return AppServices.Get<T>();
        }
        catch (Exception)
        {
            return null;
        }
    }
}
