using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using UnboundOS.Core.Abstractions;
using UnboundOS.Core.Models;
using UnboundOS.Core.Overlay;
using UnboundOS.Infrastructure.Tools;

namespace UnboundOS.App.ViewModels;

/// <summary>
/// Rainmeter overlay surface over Home. Opens the real host
/// and Gets Phenix. Does not start, stop, or own session/network/process behavior.
/// </summary>
public partial class OverlayViewModel : ObservableObject
{
    private readonly IDesktopToolLauncher _launcher;
    private readonly RainmeterLauncher _rainmeter;

    public OverlayViewModel(
        IDesktopOverlayHost host,
        IDesktopToolLauncher launcher,
        RainmeterLauncher rainmeter)
    {
        ArgumentNullException.ThrowIfNull(host);
        _launcher = launcher;
        _rainmeter = rainmeter;
        IsEnabled = host.IsEnabled;
        DisplayName = host.DisplayName;
        Widgets = host.Widgets;
        Headline = host.IsEnabled ? "Rainmeter overlay" : "Rainmeter overlay is off";
        Detail = host.IsEnabled
            ? "Rainmeter skins sit over Home. Open launches Rainmeter if installed. Get Phenix for the recommended starter theme. UnboundOS does not rewrite rainmeter.ini. Built-in Home widgets stay as the fallback."
            : "Rainmeter overlay is disabled (OverlayHostOptions.Enabled = false). Built-in Home widgets remain the fallback.";
        Status = host.IsEnabled
            ? (CanOpenRainmeter
                ? "Rainmeter found. Open to show skins over Home."
                : "Rainmeter not installed. Get Rainmeter, then Get Phenix.")
            : "Overlay host is off.";
    }

    public bool IsEnabled { get; }
    public string DisplayName { get; }
    public string Headline { get; }
    public string Detail { get; }
    public IReadOnlyList<OverlayWidgetDescriptor> Widgets { get; }

    public bool CanOpenRainmeter => _rainmeter.FindExecutable() is { Length: > 0 };

    [ObservableProperty] private string _status = "";

    [RelayCommand]
    private async Task OpenRainmeterAsync()
    {
        var result = _rainmeter.Open();
        Status = result.Message;
        await Task.CompletedTask;
    }

    [RelayCommand]
    private async Task GetPhenixAsync()
    {
        var result = await _launcher.OpenGetPathAsync(PhenixTile());
        Status = result.Message;
    }

    [RelayCommand]
    private async Task GetRainmeterAsync()
    {
        var result = await _launcher.OpenGetPathAsync(RainmeterTile());
        Status = result.Message;
    }

    private static DesktopTool PhenixTile() =>
        new(
            DesktopToolIds.Phenix,
            "Phenix",
            "Recommended Rainmeter starter over Home.",
            false,
            null,
            [],
            DesktopToolCatalog.PhenixGetPath,
            OpensViaUri: true,
            LaunchPath: DesktopToolCatalog.PhenixGetPath);

    private static DesktopTool RainmeterTile() =>
        new(
            DesktopToolIds.Rainmeter,
            "Rainmeter",
            "Desktop skins over Home.",
            false,
            null,
            ["Rainmeter"],
            DesktopToolCatalog.RainmeterGetPath);
}
