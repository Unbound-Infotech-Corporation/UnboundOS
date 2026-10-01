using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using UnboundOS.Core;
using UnboundOS.Core.Abstractions;
using UnboundOS.Core.Models;

namespace UnboundOS.App.ViewModels;

public partial class SwitcherViewModel(IRunningAppSwitcher switcher, ITrayStandIn tray) : ObservableObject
{
    public ObservableCollection<RunningApp> Apps { get; } = [];
    public ObservableCollection<TrayApp> Tray { get; } = [];

    [ObservableProperty] private RunningApp? _selectedApp;
    [ObservableProperty] private string _status = "Running windows. Alt+Tab still works (DWM).";

    public string Honesty => OsProductCopy.DesktopModeHonesty;

    public async Task InitializeAsync() => await RefreshAsync();

    [RelayCommand]
    private async Task RefreshAsync()
    {
        Apps.Clear();
        foreach (var app in await switcher.ListAsync())
        {
            Apps.Add(app);
        }

        Tray.Clear();
        foreach (var app in await tray.ListAsync())
        {
            Tray.Add(app);
        }

        SelectedApp = Apps.FirstOrDefault();
        Status = $"{Apps.Count} windows · tray stand-in {Tray.Count(item => item.IsRunning)} live.";
    }

    [RelayCommand]
    private async Task ActivateAsync()
    {
        if (SelectedApp is null)
        {
            return;
        }

        var result = await switcher.ActivateAsync(SelectedApp);
        Status = result.Message;
    }

    [RelayCommand]
    private async Task CloseSelectedAsync()
    {
        if (SelectedApp is null)
        {
            return;
        }

        var result = await switcher.CloseAsync(SelectedApp);
        Status = result.Message;
        await RefreshAsync();
    }
}
