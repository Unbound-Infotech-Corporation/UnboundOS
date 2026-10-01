using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using UnboundOS.Core;
using UnboundOS.Core.Abstractions;
using UnboundOS.Core.Models;

namespace UnboundOS.App.ViewModels;

public partial class LauncherViewModel(IAppLauncherCatalog catalog) : ObservableObject
{
    public ObservableCollection<LauncherApp> Apps { get; } = [];

    [ObservableProperty] private LauncherApp? _selectedApp;
    [ObservableProperty] private string _filter = "";
    [ObservableProperty] private string _status = "Start Menu, Steam, Epic, GOG, Store links.";

    public string Honesty => OsProductCopy.AnticheatHonesty;

    public async Task InitializeAsync() => await RefreshAsync();

    [RelayCommand]
    private async Task RefreshAsync()
    {
        Apps.Clear();
        foreach (var app in await catalog.DiscoverAsync())
        {
            if (string.IsNullOrWhiteSpace(Filter) ||
                app.Title.Contains(Filter, StringComparison.OrdinalIgnoreCase) ||
                app.Source.Contains(Filter, StringComparison.OrdinalIgnoreCase))
            {
                Apps.Add(app);
            }
        }

        SelectedApp = Apps.FirstOrDefault();
        Status = $"{Apps.Count} launchers.";
    }

    [RelayCommand]
    private async Task LaunchAsync()
    {
        if (SelectedApp is null)
        {
            return;
        }

        var result = await catalog.LaunchAsync(SelectedApp);
        Status = result.Message;
    }

    partial void OnFilterChanged(string value) => _ = RefreshAsync();
}
