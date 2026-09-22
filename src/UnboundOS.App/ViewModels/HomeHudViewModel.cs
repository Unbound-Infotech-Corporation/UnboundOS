using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml;
using UnboundOS.Core.Abstractions;
using UnboundOS.Core.Home;

namespace UnboundOS.App.ViewModels;

public partial class HomeHudViewModel : ObservableObject
{
    private readonly IHomeHudSettings _hud;
    private readonly IThermalProbe _thermal;
    private readonly DispatcherTimer _clockTimer;
    private readonly DispatcherTimer _tempTimer;
    private bool _tempsBusy;
    private bool _packageHasReading;

    public HomeHudViewModel(IHomeHudSettings hud, IThermalProbe thermal)
    {
        _hud = hud;
        _thermal = thermal;
        _hud.Changed += OnHudChanged;
        _clockTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _clockTimer.Tick += (_, _) => TickClock();
        _tempTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(3) };
        _tempTimer.Tick += async (_, _) => await RefreshTempsAsync();
        TickClock();
        ApplyVisibility();
    }

    [ObservableProperty] private string _clockText = "--:--";
    [ObservableProperty] private string _cpuTempText = "—";
    [ObservableProperty] private string _gpuTempText = "—";
    [ObservableProperty] private string _packageTempText = "—";
    [ObservableProperty] private Visibility _clockStripVisibility = Visibility.Visible;
    [ObservableProperty] private Visibility _tempsStripVisibility = Visibility.Visible;
    [ObservableProperty] private Visibility _cpuWidgetVisibility = Visibility.Visible;
    [ObservableProperty] private Visibility _gpuWidgetVisibility = Visibility.Visible;
    [ObservableProperty] private Visibility _packageVisibility = Visibility.Collapsed;
    [ObservableProperty] private HomeWidgetAppearance _appearance = HomeWidgetAppearance.Glass;
    [ObservableProperty] private string _appearanceToken = "glass";
    [ObservableProperty] private int _layoutRevision;

    public IReadOnlyList<HomeWidgetPlacement> Placements => _hud.Placements;

    public HomeWidgetPlacement Placement(string id) => HomeWidgets.Place(_hud.Placements, id);

    public async Task InitializeAsync()
    {
        await _hud.InitializeAsync();
        ApplyVisibility();
        TickClock();
        _clockTimer.Start();
        _tempTimer.Start();
        await RefreshTempsAsync();
    }

    public Task MoveAsync(string id, double x, double y) => _hud.SetPlacementAsync(id, x, y);

    public Task ResetLayoutAsync() => _hud.ResetPlacementsAsync();

    private void OnHudChanged(object? sender, EventArgs e)
    {
        var queue = App.DispatcherQueue;
        if (queue is not null && !queue.HasThreadAccess)
        {
            _ = queue.TryEnqueue(ApplyVisibility);
            return;
        }

        ApplyVisibility();
    }

    private void ApplyVisibility()
    {
        var hud = _hud.HudEnabled;
        var clockOn = hud && _hud.ClockEnabled && Placement(HomeWidgets.Clock).IsVisible;
        var tempsOn = hud && _hud.TempsEnabled;
        ClockStripVisibility = clockOn ? Visibility.Visible : Visibility.Collapsed;
        TempsStripVisibility = tempsOn ? Visibility.Visible : Visibility.Collapsed;
        CpuWidgetVisibility = tempsOn && Placement(HomeWidgets.Cpu).IsVisible ? Visibility.Visible : Visibility.Collapsed;
        GpuWidgetVisibility = tempsOn && Placement(HomeWidgets.Gpu).IsVisible ? Visibility.Visible : Visibility.Collapsed;
        PackageVisibility = tempsOn && _packageHasReading && Placement(HomeWidgets.Package).IsVisible
            ? Visibility.Visible
            : Visibility.Collapsed;
        Appearance = _hud.Appearance;
        AppearanceToken = HomeWidgets.AppearanceToken(_hud.Appearance);
        LayoutRevision++;
    }

    private void TickClock() =>
        ClockText = DateTime.Now.ToString("HH:mm");

    private async Task RefreshTempsAsync()
    {
        if (_tempsBusy || _hud is { HudEnabled: false } || !_hud.TempsEnabled)
        {
            return;
        }

        _tempsBusy = true;
        try
        {
            var snap = await _thermal.ReadAsync();
            CpuTempText = FormatTemp(snap.CpuCelsius);
            GpuTempText = FormatTemp(snap.GpuCelsius);
            PackageTempText = FormatTemp(snap.PackageCelsius);
            _packageHasReading = snap.PackageCelsius is not null &&
                snap.PackageCelsius != snap.CpuCelsius;
            ApplyVisibility();
        }
        catch
        {
            CpuTempText = "—";
            GpuTempText = "—";
            PackageTempText = "—";
            _packageHasReading = false;
            ApplyVisibility();
        }
        finally
        {
            _tempsBusy = false;
        }
    }

    private static string FormatTemp(double? celsius) =>
        celsius is >= 1 and <= 125 ? $"{celsius.Value:0}°" : "—";
}
