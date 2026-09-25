using UnboundOS.Core;
using UnboundOS.Core.Abstractions;
using UnboundOS.Core.Models;

namespace UnboundOS.Infrastructure.Setup;

public sealed record SetupCleanupSettings
{
    public string Root { get; init; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Unbound Infotech Corporation",
        "UnboundOS",
        "setup-leftovers");
}

/// <summary>
/// Cheap leftover wipe for known Unbound staging folders. Full OOBE wipe is on the image.
/// Spec: docs/os-spec.md §1.
/// </summary>
public sealed class SetupCleanupService(SetupCleanupSettings? settings = null) : ISetupCleanup
{
    private readonly SetupCleanupSettings _settings = settings ?? new SetupCleanupSettings();

    public Task<SetupCleanupResult> CleanLeftoversAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var root = _settings.Root;
        Directory.CreateDirectory(root);

        var removed = 0;
        removed += DeleteIfExists(Path.Combine(root, "UnboundOS-Setup"));
        removed += DeleteIfExists(Path.Combine(root, "installer-leftovers"));
        foreach (var log in SafeEnumerate(root, "UnboundOS-*.log"))
        {
            try
            {
                File.Delete(log);
                removed++;
            }
            catch
            {
                // best effort
            }
        }

        var pack = Path.Combine(root, "OfflineNicDrivers");
        var onlineOk = Path.Combine(root, "online-ok.flag");
        if (File.Exists(onlineOk))
        {
            removed += DeleteIfExists(pack);
        }

        var message = removed == 0
            ? $"{OsProductCopy.CleanupHonesty} Nothing to remove in {root}."
            : $"Removed {removed} leftover item(s). {OsProductCopy.CleanupHonesty}";

        return Task.FromResult(new SetupCleanupResult(true, removed, message));
    }

    private static int DeleteIfExists(string path)
    {
        try
        {
            if (Directory.Exists(path))
            {
                Directory.Delete(path, recursive: true);
                return 1;
            }

            if (File.Exists(path))
            {
                File.Delete(path);
                return 1;
            }
        }
        catch
        {
            return 0;
        }

        return 0;
    }

    private static IEnumerable<string> SafeEnumerate(string root, string pattern)
    {
        try
        {
            return Directory.EnumerateFiles(root, pattern);
        }
        catch
        {
            return [];
        }
    }
}
