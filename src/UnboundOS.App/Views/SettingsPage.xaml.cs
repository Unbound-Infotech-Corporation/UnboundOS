using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using UnboundOS.App.Services;
using UnboundOS.App.ViewModels;

namespace UnboundOS.App.Views;

public sealed partial class SettingsPage : Page
{
    public SettingsViewModel ViewModel { get; } = AppServices.Get<SettingsViewModel>();

    public SettingsPage()
    {
        InitializeComponent();
        ViewModel.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName is nameof(ViewModel.SelectedGroup) or nameof(ViewModel.ShowMotion) or null)
            {
                SyncPanels();
            }
        };
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        await ViewModel.InitializeAsync();
        SyncPanels();
    }

    private void SyncPanels()
    {
        MotionPanel.Visibility = ViewModel.ShowMotion ? Visibility.Visible : Visibility.Collapsed;
        DisplayPanel.Visibility = ViewModel.ShowDisplay ? Visibility.Visible : Visibility.Collapsed;
        OverclockPanel.Visibility = ViewModel.ShowOverclock ? Visibility.Visible : Visibility.Collapsed;
        StartupPanel.Visibility = ViewModel.ShowStartup ? Visibility.Visible : Visibility.Collapsed;
        CleanupPanel.Visibility = ViewModel.ShowCleanup ? Visibility.Visible : Visibility.Collapsed;
    }
}
