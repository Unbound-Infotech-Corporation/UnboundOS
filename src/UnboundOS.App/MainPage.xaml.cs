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
        ViewModel.StatusLine = "Rotate the cube. Enter opens the front face. Settings and Profiles stay in the top bar.";
        ApplyNavState(ViewModel.SelectedNav);
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

    private void OnCubeFrontChanged(object sender, CubeDestination destination)
    {
        var info = CubeCatalog.Info(destination);
        ViewModel.StatusLine = $"{info.Title} faces you. Enter opens it. Settings and Profiles stay in the top bar.";
    }

    private void Navigate(string tag)
    {
        ViewModel.SelectedNav = tag;
        ViewModel.StatusLine = tag switch
        {
            "Home" => "Rotate the cube. Enter opens the front face. Settings and Profiles stay in the top bar.",
            "Session" => "Session engine ready.",
            "Network" => "Network director ready.",
            "Tools" => "Tools marketplace ready.",
            "Files" => "Daily folders. Windows Explorer stays for game launchers.",
            "Hardware" => "CPU, GPU, disks, RAM from this PC. Sensors wait on the image.",
            "Mods" => "Workshop catalog and mod profiles ready.",
            "Profiles" => "Profile bay open.",
            "Settings" => "Display, overclocking launch, startup audit, motion.",
            "Overlay" => "Overlay addon hook — optional and off unless a host is registered.",
            _ => ViewModel.StatusLine
        };

        ApplyNavState(tag);

        if (tag == "Home")
        {
            HomeView.Visibility = Visibility.Visible;
            ContentFrame.Visibility = Visibility.Collapsed;
            return;
        }

        HomeView.Visibility = Visibility.Collapsed;
        ContentFrame.Visibility = Visibility.Visible;

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

    private void ApplyNavState(string tag)
    {
        var motion = AppServices.Get<IUiMotionPolicy>();
        VisualStateManager.GoToState(this, tag, useTransitions: motion.AllowMotion);
    }
}
