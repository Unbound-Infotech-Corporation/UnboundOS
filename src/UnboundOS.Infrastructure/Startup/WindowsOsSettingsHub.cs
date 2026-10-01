using System.Net.NetworkInformation;
using Diag = System.Diagnostics;
using Microsoft.Win32;
using UnboundOS.Core.Abstractions;
using UnboundOS.Core.Models;
using UnboundOS.Core.Shell;

namespace UnboundOS.Infrastructure.Startup;

public sealed class WindowsOsSettingsHub : IOsSettingsHub
{
    public IReadOnlyList<OsSettingsEntry> Entries => OsSettingsCatalog.All;

    public Task<IReadOnlyList<OsAdapterStatus>> ListAdaptersAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            IReadOnlyList<OsAdapterStatus> items = NetworkInterface.GetAllNetworkInterfaces()
                .Where(nic => nic.NetworkInterfaceType is not NetworkInterfaceType.Loopback
                              and not NetworkInterfaceType.Tunnel)
                .Select(nic => new OsAdapterStatus(
                    nic.Name,
                    nic.NetworkInterfaceType.ToString(),
                    nic.OperationalStatus == OperationalStatus.Up,
                    nic.OperationalStatus.ToString()))
                .ToArray();
            return Task.FromResult(items);
        }
        catch (Exception)
        {
            return Task.FromResult<IReadOnlyList<OsAdapterStatus>>([]);
        }
    }

    public Task<IReadOnlyList<PowerPlanInfo>> ListPowerPlansAsync(CancellationToken cancellationToken = default)
    {
        if (!OperatingSystem.IsWindows())
        {
            return Task.FromResult<IReadOnlyList<PowerPlanInfo>>([]);
        }

        try
        {
            var start = new Diag.ProcessStartInfo
            {
                FileName = "powercfg",
                Arguments = "/list",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using var process = Diag.Process.Start(start);
            var text = process?.StandardOutput.ReadToEnd() ?? "";
            process?.WaitForExit(4000);
            var plans = new List<PowerPlanInfo>();
            foreach (var line in text.Split('\n'))
            {
                var guidStart = line.IndexOf(':');
                if (guidStart < 0)
                {
                    continue;
                }

                var rest = line[(guidStart + 1)..].Trim();
                var parts = rest.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length < 2)
                {
                    continue;
                }

                var name = parts[1].Replace("*", string.Empty, StringComparison.Ordinal).Trim();
                var active = line.Contains('*', StringComparison.Ordinal);
                plans.Add(new PowerPlanInfo(parts[0], name, active));
            }

            return Task.FromResult<IReadOnlyList<PowerPlanInfo>>(plans);
        }
        catch (Exception)
        {
            return Task.FromResult<IReadOnlyList<PowerPlanInfo>>([]);
        }
    }

    public Task<(bool Succeeded, string Message)> SetPowerPlanAsync(string guid, CancellationToken cancellationToken = default)
    {
        if (!OperatingSystem.IsWindows())
        {
            return Task.FromResult((false, "powercfg runs on Windows."));
        }

        try
        {
            Diag.Process.Start(new Diag.ProcessStartInfo
            {
                FileName = "powercfg",
                Arguments = $"/setactive {guid}",
                UseShellExecute = false,
                CreateNoWindow = true
            });
            return Task.FromResult((true, $"Active plan {guid}."));
        }
        catch (Exception ex)
        {
            return Task.FromResult((false, ex.Message));
        }
    }

    public Task<IReadOnlyList<StorageVolumeInfo>> ListVolumesAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<StorageVolumeInfo> items = DriveInfo.GetDrives()
            .Select(drive =>
            {
                try
                {
                    return new StorageVolumeInfo(
                        drive.Name,
                        drive.IsReady ? drive.VolumeLabel : "",
                        drive.IsReady ? drive.AvailableFreeSpace : 0,
                        drive.IsReady ? drive.TotalSize : 0,
                        drive.IsReady,
                        drive.DriveType == DriveType.Removable);
                }
                catch (Exception)
                {
                    return new StorageVolumeInfo(drive.Name, "", 0, 0, false, drive.DriveType == DriveType.Removable);
                }
            })
            .ToArray();
        return Task.FromResult(items);
    }

    public Task<IReadOnlyList<InstalledAppInfo>> ListInstalledAppsAsync(CancellationToken cancellationToken = default)
    {
        if (!OperatingSystem.IsWindows())
        {
            return Task.FromResult<IReadOnlyList<InstalledAppInfo>>([]);
        }

        var apps = new List<InstalledAppInfo>();
        foreach (var hive in new[]
                 {
                     @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall",
                     @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall"
                 })
        {
            try
            {
                using var key = Registry.LocalMachine.OpenSubKey(hive);
                if (key is null)
                {
                    continue;
                }

                foreach (var name in key.GetSubKeyNames().Take(400))
                {
                    using var sub = key.OpenSubKey(name);
                    var display = sub?.GetValue("DisplayName") as string;
                    if (string.IsNullOrWhiteSpace(display))
                    {
                        continue;
                    }

                    apps.Add(new InstalledAppInfo(
                        name,
                        display,
                        sub?.GetValue("UninstallString") as string,
                        sub?.GetValue("Publisher") as string ?? ""));
                }
            }
            catch (Exception)
            {
                // Registry read is best-effort.
            }
        }

        IReadOnlyList<InstalledAppInfo> ordered = apps
            .GroupBy(app => app.DisplayName, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .OrderBy(app => app.DisplayName, StringComparer.OrdinalIgnoreCase)
            .Take(300)
            .ToArray();
        return Task.FromResult(ordered);
    }

    public Task<(bool Succeeded, string Message)> UninstallAsync(InstalledAppInfo app, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(app.UninstallCommand))
        {
            return OpenUriAsync("ms-settings:appsfeatures", cancellationToken);
        }

        try
        {
            Diag.Process.Start(new Diag.ProcessStartInfo
            {
                FileName = app.UninstallCommand,
                UseShellExecute = true
            });
            return Task.FromResult((true, $"Uninstall started for {app.DisplayName}."));
        }
        catch (Exception)
        {
            return OpenUriAsync("ms-settings:appsfeatures", cancellationToken);
        }
    }

    public Task<(bool Succeeded, string Message)> OpenUriAsync(string uri, CancellationToken cancellationToken = default)
    {
        try
        {
            Diag.Process.Start(new Diag.ProcessStartInfo
            {
                FileName = uri,
                UseShellExecute = true
            });
            return Task.FromResult((true, $"Opened {uri}."));
        }
        catch (Exception ex)
        {
            return Task.FromResult((false, ex.Message));
        }
    }
}
