using System.Diagnostics;
using UnboundOS.Core.Abstractions;
using UnboundOS.Core.Models;

namespace UnboundOS.Infrastructure.Process;

public sealed class ProcessGuardian : IProcessGuardian
{
    private static readonly HashSet<string> HardProtect = new(StringComparer.OrdinalIgnoreCase)
    {
        "System", "Registry", "smss", "csrss", "wininit", "services", "lsass", "svchost",
        "dwm", "explorer", "fontdrvhost", "conhost", "RuntimeBroker", "SearchHost",
        "ShellExperienceHost", "StartMenuExperienceHost", "TextInputHost", "sihost",
        "taskhostw", "SecurityHealthService", "MsMpEng", "UnboundOS.App"
    };

    public Task<IReadOnlyList<string>> ListRunningSuspectsAsync(IEnumerable<string> denylist, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        var names = Normalize(denylist);
        var hits = new List<string>();

        foreach (var proc in System.Diagnostics.Process.GetProcesses())
        {
            try
            {
                var name = proc.ProcessName;
                if (!string.IsNullOrWhiteSpace(name) && names.Contains(name))
                {
                    hits.Add(name);
                }
            }
            catch
            {
                // Ignore inaccessible processes.
            }
            finally
            {
                proc.Dispose();
            }
        }

        return Task.FromResult<IReadOnlyList<string>>(
            hits.Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(n => n, StringComparer.OrdinalIgnoreCase)
                .ToList());
    }

    public Task<IReadOnlyList<TerminatedProcessRecord>> TerminateAsync(
        IEnumerable<string> processNames,
        IEnumerable<string> protectNames,
        CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        var targets = Normalize(processNames);
        var protect = Normalize(protectNames);
        protect.UnionWith(HardProtect);

        var terminated = new List<TerminatedProcessRecord>();

        foreach (var proc in System.Diagnostics.Process.GetProcesses())
        {
            ct.ThrowIfCancellationRequested();
            string? name = null;
            try
            {
                name = proc.ProcessName;
                if (string.IsNullOrWhiteSpace(name))
                {
                    continue;
                }

                if (!targets.Contains(name) || protect.Contains(name))
                {
                    continue;
                }

                var record = new TerminatedProcessRecord
                {
                    ProcessName = name,
                    ProcessId = proc.Id,
                    FileName = SafeMainModule(proc)
                };

                proc.Kill(entireProcessTree: true);
                terminated.Add(record);
            }
            catch
            {
                // Access denied / already exited — skip; session engine reports partial success.
            }
            finally
            {
                proc.Dispose();
            }
        }

        return Task.FromResult<IReadOnlyList<TerminatedProcessRecord>>(terminated);
    }

    public int CountBackgroundSuspects(IEnumerable<string> denylist)
    {
        var names = Normalize(denylist);
        return System.Diagnostics.Process.GetProcesses()
            .Count(p =>
            {
                try
                {
                    return names.Contains(p.ProcessName);
                }
                catch
                {
                    return false;
                }
                finally
                {
                    p.Dispose();
                }
            });
    }

    private static HashSet<string> Normalize(IEnumerable<string> names) =>
        names
            .Where(n => !string.IsNullOrWhiteSpace(n))
            .Select(n => n.Trim().Replace(".exe", string.Empty, StringComparison.OrdinalIgnoreCase))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

    private static string? SafeMainModule(System.Diagnostics.Process p)
    {
        try { return p.MainModule?.FileName; }
        catch { return null; }
    }
}
