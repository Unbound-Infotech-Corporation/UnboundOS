using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using UnboundOS.App.Services;
using UnboundOS.App.ViewModels;
using UnboundOS.App.Views;

namespace UnboundOS.App;

public sealed partial class MainPage : Page
{
    public ShellViewModel ViewModel { get; } = AppServices.Get<ShellViewModel>();

    public MainPage()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        await ViewModel.InitializeAsync();
        ViewModel.StatusLine = "Shell online. Pick a lane.";
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
    private void GoStream_Click(object sender, RoutedEventArgs e) => Navigate("Stream");
    private void GoNetwork_Click(object sender, RoutedEventArgs e) => Navigate("Network");
    private void GoMods_Click(object sender, RoutedEventArgs e) => Navigate("Mods");

    private void Navigate(string tag)
    {
        ViewModel.SelectedNav = tag;
        ViewModel.StatusLine = tag switch
        {
            "Home" => "Shell online. Pick a lane.",
            "Session" => "Session engine armed.",
            "Network" => "Network director ready.",
            "Stream" => "Ultrawide canvas ready.",
            "Mods" => "Workshop catalog and mod profiles ready.",
            "Profiles" => "Profile bay open.",
            _ => ViewModel.StatusLine
        };

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
            "Stream" => typeof(StreamPage),
            "Mods" => typeof(ModsPage),
            "Profiles" => typeof(ProfilesPage),
            _ => typeof(SessionPage)
        };

        ContentFrame.Navigate(pageType);
    }
}
