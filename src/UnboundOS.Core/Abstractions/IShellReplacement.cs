using UnboundOS.Core.Models;

namespace UnboundOS.Core.Abstractions;

public interface IShellReplacement
{
    bool IsEnabled { get; }

    Task<(bool Succeeded, string Message)> SetEnabledAsync(bool enabled, CancellationToken cancellationToken = default);
}

public interface IDesktopMode
{
    bool ExplorerRunning { get; }

    Task<(bool Succeeded, string Message)> StartDesktopAsync(CancellationToken cancellationToken = default);

    Task<(bool Succeeded, string Message)> ReturnToShellAsync(CancellationToken cancellationToken = default);
}

public interface ISessionPower
{
    Task<(bool Succeeded, string Message)> LockAsync(CancellationToken cancellationToken = default);

    Task<(bool Succeeded, string Message)> SleepAsync(CancellationToken cancellationToken = default);

    Task<(bool Succeeded, string Message)> RestartAsync(CancellationToken cancellationToken = default);

    Task<(bool Succeeded, string Message)> ShutdownAsync(CancellationToken cancellationToken = default);

    Task<(bool Succeeded, string Message)> SignOutAsync(CancellationToken cancellationToken = default);

    Task<(bool Succeeded, string Message)> SwitchUserAsync(CancellationToken cancellationToken = default);
}

public interface IVolumeKeys
{
    Task<(bool Succeeded, string Message)> VolumeUpAsync();

    Task<(bool Succeeded, string Message)> VolumeDownAsync();

    Task<(bool Succeeded, string Message)> MuteAsync();
}

public interface IRunningAppSwitcher
{
    Task<IReadOnlyList<RunningApp>> ListAsync(CancellationToken cancellationToken = default);

    Task<(bool Succeeded, string Message)> ActivateAsync(RunningApp app, CancellationToken cancellationToken = default);

    Task<(bool Succeeded, string Message)> CloseAsync(RunningApp app, CancellationToken cancellationToken = default);
}

public interface IAppLauncherCatalog
{
    Task<IReadOnlyList<LauncherApp>> DiscoverAsync(CancellationToken cancellationToken = default);

    Task<(bool Succeeded, string Message)> LaunchAsync(LauncherApp app, CancellationToken cancellationToken = default);
}

public interface ITrayStandIn
{
    Task<IReadOnlyList<TrayApp>> ListAsync(CancellationToken cancellationToken = default);
}

public interface IOsSettingsHub
{
    IReadOnlyList<OsSettingsEntry> Entries { get; }

    Task<IReadOnlyList<OsAdapterStatus>> ListAdaptersAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PowerPlanInfo>> ListPowerPlansAsync(CancellationToken cancellationToken = default);

    Task<(bool Succeeded, string Message)> SetPowerPlanAsync(string guid, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<StorageVolumeInfo>> ListVolumesAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<InstalledAppInfo>> ListInstalledAppsAsync(CancellationToken cancellationToken = default);

    Task<(bool Succeeded, string Message)> UninstallAsync(InstalledAppInfo app, CancellationToken cancellationToken = default);

    Task<(bool Succeeded, string Message)> OpenUriAsync(string uri, CancellationToken cancellationToken = default);
}
