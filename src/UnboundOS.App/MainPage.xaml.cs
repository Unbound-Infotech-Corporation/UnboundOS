using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using UnboundOS.App.Services;
using UnboundOS.App.ViewModels;
using UnboundOS.App.Views;
using UnboundOS.Core.Abstractions;
using UnboundOS.Core.Home;
using UnboundOS.Core.Navigation;

namespace UnboundOS.App;

public sealed partial class MainPage : Page
{
    public ShellViewModel ViewModel { get; } = AppServices.Get<ShellViewModel>();
    public HomeHudViewModel Hud { get; } = AppServices.Get<HomeHudViewModel>();

    private FrameworkElement? _dragWidget;
    private double _dragStartX;
    private double _dragStartY;
    private double _widgetStartLeft;
    private double _widgetStartTop;

    public MainPage()
    {
        InitializeComponent();
        OverlayNav.Visibility = ViewModel.OverlayNavVisible
            ? Visibility.Visible
            : Visibility.Collapsed;
        Loaded += OnLoaded;
        HomeCube.OverlayChanged += OnHomeOverlayChanged;
        Hud.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName is nameof(Hud.AppearanceToken) or nameof(Hud.LayoutRevision) or null)
            {
                ApplyWidgetLook();
                ApplyWidgetPositions();
            }
        };
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        await ViewModel.InitializeAsync();
        await Hud.InitializeAsync();
        ApplyWidgetLook();
        ApplyWidgetPositions();
        ApplyWidgetListDim(HomeCube.OverlayOpen);
        ApplyHomeChrome(home: true);
        ApplyNavState("Home");
        HomeCube.Focus(FocusState.Programmatic);
    }

    private void Nav_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: string tag })
        {
            return;
        }

        Navigate(tag);
    }

    private void OnCubeActivated(object sender, CubeDestination destination) =>
        Navigate(CubeCatalog.NavTag(destination));

    private void OnHomeSettingsClick(object sender, RoutedEventArgs e)
    {
        ViewModel.SelectedNav = "Home";
        ViewModel.StatusLine = "Options. Escape returns to Home.";
        ApplyNavState("Home");
        ApplyHomeChrome(home: true);
        HomeCube.OpenOptionsList();
        HomeCube.Focus(FocusState.Programmatic);
    }

    private void OnGalaxySettingsRequested(object sender, EventArgs e)
    {
        var id = HomeCube.OptionsGroupId;
        Navigate("Settings");
        if (!string.IsNullOrWhiteSpace(id))
        {
            AppServices.Get<SettingsViewModel>().SelectGroup(id);
        }
    }

    private void OnCubeNotice(object sender, string message) =>
        ViewModel.StatusLine = message;

    private void OnCubeFrontChanged(object sender, CubeDestination destination)
    {
        var info = CubeCatalog.Info(destination);
        ViewModel.StatusLine = $"{info.Title} node. Up opens this list from the bottom. Down opens this list from the top.";
    }

    private void Navigate(string tag)
    {
        ViewModel.SelectedNav = tag;
        ViewModel.StatusLine = tag switch
        {
            "Home" => "Home. Left/right: tabs. Up/Down: this tab's list (bottom/top).",
            "Session" => "Session engine ready.",
            "Network" => "Network director ready.",
            "Tools" => "Tools marketplace ready.",
            "Files" => "Daily folders. Windows Explorer stays for game launchers.",
            "Hardware" => "CPU, GPU, disks, RAM from this PC. Sensors wait on the image.",
            "Mods" => "Workshop catalog and mod profiles ready.",
            "Profiles" => "Profile bay open.",
            "Settings" => "Display, overclocking launch, startup audit, Home widgets, motion.",
            "Overlay" => "Rainmeter overlay. Open the host or Get Phenix.",
            _ => ViewModel.StatusLine
        };

        ApplyNavState(tag);
        ApplyHomeChrome(home: tag == "Home");

        if (tag == "Home")
        {
            HomeCube.ResetScene();
            HomeCube.Focus(FocusState.Programmatic);
            return;
        }

        var pageType = tag switch
        {
            "Session" => typeof(SessionPage),
            "Network" => typeof(NetworkPage),
            "Tools" => typeof(ToolsPage),
            "Files" => typeof(FilesPage),
            "Hardware" => typeof(HardwarePage),
            "Mods" => typeof(ModsPage),
            "Profiles" => typeof(ProfilesPage),
            "Settings" => typeof(SettingsPage),
            "Overlay" => typeof(OverlayPage),
            _ => typeof(SessionPage)
        };

        ContentFrame.Navigate(pageType);
    }

    private void ApplyHomeChrome(bool home)
    {
        ChromeBar.Visibility = home ? Visibility.Collapsed : Visibility.Visible;
        HeroTicks.Visibility = home ? Visibility.Collapsed : Visibility.Visible;
        HeroScan.Visibility = home ? Visibility.Collapsed : Visibility.Visible;
        HeroGrid.Visibility = home ? Visibility.Collapsed : Visibility.Visible;
        HomeView.Visibility = home ? Visibility.Visible : Visibility.Collapsed;
        ContentFrame.Visibility = home ? Visibility.Collapsed : Visibility.Visible;
    }

    private void ApplyNavState(string tag)
    {
        var motion = AppServices.Get<IUiMotionPolicy>();
        VisualStateManager.GoToState(this, tag, useTransitions: motion.AllowMotion);
    }

    private void OnHomeWidgetLayerSizeChanged(object sender, SizeChangedEventArgs e) =>
        ApplyWidgetPositions();

    private void OnWidgetPointerPressed(object sender, PointerRoutedEventArgs e)
    {
        if (sender is not FrameworkElement el)
        {
            return;
        }

        _dragWidget = el;
        var point = e.GetCurrentPoint(HomeWidgetLayer).Position;
        _dragStartX = point.X;
        _dragStartY = point.Y;
        _widgetStartLeft = Canvas.GetLeft(el);
        _widgetStartTop = Canvas.GetTop(el);
        if (double.IsNaN(_widgetStartLeft))
        {
            _widgetStartLeft = 0;
        }

        if (double.IsNaN(_widgetStartTop))
        {
            _widgetStartTop = 0;
        }

        el.CapturePointer(e.Pointer);
        e.Handled = true;
    }

    private void OnWidgetPointerMoved(object sender, PointerRoutedEventArgs e)
    {
        if (_dragWidget is null || sender is not FrameworkElement el)
        {
            return;
        }

        var point = e.GetCurrentPoint(HomeWidgetLayer).Position;
        var maxX = Math.Max(0, HomeWidgetLayer.ActualWidth - el.ActualWidth);
        var maxY = Math.Max(0, HomeWidgetLayer.ActualHeight - el.ActualHeight);
        Canvas.SetLeft(el, Math.Clamp(_widgetStartLeft + point.X - _dragStartX, 0, maxX));
        Canvas.SetTop(el, Math.Clamp(_widgetStartTop + point.Y - _dragStartY, 0, maxY));
        e.Handled = true;
    }

    private async void OnWidgetPointerReleased(object sender, PointerRoutedEventArgs e)
    {
        if (_dragWidget is not FrameworkElement el || el.Tag is not string id)
        {
            _dragWidget = null;
            return;
        }

        el.ReleasePointerCapture(e.Pointer);
        _dragWidget = null;
        var w = Math.Max(1, HomeWidgetLayer.ActualWidth - el.ActualWidth);
        var h = Math.Max(1, HomeWidgetLayer.ActualHeight - el.ActualHeight);
        var x = Math.Clamp(Canvas.GetLeft(el) / w, 0, 1);
        var y = Math.Clamp(Canvas.GetTop(el) / h, 0, 1);
        await Hud.MoveAsync(id, x, y);
        e.Handled = true;
    }

    private void ApplyWidgetPositions()
    {
        if (HomeWidgetLayer.ActualWidth < 8 || HomeWidgetLayer.ActualHeight < 8)
        {
            return;
        }

        Place(HomeCpuWidget, HomeWidgets.Cpu);
        Place(HomeGpuWidget, HomeWidgets.Gpu);
        Place(HomePackageWidget, HomeWidgets.Package);
        Place(HomeClockWidget, HomeWidgets.Clock);
        Place(HomeLoadWidget, HomeWidgets.Load);
    }

    private void OnHomeOverlayChanged(object? sender, bool open) =>
        ApplyWidgetListDim(open);

    private void ApplyWidgetListDim(bool listOpen) =>
        HomeWidgetLayer.Opacity = listOpen ? 0.42 : 1;

    private void Place(FrameworkElement el, string id)
    {
        if (_dragWidget == el)
        {
            return;
        }

        var placement = Hud.Placement(id);
        var maxX = Math.Max(0, HomeWidgetLayer.ActualWidth - Math.Max(el.ActualWidth, 8));
        var maxY = Math.Max(0, HomeWidgetLayer.ActualHeight - Math.Max(el.ActualHeight, 8));
        Canvas.SetLeft(el, placement.X * maxX);
        Canvas.SetTop(el, placement.Y * maxY);
    }

    private void ApplyWidgetLook()
    {
        var key = Hud.Appearance switch
        {
            HomeWidgetAppearance.Dim => "HomeWidgetDimPlaqueStyle",
            HomeWidgetAppearance.Compact => "HomeWidgetCompactPlaqueStyle",
            _ => "HomeWidgetPlaqueStyle"
        };

        if (Application.Current?.Resources.TryGetValue(key, out var found) != true || found is not Style style)
        {
            return;
        }

        HomeCpuWidget.Style = style;
        HomeGpuWidget.Style = style;
        HomePackageWidget.Style = style;
        HomeClockWidget.Style = style;
        HomeLoadWidget.Style = style;
    }
}
