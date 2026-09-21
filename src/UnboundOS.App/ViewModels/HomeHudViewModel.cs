using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml;
using UnboundOS.Core.Abstractions;

namespace UnboundOS.App.ViewModels;

public partial class HomeHudViewModel : ObservableObject
{
    private readonly IHomeHudSettings _hud;
    private readonly IThermalProbe _thermal;
    private readonly DispatcherTimer _clockTimer;
    private readonly DispatcherTimer _tempTimer;
    private bool _tempsBusy;

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
    [ObservableProperty] private Visibility _packageVisibility = Visibility.Collapsed;

    public async Task InitializeAsync()
    {
        await _hud.InitializeAsync();
        ApplyVisibility();
        TickClock();
        _clockTimer.Start();
        _tempTimer.Start();
        await RefreshTempsAsync();
    }

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
        ClockStripVisibility = hud && _hud.ClockEnabled ? Visibility.Visible : Visibility.Collapsed;
        TempsStripVisibility = hud && _hud.TempsEnabled ? Visibility.Visible : Visibility.Collapsed;
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
            PackageVisibility = snap.PackageCelsius is not null &&
                snap.PackageCelsius != snap.CpuCelsius
                ? Visibility.Visible
                : Visibility.Collapsed;
        }
        catch
        {
            CpuTempText = "—";
            GpuTempText = "—";
            PackageTempText = "—";
            PackageVisibility = Visibility.Collapsed;
        }
        finally
        {
            _tempsBusy = false;
        }
    }

    private static string FormatTemp(double? celsius) =>
        celsius is >= 1 and <= 125 ? $"{celsius.Value:0}°" : "—";
}
