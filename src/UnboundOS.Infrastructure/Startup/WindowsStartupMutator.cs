using Microsoft.Win32;
using UnboundOS.Core.Abstractions;
using UnboundOS.Core.Models;

namespace UnboundOS.Infrastructure.Startup;

/// <summary>
/// Disables user-level Review items only (HKCU Run, Startup folder).
/// HKLM and Scheduled Tasks stay report-only in this slice.
/// </summary>
public sealed class WindowsStartupMutator : IStartupMutator
{
    public Task<string> DisableAsync(StartupEntry entry, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(entry);

        if (entry.Disposition == StartupDisposition.Protected ||
            entry.Disposition == StartupDisposition.Pinned ||
            StartupPolicy.IsProtected($"{entry.Name} {entry.Command}"))
        {
            return Task.FromResult("Skipped protected or pinned startup item.");
        }

        try
        {
            switch (entry.Source)
            {
                case StartupSource.RunKeyUser:
                    using (var key = Registry.CurrentUser.OpenSubKey(
                               @"Software\Microsoft\Windows\CurrentVersion\Run",
                               writable: true))
                    {
                        key?.DeleteValue(entry.Name, throwOnMissingValue: false);
                    }

                    return Task.FromResult($"Disabled HKCU Run value '{entry.Name}'.");
                case StartupSource.StartupFolder:
                    var userStartup = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
                    if (string.IsNullOrWhiteSpace(userStartup) ||
                        !entry.Command.StartsWith(userStartup, StringComparison.OrdinalIgnoreCase))
                    {
                        return Task.FromResult(
                            "Report-only in this slice (common Startup folder). See docs/os-spec.md.");
                    }

                    if (File.Exists(entry.Command))
                    {
                        File.Delete(entry.Command);
                        return Task.FromResult($"Disabled Startup folder item '{entry.Name}'.");
                    }

                    return Task.FromResult("Startup folder item already gone.");
                default:
                    return Task.FromResult(
                        "Report-only in this slice (HKLM / Scheduled Tasks). See docs/os-spec.md and docs/startup-audit-task.xml.");
            }
        }
        catch (Exception error)
        {
            return Task.FromResult($"Could not disable '{entry.Name}': {error.Message}");
        }
    }
}
