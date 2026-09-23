using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using UnboundOS.Core;
using UnboundOS.Core.Abstractions;
using UnboundOS.Core.Home;
using UnboundOS.Core.Models;

namespace UnboundOS.App.ViewModels;

public sealed record SettingsGroup(string Id, string Title, string Hint);

public partial class SettingsViewModel : ObservableObject
{
    private readonly IUiMotionPolicy _motion;
    private readonly IHomeHudSettings _hud;
    private readonly IStartupAuditService _startup;
    private readonly IVendorAppCatalog _vendors;
    private readonly IVendorAppLauncher _vendorLauncher;
    private readonly ISetupCleanup _cleanup;
    private readonly IGamingSkinnyPolicy _skinny;
    private readonly IUpdateGuardPolicy _updates;
    private readonly IShellSettingsStore _shell;
    private bool _suppressToggle;
    private bool _suppressHudToggle;
    private bool _suppressHagsToggle;
    private bool _suppressUpdateGuardToggle;

    public SettingsViewModel(
        IUiMotionPolicy motion,
        IHomeHudSettings hud,
        IStartupAuditService startup,
        IVendorAppCatalog vendors,
        IVendorAppLauncher vendorLauncher,
        ISetupCleanup cleanup,
        IGamingSkinnyPolicy skinny,
        IUpdateGuardPolicy updates,
        IShellSettingsStore shell)
    {
        _motion = motion;
        _hud = hud;
        _startup = startup;
        _vendors = vendors;
        _vendorLauncher = vendorLauncher;
        _cleanup = cleanup;
        _skinny = skinny;
        _updates = updates;
        _shell = shell;
        _motion.Changed += OnMotionChanged;
        _hud.Changed += OnHudChanged;
    }

    [ObservableProperty] private bool _interfaceMotionEnabled = true;
    [ObservableProperty] private bool _homeHudEnabled = true;
    [ObservableProperty] private bool _hagsEnabled;
    [ObservableProperty] private string _hagsStatus =
        "HAGS is hardware/game dependent. Test frametimes. Not forced on session enter.";
    [ObservableProperty] private bool _updateGuardEnabled;
    [ObservableProperty] private string _updateGuardStatus = OsProductCopy.UpdateGuardHonesty;
    [ObservableProperty] private string _windowsQualityStatus = "Windows quality: unknown.";
    [ObservableProperty] private string _shellVersionStatus = "UnboundOS shell: —";
    [ObservableProperty] private bool _homeClockEnabled = true;
    [ObservableProperty] private bool _homeTempsEnabled = true;
    [ObservableProperty] private string _homeWidgetLook = "Glass";
    [ObservableProperty] private string _motionStatus = string.Empty;
    [ObservableProperty] private string _sessionNote =
        "A live session always pauses motion so frame time stays clean. This toggle is not hidden.";
    [ObservableProperty] private string _startupSummary = OsProductCopy.StartupHonesty;
    [ObservableProperty] private string _vendorStatus = OsProductCopy.DisplayHonesty;
    [ObservableProperty] private string _cleanupStatus = OsProductCopy.CleanupHonesty;
    [ObservableProperty] private StartupEntry? _selectedStartup;
    [ObservableProperty] private VendorApp? _selectedDisplayApp;
    [ObservableProperty] private VendorApp? _selectedOcApp;
    [ObservableProperty] private SettingsGroup? _selectedGroup;

    /// <summary>Keep titles aligned with <c>CubeBrowse.Options()</c>.</summary>
    public ObservableCollection<SettingsGroup> Groups { get; } =
    [
        new("motion", "Interface motion", "Home tab lift, list ease, and tile focus motion."),
        new("hud", "Home HUD", "Packaged clock, date, viz, and calendar on Home. Off hides that chrome."),
        new("skinny", "Session skinny", "Game Mode, Game DVR, visual effects, HAGS. Reversible. Defender stays on."),
        new("updates", "Updates", "Quality/LCU from Microsoft; feature updates deferred. UnboundOS does not host Windows patches."),
        new("display", "Display", "Launch the GPU vendor app. UnboundOS does not write display settings."),
        new("overclock", "Overclocking", "Launch-only vendor OC hubs. No silent clocks."),
        new("startup", "Startup audit", "Pin allowlist. Never silently kill anticheat or GPU vendor."),
        new("cleanup", "Finish setup", "Known leftover folders. The image owns the full wipe.")
    ];

    public ObservableCollection<StartupEntry> StartupEntries { get; } = [];
    public ObservableCollection<VendorApp> DisplayApps { get; } = [];
    public ObservableCollection<VendorApp> OverclockApps { get; } = [];
    public IReadOnlyList<string> WidgetLooks { get; } = ["Glass", "Dim", "Compact"];

    public string DisplayHonesty => OsProductCopy.DisplayHonesty;
    public string OverclockHonesty => OsProductCopy.OverclockHonesty;
    public string StartupHonesty => OsProductCopy.StartupHonesty;
    public string CleanupHonesty => OsProductCopy.CleanupHonesty;
    public string HardwareHonesty => OsProductCopy.HardwareHonesty;
    public string FilesHonesty => OsProductCopy.FilesHonesty;
    public string UpdateGuardHonesty => OsProductCopy.UpdateGuardHonesty;

    public bool CanPinSelected => SelectedStartup is { Disposition: not StartupDisposition.Protected };
    public bool SelectedIsPinned => SelectedStartup?.IsPinned == true;

    public async Task InitializeAsync()
    {
        await _motion.InitializeAsync();
        await _hud.InitializeAsync();
        SyncFromPolicy();
        SyncFromHud();
        await SyncFromSkinnyAsync();
        await SyncFromUpdateGuardAsync();
        await RefreshVendorsAsync();
        await RefreshStartupAsync();
        SelectedGroup ??= Groups[0];
    }

    public void SelectGroup(string? id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return;
        }

        var match = Groups.FirstOrDefault(group =>
            string.Equals(group.Id, id, StringComparison.OrdinalIgnoreCase));
        if (match is not null)
        {
            SelectedGroup = match;
        }
    }

    partial void OnSelectedGroupChanged(SettingsGroup? value)
    {
        OnPropertyChanged(nameof(ShowMotion));
        OnPropertyChanged(nameof(ShowHud));
        OnPropertyChanged(nameof(ShowSkinny));
        OnPropertyChanged(nameof(ShowUpdates));
        OnPropertyChanged(nameof(ShowDisplay));
        OnPropertyChanged(nameof(ShowOverclock));
        OnPropertyChanged(nameof(ShowStartup));
        OnPropertyChanged(nameof(ShowCleanup));
        OnPropertyChanged(nameof(GroupHint));
    }

    public bool ShowMotion => SelectedGroup?.Id == "motion";
    public bool ShowHud => SelectedGroup?.Id == "hud";
    public bool ShowSkinny => SelectedGroup?.Id == "skinny";
    public bool ShowUpdates => SelectedGroup?.Id == "updates";
    public bool ShowDisplay => SelectedGroup?.Id == "display";
    public bool ShowOverclock => SelectedGroup?.Id == "overclock";
    public bool ShowStartup => SelectedGroup?.Id == "startup";
    public bool ShowCleanup => SelectedGroup?.Id == "cleanup";
    public string GroupHint => SelectedGroup?.Hint ?? "Pick a group.";

    partial void OnInterfaceMotionEnabledChanged(bool value)
    {
        if (_suppressToggle)
        {
            return;
        }

        _ = _motion.SetUserWantsMotionAsync(value);
    }

    partial void OnHomeHudEnabledChanged(bool value)
    {
        if (_suppressHudToggle)
        {
            return;
        }

        _ = _hud.SetHudEnabledAsync(value);
    }

    partial void OnHomeClockEnabledChanged(bool value)
    {
        if (_suppressHudToggle)
        {
            return;
        }

        _ = _hud.SetClockEnabledAsync(value);
    }

    partial void OnHomeTempsEnabledChanged(bool value)
    {
        if (_suppressHudToggle)
        {
            return;
        }

        _ = _hud.SetTempsEnabledAsync(value);
    }

    partial void OnHagsEnabledChanged(bool value)
    {
        if (_suppressHagsToggle)
        {
            return;
        }

        _ = PersistHagsAsync(value);
    }

    private async Task PersistHagsAsync(bool enabled)
    {
        try
        {
            var current = await _shell.LoadAsync();
            await _shell.SaveAsync(current with { HardwareGpuScheduling = enabled });
            var result = await _skinny.TrySetHagsAsync(enabled);
            HagsStatus = result.Message;
        }
        catch (Exception error)
        {
            HagsStatus = error.Message;
        }
    }

    private async Task SyncFromSkinnyAsync()
    {
        var settings = await _shell.LoadAsync();
        _suppressHagsToggle = true;
        HagsEnabled = settings.HardwareGpuScheduling ?? false;
        _suppressHagsToggle = false;
        HagsStatus = settings.HardwareGpuScheduling is null
            ? "HAGS is hardware/game dependent. Test frametimes. Not forced on session enter."
            : settings.HardwareGpuScheduling.Value
                ? "HAGS preference is on. Test frametimes in your titles."
                : "HAGS preference is off. Windows default left unless applied elevated.";
    }

    partial void OnUpdateGuardEnabledChanged(bool value)
    {
        if (_suppressUpdateGuardToggle)
        {
            return;
        }

        _ = PersistUpdateGuardAsync(value);
    }

    private async Task PersistUpdateGuardAsync(bool enabled)
    {
        try
        {
            var target = _updates.DescribeTarget();
            var current = await _shell.LoadAsync();
            await _shell.SaveAsync(current with
            {
                UpdateGuardEnabled = enabled,
                UpdateGuardTargetRelease = $"{target.ProductVersion} {target.DisplayVersion}".Trim()
            });
            var result = enabled
                ? await _updates.TryApplyUpdateGuardAsync()
                : await _updates.TryClearUpdateGuardAsync();
            UpdateGuardStatus = result.Message;
            await RefreshUpdateStatusAsync();
        }
        catch (Exception error)
        {
            UpdateGuardStatus = error.Message;
        }
    }

    private async Task SyncFromUpdateGuardAsync()
    {
        var settings = await _shell.LoadAsync();
        _suppressUpdateGuardToggle = true;
        UpdateGuardEnabled = settings.UpdateGuardEnabled ?? false;
        _suppressUpdateGuardToggle = false;
        UpdateGuardStatus = settings.UpdateGuardEnabled is true
            ? OsProductCopy.UpdateGuardHonesty
            : "Update Guard is off. Windows Update stays available. " + OsProductCopy.UpdateGuardHonesty;
        await RefreshUpdateStatusAsync();
    }

    [RelayCommand]
    private async Task ApplyUpdateGuardAsync()
    {
        _suppressUpdateGuardToggle = true;
        UpdateGuardEnabled = true;
        _suppressUpdateGuardToggle = false;
        await PersistUpdateGuardAsync(true);
    }

    [RelayCommand]
    private async Task RestoreUpdateGuardAsync()
    {
        _suppressUpdateGuardToggle = true;
        UpdateGuardEnabled = false;
        _suppressUpdateGuardToggle = false;
        await PersistUpdateGuardAsync(false);
    }

    [RelayCommand]
    private async Task CheckQualityUpdatesAsync()
    {
        var result = await _updates.OpenWindowsUpdateSettingsAsync();
        UpdateGuardStatus = result.Message;
        await RefreshUpdateStatusAsync();
    }

    private async Task RefreshUpdateStatusAsync()
    {
        var probe = await _updates.ProbeQualityStatusAsync();
        WindowsQualityStatus = $"Windows quality: {probe.QualityLabel}. {probe.Detail}";
        ShellVersionStatus = $"UnboundOS shell: {probe.ShellVersion}";
        if (!string.IsNullOrWhiteSpace(probe.TargetRelease))
        {
            WindowsQualityStatus += $" Pin {probe.TargetRelease}.";
        }
    }

    partial void OnHomeWidgetLookChanged(string value)
    {
        if (_suppressHudToggle)
        {
            return;
        }

        _ = _hud.SetAppearanceAsync(HomeWidgets.ParseAppearance(value));
    }

    [RelayCommand]
    private Task ResetWidgetPositionsAsync() => _hud.ResetPlacementsAsync();

    partial void OnSelectedStartupChanged(StartupEntry? value)
    {
        OnPropertyChanged(nameof(CanPinSelected));
        OnPropertyChanged(nameof(SelectedIsPinned));
    }

    [RelayCommand]
    private async Task RefreshStartupAsync()
    {
        try
        {
            var report = await _startup.AuditAsync();
            StartupEntries.Clear();
            foreach (var entry in report.Entries)
            {
                StartupEntries.Add(entry);
            }

            SelectedStartup = StartupEntries.FirstOrDefault();
            StartupSummary = report.Summary + " " + OsProductCopy.StartupHonesty;
        }
        catch (Exception error)
        {
            StartupSummary = $"Startup audit could not complete: {error.Message}";
        }
    }

    [RelayCommand]
    private async Task PinSelectedAsync()
    {
        if (SelectedStartup is null || SelectedStartup.Disposition == StartupDisposition.Protected)
        {
            return;
        }

        await _startup.PinAsync(SelectedStartup.Id, pinned: !SelectedStartup.IsPinned);
        await RefreshStartupAsync();
    }

    [RelayCommand]
    private async Task ApplyStartupAsync()
    {
        try
        {
            StartupSummary = await _startup.ApplyRecommendedAsync() + " " + OsProductCopy.StartupHonesty;
            await RefreshStartupAsync();
        }
        catch (Exception error)
        {
            StartupSummary = error.Message;
        }
    }

    [RelayCommand]
    private async Task RefreshVendorsAsync()
    {
        try
        {
            var apps = await _vendors.DiscoverAsync();
            DisplayApps.Clear();
            OverclockApps.Clear();
            foreach (var app in apps)
            {
                if (app.Role == VendorAppRole.Display)
                {
                    DisplayApps.Add(app);
                }
                else
                {
                    OverclockApps.Add(app);
                }
            }

            SelectedDisplayApp = DisplayApps.FirstOrDefault(app => app.IsInstalled) ?? DisplayApps.FirstOrDefault();
            SelectedOcApp = OverclockApps.FirstOrDefault(app => app.IsInstalled) ?? OverclockApps.FirstOrDefault();
            VendorStatus = DisplayApps.Any(app => app.IsInstalled)
                ? OsProductCopy.DisplayHonesty
                : "No GPU vendor app found. Get NVIDIA App, AMD Adrenalin, or Intel Arc Control. " + OsProductCopy.DisplayHonesty;
        }
        catch (Exception error)
        {
            VendorStatus = error.Message;
        }
    }

    [RelayCommand]
    private async Task OpenDisplayAsync()
    {
        if (SelectedDisplayApp is null)
        {
            VendorStatus = "No display vendor app selected. " + OsProductCopy.DisplayHonesty;
            return;
        }

        var result = SelectedDisplayApp.IsInstalled
            ? await _vendorLauncher.LaunchAsync(SelectedDisplayApp)
            : await _vendorLauncher.OpenGetPathAsync(SelectedDisplayApp);
        VendorStatus = result.Message;
    }

    [RelayCommand]
    private async Task OpenOverclockAsync()
    {
        if (SelectedOcApp is null)
        {
            VendorStatus = "No overclocking tool found. " + OsProductCopy.OverclockHonesty;
            return;
        }

        var result = SelectedOcApp.IsInstalled
            ? await _vendorLauncher.LaunchAsync(SelectedOcApp)
            : await _vendorLauncher.OpenGetPathAsync(SelectedOcApp);
        VendorStatus = result.Message;
    }

    [RelayCommand]
    private async Task CleanLeftoversAsync()
    {
        try
        {
            var result = await _cleanup.CleanLeftoversAsync();
            CleanupStatus = result.Message;
        }
        catch (Exception error)
        {
            CleanupStatus = error.Message;
        }
    }

    private void OnMotionChanged(object? sender, EventArgs e)
    {
        var queue = App.DispatcherQueue;
        if (queue is not null && !queue.HasThreadAccess)
        {
            _ = queue.TryEnqueue(SyncFromPolicy);
            return;
        }

        SyncFromPolicy();
    }

    private void SyncFromPolicy()
    {
        _suppressToggle = true;
        InterfaceMotionEnabled = _motion.UserWantsMotion;
        _suppressToggle = false;
        MotionStatus = _motion.StatusText;
    }

    private void OnHudChanged(object? sender, EventArgs e)
    {
        var queue = App.DispatcherQueue;
        if (queue is not null && !queue.HasThreadAccess)
        {
            _ = queue.TryEnqueue(SyncFromHud);
            return;
        }

        SyncFromHud();
    }

    private void SyncFromHud()
    {
        _suppressHudToggle = true;
        HomeHudEnabled = _hud.HudEnabled;
        HomeClockEnabled = _hud.ClockEnabled;
        HomeTempsEnabled = _hud.TempsEnabled;
        HomeWidgetLook = _hud.Appearance switch
        {
            HomeWidgetAppearance.Dim => "Dim",
            HomeWidgetAppearance.Compact => "Compact",
            _ => "Glass"
        };
        _suppressHudToggle = false;
    }
}
