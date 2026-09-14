using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using UnboundOS.App.Services;
using UnboundOS.App.ViewModels;
using UnboundOS.App.Views;
using UnboundOS.Core.Abstractions;

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
        ViewModel.StatusLine = "Shell online. Pick a lane.";
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

    private void GoSession_Click(object sender, RoutedEventArgs e) => Navigate("Session");
    private void GoTools_Click(object sender, RoutedEventArgs e) => Navigate("Tools");
    private void GoNetwork_Click(object sender, RoutedEventArgs e) => Navigate("Network");
    private void GoMods_Click(object sender, RoutedEventArgs e) => Navigate("Mods");

    private void GoProfiles_Click(object sender, RoutedEventArgs e) => Navigate("Profiles");

    private void GoSettings_Click(object sender, RoutedEventArgs e) => Navigate("Settings");

    private void Navigate(string tag)
    {
        ViewModel.SelectedNav = tag;
        ViewModel.StatusLine = tag switch
        {
            "Home" => "Shell online. Pick a lane.",
            "Session" => "Session engine ready.",
            "Network" => "Network director ready.",
            "Tools" => "Tools marketplace ready.",
            "Mods" => "Workshop catalog and mod profiles ready.",
            "Profiles" => "Profile bay open.",
            "Settings" => "Shell settings. Interface motion can pause itself during a session.",
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
