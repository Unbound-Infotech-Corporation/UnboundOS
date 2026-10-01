using System.Net.NetworkInformation;
using Microsoft.Win32;
using UnboundOS.Core;
using UnboundOS.Core.Abstractions;
using UnboundOS.Core.Diagnostics;
using UnboundOS.Infrastructure.Startup;
using UnboundOS.Infrastructure.Updates;

namespace UnboundOS.Infrastructure.Diagnostics;

public sealed class OsHealthCheckService : IHealthCheckService
{
    public const string AutostartValueName = WindowsShellAutostart.ValueName;

    public async Task<HealthReport> RunAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var measure = PerfLog.Measure("health.run");

        var stamp = DateTimeOffset.UtcNow;
        var logPath = Path.Combine(
            UnboundPaths.Root,
            $"health-{stamp.ToLocalTime():yyyyMMdd-HHmmss}.txt");

        var checks = new List<HealthCheck>
        {
            ProbeNetwork(),
            ProbeGpu(),
            ProbeAutostart(),
            ProbeUpdateGuard(),
            ProbeDisk()
        };

        var report = new HealthReport(stamp, checks, logPath);
        await File.WriteAllTextAsync(logPath, report.ToLogText(), cancellationToken).ConfigureAwait(false);

        var latest = Path.Combine(UnboundPaths.Root, "health-latest.txt");
        await File.WriteAllTextAsync(latest, report.ToLogText(), cancellationToken).ConfigureAwait(false);

        return report;
    }

    private static HealthCheck ProbeNetwork()
    {
        try
        {
            var available = NetworkInterface.GetIsNetworkAvailable();
            var up = NetworkInterface.GetAllNetworkInterfaces()
                .Where(nic =>
                    nic.NetworkInterfaceType is not NetworkInterfaceType.Loopback
                    && nic.NetworkInterfaceType is not NetworkInterfaceType.Tunnel
                    && nic.OperationalStatus == OperationalStatus.Up)
                .Select(nic => nic.Name)
                .Take(4)
                .ToArray();

            var ok = available && up.Length > 0;
            var detail = ok
                ? $"Up ({string.Join(", ", up)})"
                : "No active non-loopback interface. Plug Ethernet or use the offline NIC pack, then retry.";
            return new("network", "Network", ok, detail);
        }
        catch (Exception ex)
        {
            return new("network", "Network", false, ex.Message);
        }
    }

    private static HealthCheck ProbeGpu()
    {
        if (!OperatingSystem.IsWindows())
        {
            return new("gpu", "GPU driver", false, "GPU probe runs on Windows (Win32_VideoController).");
        }

        try
        {
            using var searcher = new System.Management.ManagementObjectSearcher(
                "SELECT Name, DriverVersion, PNPDeviceID FROM Win32_VideoController");
            using var results = searcher.Get();
            foreach (var item in results)
            {
                using (item)
                {
                    var name = item["Name"]?.ToString() ?? "";
                    var version = item["DriverVersion"]?.ToString() ?? "";
                    if (string.IsNullOrWhiteSpace(name))
                    {
                        continue;
                    }

                    var basic = name.Contains("Basic Display", StringComparison.OrdinalIgnoreCase) ||
                                name.Contains("Microsoft Basic", StringComparison.OrdinalIgnoreCase);
                    if (basic)
                    {
                        return new("gpu", "GPU driver", false,
                            $"{name} — install the vendor GPU driver. UnboundOS does not bundle GPU packs.");
                    }

                    var extra = string.IsNullOrWhiteSpace(version) ? name : $"{name} {version}";
                    return new("gpu", "GPU driver", true, extra);
                }
            }

            return new("gpu", "GPU driver", false, "No Win32_VideoController rows. Check Device Manager.");
        }
        catch (Exception ex)
        {
            return new("gpu", "GPU driver", false, ex.Message);
        }
    }

    private static HealthCheck ProbeAutostart()
    {
        var autostart = new WindowsShellAutostart();
        var on = autostart.IsEnabled;
        var detail = on
            ? "HKCU Run UnboundOS is set. Explorer remains the Windows shell. Never Shell=."
            : "Off (default). Enable in Options → Startup audit for sign-in. Never Shell=. Explorer stays the Windows shell.";
        return new("autostart", "Shell autostart", true, detail);
    }

    private static HealthCheck ProbeUpdateGuard()
    {
        if (!OperatingSystem.IsWindows())
        {
            return new("updates", "Update Guard", false, "Update Guard policy lives in HKLM on Windows.");
        }

        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(WindowsUpdateGuardPolicy.WuPolicyKey);
            var raw = key?.GetValue("TargetReleaseVersion");
            var pinned = raw is int i && i == 1
                || raw is long l && l == 1
                || string.Equals(raw?.ToString(), "1", StringComparison.Ordinal);
            var info = key?.GetValue("TargetReleaseVersionInfo") as string;
            if (pinned)
            {
                return new("updates", "Update Guard", true,
                    $"Pinned {info ?? "current"}. Quality/LCU still from Microsoft. Defender stays on.");
            }

            return new("updates", "Update Guard", true,
                "Not applied (default). Quality updates stay on Microsoft WU. Defender stays on. Options → Updates can pin feature updates.");
        }
        catch (Exception ex)
        {
            return new("updates", "Update Guard", false, ex.Message);
        }
    }

    private static HealthCheck ProbeDisk()
    {
        try
        {
            var root = Path.GetPathRoot(Environment.SystemDirectory) ??
                       Path.GetPathRoot(Environment.CurrentDirectory);
            if (string.IsNullOrWhiteSpace(root))
            {
                return new("disk", "Free disk", false, "Could not resolve the system drive.");
            }

            var drive = new DriveInfo(root);
            if (!drive.IsReady)
            {
                return new("disk", "Free disk", false, $"{root} is not ready.");
            }

            var gb = drive.AvailableFreeSpace / (1024d * 1024d * 1024d);
            var ok = gb >= 40;
            var detail = $"{gb:0.0} GB free on {drive.Name.TrimEnd('\\')}";
            if (!ok)
            {
                detail += " — keep 40 GB+ for Windows quality updates and games.";
            }

            return new("disk", "Free disk", ok, detail);
        }
        catch (Exception ex)
        {
            return new("disk", "Free disk", false, ex.Message);
        }
    }
}
