using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using UnboundOS.Core.Abstractions;
using UnboundOS.Core.Models;

namespace UnboundOS.App.ViewModels;

public partial class ToolsViewModel(
    IDesktopToolCatalog catalog,
    IDesktopToolLauncher launcher) : ObservableObject
{
    public ObservableCollection<DesktopTool> Tools { get; } = [];
    public ObservableCollection<DesktopTool> KitTools { get; } = [];
    public ObservableCollection<DesktopTool> UtilityTools { get; } = [];

    [ObservableProperty] private DesktopTool? _selectedTool;
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private string _status = "Scanning local kit…";

    public bool CanLaunch => SelectedTool?.CanOpen == true;

    public bool CanGet => SelectedTool is not null;

    public bool ShowObsRecipe => SelectedTool?.HasObsRecipe == true;

    public bool ShowObsNeedsInstall => ShowObsRecipe && SelectedTool?.IsInstalled == false;

    public string AvailabilityLabel => SelectedTool?.AvailabilityLabel ?? "Select a tool";

    public string SelectedJob => SelectedTool?.Job ?? "Pick a kit app to launch or get.";

    public string SelectedName => SelectedTool?.DisplayName ?? "Tools";

    public DesktopTool? SelectedKitTool =>
        SelectedTool?.Group == DesktopToolGroup.Kit ? SelectedTool : null;

    public DesktopTool? SelectedUtilityTool =>
        SelectedTool?.Group == DesktopToolGroup.Utility ? SelectedTool : null;

    public async Task InitializeAsync()
    {
        IsBusy = true;
        try
        {
            var discovered = await catalog.DiscoverAsync();
            var selectedId = SelectedTool?.Id;
            Tools.Clear();
            KitTools.Clear();
            UtilityTools.Clear();
            foreach (var tool in discovered)
            {
                Tools.Add(tool);
                if (tool.Group == DesktopToolGroup.Utility)
                {
                    UtilityTools.Add(tool);
                }
                else
                {
                    KitTools.Add(tool);
                }
            }

            SelectedTool = Tools.FirstOrDefault(tool => tool.Id == selectedId)
                ?? KitTools.FirstOrDefault()
                ?? Tools.FirstOrDefault();
            Status = Summarize();
        }
        catch (Exception error)
        {
            Status = $"Tool discovery could not complete: {error.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    partial void OnSelectedToolChanged(DesktopTool? value)
    {
        OnPropertyChanged(nameof(CanLaunch));
        OnPropertyChanged(nameof(CanGet));
        OnPropertyChanged(nameof(ShowObsRecipe));
        OnPropertyChanged(nameof(ShowObsNeedsInstall));
        OnPropertyChanged(nameof(AvailabilityLabel));
        OnPropertyChanged(nameof(SelectedJob));
        OnPropertyChanged(nameof(SelectedName));
        OnPropertyChanged(nameof(SelectedKitTool));
        OnPropertyChanged(nameof(SelectedUtilityTool));
    }

    [RelayCommand]
    private void SelectTool(DesktopTool? tool)
    {
        if (tool is not null)
        {
            SelectedTool = tool;
        }
    }

    [RelayCommand]
    private async Task RefreshAsync() => await InitializeAsync();

    [RelayCommand]
    private async Task LaunchAsync()
    {
        if (SelectedTool is null)
        {
            return;
        }

        var result = await launcher.LaunchAsync(SelectedTool);
        Status = result.Message;
    }

    [RelayCommand]
    private async Task GetAsync()
    {
        if (SelectedTool is null)
        {
            return;
        }

        var result = await launcher.OpenGetPathAsync(SelectedTool);
        Status = result.Message;
    }

    private string Summarize()
    {
        var installed = Tools.Count(tool => tool.IsInstalled);
        if (Tools.Count == 0)
        {
            return "No kit tiles are registered.";
        }

        return $"{installed} of {Tools.Count} kit apps found locally. Missing tiles offer an official Get page — UnboundOS does not install them.";
    }
}
