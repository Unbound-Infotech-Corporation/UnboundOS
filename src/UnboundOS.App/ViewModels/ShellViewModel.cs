using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml;
using UnboundOS.Core;
using UnboundOS.Core.Abstractions;
using UnboundOS.Core.Models;
using UnboundOS.Core.Overlay;

namespace UnboundOS.App.ViewModels;

public partial class ShellViewModel : ObservableObject
{
    private readonly ISessionEngine _session;
    private readonly ITelemetryService _telemetry;
    private readonly IProfileStore _profiles;
    private readonly IDesktopOverlayHost _overlay;
    private readonly IUiMotionPolicy _motion;
    private readonly DispatcherTimer _timer;

    public ShellViewModel(
        ISessionEngine session,
        ITelemetryService telemetry,
        IProfileStore profiles,
        IDesktopOverlayHost overlay,
        IUiMotionPolicy motion)
    {
        _session = session;
        _telemetry = telemetry;
        _profiles = profiles;
        _overlay = overlay;
        _motion = motion;
        _session.StateChanged += (_, state) =>
        {
            SessionStateText = state.ToString();
            IsSessionActive = state == SessionState.Active;
        };

        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
        _timer.Tick += async (_, _) => await RefreshTelemetryAsync();
    }

    public string ProductName => Branding.ProductName;
    public string CompanyName => Branding.CompanyName;
    public string Tagline => Branding.Tagline;
    public string VersionText => Branding.Version;

    [ObservableProperty] private string _sessionStateText = SessionState.Idle.ToString();
    [ObservableProperty] private bool _isSessionActive;
    [ObservableProperty] private string _activeProfileName = "None";
    [ObservableProperty] private string _statusLine = "Shell online. Pick a lane.";
    [ObservableProperty] private double _cpuUsage;
    [ObservableProperty] private string _memoryText = "—";
    [ObservableProperty] private int _processCount;
    [ObservableProperty] private int _suspectCount;
    [ObservableProperty] private string _selectedNav = "Home";

    public string CpuUsageText => $"{CpuUsage:0}%";

    /// <summary>Overlay nav stays hidden unless an addon host is enabled.</summary>
    public bool OverlayNavVisible => _overlay.IsEnabled;

    partial void OnCpuUsageChanged(double value) => OnPropertyChanged(nameof(CpuUsageText));

    public async Task InitializeAsync()
    {
        await _profiles.EnsureDefaultsAsync();
        await _motion.InitializeAsync();
        await RefreshTelemetryAsync();
        _timer.Start();
    }

    [RelayCommand]
    private void Navigate(string tag) => SelectedNav = tag;

    private async Task RefreshTelemetryAsync()
    {
        var profiles = await _profiles.LoadAsync();
        var denylist = profiles.SelectMany(p => p.TerminateProcessNames).Distinct(StringComparer.OrdinalIgnoreCase);
        var sample = await _telemetry.SampleAsync(denylist);
        CpuUsage = Math.Round(sample.CpuUsagePercent, 0);
        MemoryText = $"{sample.MemoryUsedGb:0.0} / {sample.MemoryTotalGb:0.0} GB";
        ProcessCount = sample.ProcessCount;
        SuspectCount = sample.BackgroundSuspectCount;
        ActiveProfileName = _session.ActiveProfile?.Name ?? "None";
        SessionStateText = _session.State.ToString();
        IsSessionActive = _session.State == SessionState.Active;
    }
}
