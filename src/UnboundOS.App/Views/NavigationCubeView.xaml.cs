using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.Web.WebView2.Core;
using Windows.Foundation;
using Microsoft.UI.Xaml.Automation;
using UnboundOS.App.Services;
using UnboundOS.Core.Abstractions;
using UnboundOS.Core.Navigation;

namespace UnboundOS.App.Views;

public sealed partial class NavigationCubeView : UserControl
{
    private IUiMotionPolicy? _motion;
    private CubePose _pose = CubePose.Home;
    private CubeDestination _announced = CubeDestination.Session;
    private bool _sceneReady;
    private bool _wired;

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
    }

    private async Task StartSceneAsync()
    {
        try
        {
            CubeWeb.DefaultBackgroundColor = Windows.UI.Color.FromArgb(255, 5, 7, 10);
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
                ActivateFront();
                break;
            case "turn":
                if (CubeBridge.ParseTurn(message.Turn) is { } turn)
                {
                    Rotate(turn);
                }

                break;
            case "pick":
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
                FinishDrag(message.Dx, message.Dy, message.Vx, message.Vy);
                break;
        }
    }

    private void OnKeyDown(object sender, KeyRoutedEventArgs e)
    {
        var name = e.Key.ToString();
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

        Rotate(turn.Value);
        e.Handled = true;
    }

    private void OnFallbackPressed(object sender, PointerRoutedEventArgs e)
    {
        Focus(FocusState.Pointer);
        ActivateFront();
        e.Handled = true;
    }

    public void Rotate(CubeTurn turn) => SetPose(_pose.Turn(turn));

    public void ActivateFront() => FaceActivated?.Invoke(this, _pose.Front);

    private void FinishDrag(float dx, float dy, float vx, float vy)
    {
        var preview = CubeInput.PreviewDrag(_pose, dx, dy);
        var snapped = CubeInput.SnapFromDegrees(preview.Yaw, preview.Pitch);
        var flick = CubeInput.FlickTurn(vx, vy);
        SetPose(flick is null ? snapped : snapped.Turn(flick.Value));
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
        if (!_sceneReady || CubeWeb.CoreWebView2 is null)
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
        FrontHint.Text = info.Hint;
        AutomationProperties.SetName(this, CubeCatalog.Announce(front));
        AutomationProperties.SetName(FrontCaption, $"Front face {info.Title}");
        if (_announced != front)
        {
            _announced = front;
            FrontChanged?.Invoke(this, front);
        }
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

    private static IUiMotionPolicy? TryMotion()
    {
        try
        {
            return AppServices.Get<IUiMotionPolicy>();
        }
        catch (Exception)
        {
            return null;
        }
    }
}
