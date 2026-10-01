using UnboundOS.Core;
using UnboundOS.Core.Abstractions;
using UnboundOS.Core.Shell;

namespace UnboundOS.Infrastructure.Diagnostics;

public sealed class DiagnosticsExportService : IDiagnosticsExport
{
    private readonly string _root;

    public DiagnosticsExportService(string? root = null)
    {
        _root = root ?? UnboundPaths.Root;
    }

    public Task<(bool Succeeded, string Message, string Path)> ExportAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var stamp = DateTimeOffset.Now.ToString("yyyyMMdd-HHmmss");
        var dest = System.IO.Path.Combine(_root, "diagnostics-" + stamp);
        Directory.CreateDirectory(dest);

        CopyIfExists(System.IO.Path.Combine(_root, "health-latest.txt"), dest);
        CopyIfExists(System.IO.Path.Combine(_root, XboxModePolicy.PreferenceFileName), dest);
        CopyIfExists(System.IO.Path.Combine(_root, "last-session.json"), dest);
        CopyIfExists(System.IO.Path.Combine(_root, "watchdog.log"), dest);
        CopyIfExists(System.IO.Path.Combine(_root, "game-profiles.json"), dest);
        File.WriteAllText(
            System.IO.Path.Combine(dest, "honesty.txt"),
            string.Join(Environment.NewLine, [
                OsProductCopy.XboxModeHonesty,
                OsProductCopy.AnticheatHonesty,
                OsProductCopy.GuideHonesty,
                OsProductCopy.ControllerHonesty,
                OsProductCopy.HealthHonesty
            ]));

        return Task.FromResult((true, "Diagnostics folder written. No secrets are collected.", dest));
    }

    private static void CopyIfExists(string source, string destDir)
    {
        if (File.Exists(source))
        {
            File.Copy(source, System.IO.Path.Combine(destDir, System.IO.Path.GetFileName(source)), overwrite: true);
        }
    }
}
