using Microsoft.Win32;
using UnboundOS.Core.Abstractions;
using UnboundOS.Core.Brand;
using UnboundOS.Core.Models;

namespace UnboundOS.Infrastructure.Updates;

/// <summary>
/// Quality-yes / feature-deferred Windows Update policy. Each PC downloads
/// from Microsoft. UnboundOS never redistributes .msu/.cab.
/// </summary>
public sealed class WindowsUpdateGuardPolicy : IUpdateGuardPolicy
{
    public const string LegalNotice =
        "Downloads come from Microsoft. UnboundOS does not redistribute Windows patches. Elevation may be required.";

    public string ShellVersion => $"{UnboundProduct.ShellVersion} ({UnboundProduct.ShellChannel})";

    public UpdateGuardTarget DescribeTarget() => ReadTarget();

    public Task<(bool Succeeded, string Message)> TryApplyUpdateGuardAsync(CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        var target = ReadTarget();
        if (!OperatingSystem.IsWindows())
        {
            return Task.FromResult((true,
                WithLegal($"Update Guard preference saved. Apply on Windows: quality/LCU on, feature updates deferred (pin {target.ProductVersion} {target.DisplayVersion}). Windows Update service, Defender, and VBS stay on.")));
        }

        try
        {
            WriteDword(Registry.LocalMachine, WuPolicyPath, "TargetReleaseVersion", 1);
            WriteString(Registry.LocalMachine, WuPolicyPath, "TargetReleaseVersionInfo", target.DisplayVersion);
            WriteString(Registry.LocalMachine, WuPolicyPath, "ProductVersion", target.ProductVersion);
            WriteDword(Registry.LocalMachine, WuPolicyPath, "DeferFeatureUpdates", 1);
            WriteDword(Registry.LocalMachine, WuPolicyPath, "DeferFeatureUpdatesPeriodInDays", 365);
            WriteDword(Registry.LocalMachine, WuPolicyPath, "ManagePreviewBuilds", 1);
            WriteDword(Registry.LocalMachine, WuPolicyPath, "ManagePreviewBuildsPolicyValue", 0);
            DeleteValue(Registry.LocalMachine, WuPolicyPath, "DeferQualityUpdates");
            DeleteValue(Registry.LocalMachine, WuPolicyPath, "DeferQualityUpdatesPeriodInDays");

            WriteDword(Registry.LocalMachine, AuPath, "NoAutoUpdate", 0);
            WriteDword(Registry.LocalMachine, AuPath, "AUOptions", 4);
            WriteDword(Registry.LocalMachine, AuPath, "NoAutoRebootWithLoggedOnUsers", 1);

            WriteDword(Registry.LocalMachine, UxSettingsPath, "IsContinuousInnovationOptedIn", 0);

            var home = target.HomeEdition
                ? " Home edition is best-effort — Microsoft may ignore these policies near end of servicing."
                : "";
            return Task.FromResult((true, WithLegal(
                $"Update Guard applied. Monthly security quality from Microsoft; feature/optional churn blocked. Pinned {target.ProductVersion} {target.DisplayVersion}.{home} Session enter does not flip this. Never reboot mid Competitive session.")));
        }
        catch (UnauthorizedAccessException)
        {
            return Task.FromResult((false, WithLegal(
                "Update Guard needs an elevated write to HKLM. Preference is saved; run UnboundOS elevated to apply quality-yes / feature-deferred.")));
        }
        catch (Exception ex)
        {
            return Task.FromResult((false, WithLegal($"Could not apply Update Guard: {ex.Message}")));
        }
    }

    public Task<(bool Succeeded, string Message)> TryClearUpdateGuardAsync(CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        if (!OperatingSystem.IsWindows())
        {
            return Task.FromResult((true, WithLegal(
                "Update Guard preference cleared. Apply restore on Windows to remove feature-deferral policy. Quality updates still download from Microsoft. Windows Update service, Defender, and VBS were not disabled.")));
        }

        try
        {
            DeleteValue(Registry.LocalMachine, WuPolicyPath, "TargetReleaseVersion");
            DeleteValue(Registry.LocalMachine, WuPolicyPath, "TargetReleaseVersionInfo");
            DeleteValue(Registry.LocalMachine, WuPolicyPath, "ProductVersion");
            DeleteValue(Registry.LocalMachine, WuPolicyPath, "DeferFeatureUpdates");
            DeleteValue(Registry.LocalMachine, WuPolicyPath, "DeferFeatureUpdatesPeriodInDays");
            DeleteValue(Registry.LocalMachine, WuPolicyPath, "ManagePreviewBuilds");
            DeleteValue(Registry.LocalMachine, WuPolicyPath, "ManagePreviewBuildsPolicyValue");
            DeleteValue(Registry.LocalMachine, AuPath, "NoAutoRebootWithLoggedOnUsers");
            DeleteValue(Registry.LocalMachine, UxSettingsPath, "IsContinuousInnovationOptedIn");
            WriteDword(Registry.LocalMachine, AuPath, "NoAutoUpdate", 0);

            return Task.FromResult((true, WithLegal(
                "Update Guard restored. Feature-deferral policy removed. Windows Update service, Defender, and VBS were not disabled.")));
        }
        catch (UnauthorizedAccessException)
        {
            return Task.FromResult((false, WithLegal(
                "Update Guard restore needs an elevated write to HKLM. Preference is saved; run UnboundOS elevated to clear the policy.")));
        }
        catch (Exception ex)
        {
            return Task.FromResult((false, WithLegal($"Could not clear Update Guard: {ex.Message}")));
        }
    }

    public Task<UpdateGuardStatus> ProbeQualityStatusAsync(CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        var target = ReadTarget();
        if (!OperatingSystem.IsWindows())
        {
            return Task.FromResult(new UpdateGuardStatus
            {
                QualityLabel = "unknown",
                Detail = "Windows quality status is unknown on this host. Check Settings → Windows Update on the PC.",
                ShellVersion = ShellVersion,
                TargetRelease = $"{target.ProductVersion} {target.DisplayVersion}".Trim()
            });
        }

        var reboot = RebootPending();
        return Task.FromResult(new UpdateGuardStatus
        {
            QualityLabel = reboot ? "needs restart" : "unknown",
            Detail = reboot
                ? "Windows reports a pending restart. UnboundOS will not reboot during a live session."
                : "Quality status is best-effort. Use Check for quality updates to open Microsoft Windows Update.",
            ShellVersion = ShellVersion,
            TargetRelease = $"{target.ProductVersion} {target.DisplayVersion}".Trim()
        });
    }

    public Task<(bool Succeeded, string Message)> OpenWindowsUpdateSettingsAsync(CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        if (!OperatingSystem.IsWindows())
        {
            return Task.FromResult((true, WithLegal(
                "Open Settings → Windows Update on the PC to search for monthly quality updates.")));
        }

        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("ms-settings:windowsupdate")
            {
                UseShellExecute = true
            });
            return Task.FromResult((true, WithLegal(
                "Opened Microsoft Windows Update. Search and install stay on this PC — UnboundOS does not push .msu files.")));
        }
        catch (Exception ex)
        {
            return Task.FromResult((false, WithLegal($"Could not open Windows Update: {ex.Message}")));
        }
    }

    private static string WithLegal(string lead) => $"{lead} {LegalNotice}";

    private static UpdateGuardTarget ReadTarget()
    {
        if (!OperatingSystem.IsWindows())
        {
            return new UpdateGuardTarget
            {
                ProductVersion = "Windows 11",
                DisplayVersion = "24H2",
                Edition = "unknown",
                HomeEdition = false
            };
        }

        var display = ReadString(Registry.LocalMachine, NtCurrent, "DisplayVersion")
            ?? ReadString(Registry.LocalMachine, NtCurrent, "ReleaseId")
            ?? "24H2";
        var edition = ReadString(Registry.LocalMachine, NtCurrent, "EditionID") ?? "";
        var buildText = ReadString(Registry.LocalMachine, NtCurrent, "CurrentBuild")
            ?? ReadString(Registry.LocalMachine, NtCurrent, "CurrentBuildNumber")
            ?? "0";
        _ = int.TryParse(buildText, out var build);
        var product = build >= 22000 || (ReadString(Registry.LocalMachine, NtCurrent, "ProductName")
            ?.Contains("Windows 11", StringComparison.OrdinalIgnoreCase) ?? false)
            ? "Windows 11"
            : "Windows 10";
        var home = edition.Contains("Core", StringComparison.OrdinalIgnoreCase)
            || edition.Contains("Home", StringComparison.OrdinalIgnoreCase);

        return new UpdateGuardTarget
        {
            ProductVersion = product,
            DisplayVersion = display,
            Edition = edition,
            HomeEdition = home
        };
    }

    private static bool RebootPending()
    {
        using var reboot = Registry.LocalMachine.OpenSubKey(
            @"SOFTWARE\Microsoft\Windows\CurrentVersion\WindowsUpdate\Auto Update\RebootRequired");
        using var cbs = Registry.LocalMachine.OpenSubKey(
            @"SOFTWARE\Microsoft\Windows\CurrentVersion\Component Based Servicing\RebootPending");
        return reboot is not null || cbs is not null;
    }

    private static string? ReadString(RegistryKey root, string path, string name)
    {
        using var key = root.OpenSubKey(path);
        return key?.GetValue(name) as string;
    }

    private static void WriteDword(RegistryKey root, string path, string name, int value)
    {
        using var key = root.CreateSubKey(path, true);
        key?.SetValue(name, value, RegistryValueKind.DWord);
    }

    private static void WriteString(RegistryKey root, string path, string name, string value)
    {
        using var key = root.CreateSubKey(path, true);
        key?.SetValue(name, value, RegistryValueKind.String);
    }

    private static void DeleteValue(RegistryKey root, string path, string name)
    {
        using var key = root.OpenSubKey(path, writable: true);
        if (key is null)
        {
            return;
        }

        key.DeleteValue(name, throwOnMissingValue: false);
    }

    private const string NtCurrent = @"SOFTWARE\Microsoft\Windows NT\CurrentVersion";
    private const string WuPolicyPath = @"SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate";
    private const string AuPath = @"SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate\AU";
    private const string UxSettingsPath = @"SOFTWARE\Microsoft\WindowsUpdate\UX\Settings";
}
