using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using UnboundOS.App.Motion;
using UnboundOS.App.Services;
using UnboundOS.App.ViewModels;
using UnboundOS.Core.Abstractions;

namespace UnboundOS.App.Views;

public sealed partial class SettingsPage : Page
{
    public SettingsViewModel ViewModel { get; } = AppServices.Get<SettingsViewModel>();

    public SettingsPage()
    {
        InitializeComponent();
        ViewModel.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName is nameof(ViewModel.SelectedGroup) or nameof(ViewModel.ShowMotion) or nameof(ViewModel.ShowHud) or nameof(ViewModel.ShowSkinny) or nameof(ViewModel.ShowUpdates) or null)
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
        var allow = AppServices.Get<IUiMotionPolicy>().AllowMotion;
        ConsoleMotion.SetVisible(MotionPanel, ViewModel.ShowMotion, allow);
        ConsoleMotion.SetVisible(HudPanel, ViewModel.ShowHud, allow);
        ConsoleMotion.SetVisible(SkinnyPanel, ViewModel.ShowSkinny, allow);
        ConsoleMotion.SetVisible(UpdatesPanel, ViewModel.ShowUpdates, allow);
        ConsoleMotion.SetVisible(DisplayPanel, ViewModel.ShowDisplay, allow);
        ConsoleMotion.SetVisible(OverclockPanel, ViewModel.ShowOverclock, allow);
        ConsoleMotion.SetVisible(StartupPanel, ViewModel.ShowStartup, allow);
        ConsoleMotion.SetVisible(CleanupPanel, ViewModel.ShowCleanup, allow);
    }
}
