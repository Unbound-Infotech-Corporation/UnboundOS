using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using UnboundOS.App.Services;
using UnboundOS.App.ViewModels;
using UnboundOS.App.Views;
using UnboundOS.Core.Abstractions;
using UnboundOS.Core.Navigation;

namespace UnboundOS.App;

public sealed partial class MainPage : Page
{
    public ShellViewModel ViewModel { get; } = AppServices.Get<ShellViewModel>();
    public HomeHudViewModel Hud { get; } = AppServices.Get<HomeHudViewModel>();

    public MainPage()
    {
        InitializeComponent();
        OverlayNav.Visibility = ViewModel.OverlayNavVisible
            ? Visibility.Visible
            : Visibility.Collapsed;
        Loaded += OnLoaded;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        await ViewModel.InitializeAsync();
        await Hud.InitializeAsync();
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

    private void OnGalaxySettingsRequested(object sender, EventArgs e) =>
        Navigate("Settings");

    private void OnCubeNotice(object sender, string message) =>
        ViewModel.StatusLine = message;

    private void OnCubeFrontChanged(object sender, CubeDestination destination)
    {
        var info = CubeCatalog.Info(destination);
        ViewModel.StatusLine = $"{info.Title} node. Up opens games. Down opens Settings.";
    }

    private void Navigate(string tag)
    {
        ViewModel.SelectedNav = tag;
        ViewModel.StatusLine = tag switch
        {
            "Home" => "Galaxy home. Up: games list. Down: Settings. Left/right: shift nodes.",
            "Session" => "Session engine ready.",
            "Network" => "Network director ready.",
            "Tools" => "Tools marketplace ready.",
            "Files" => "Daily folders. Windows Explorer stays for game launchers.",
            "Hardware" => "CPU, GPU, disks, RAM from this PC. Sensors wait on the image.",
            "Mods" => "Workshop catalog and mod profiles ready.",
            "Profiles" => "Profile bay open.",
            "Settings" => "Display, overclocking launch, startup audit, Home HUD, motion.",
            "Overlay" => "Overlay addon hook — optional and off unless a host is registered.",
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
}
