using Microsoft.Win32;
using UnboundOS.Core.Abstractions;
using UnboundOS.Core.Models;

namespace UnboundOS.Infrastructure.Mods;

/// <summary>
/// Discovers Steam libraries, app manifests, and locally installed Workshop item folders.
/// This service is strictly read-only and never accesses Steam credentials or account data.
/// </summary>
public sealed class SteamWorkshopCatalogService : IModCatalogService
{
    public async Task<IReadOnlyList<ModGame>> DiscoverAsync(CancellationToken cancellationToken = default)
    {
        return await Task.Run(() => Discover(cancellationToken), cancellationToken).ConfigureAwait(false);
    }

    private static IReadOnlyList<ModGame> Discover(CancellationToken cancellationToken)
    {
        var games = new List<ModGame>();

        foreach (var library in FindSteamLibraries())
        {
            cancellationToken.ThrowIfCancellationRequested();
            var steamApps = Path.Combine(library, "steamapps");
            if (!Directory.Exists(steamApps))
            {
                continue;
            }

            foreach (var manifestPath in Directory.EnumerateFiles(steamApps, "appmanifest_*.acf"))
            {
                cancellationToken.ThrowIfCancellationRequested();
                TryDiscoverGame(steamApps, manifestPath, games);
            }
        }

        return games
            .GroupBy(game => game.GameId, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.OrderByDescending(game => game.Mods.Count).First())
            .OrderBy(game => game.DisplayName, StringComparer.CurrentCultureIgnoreCase)
            .ToArray();
    }

    private static void TryDiscoverGame(string steamApps, string appManifestPath, ICollection<ModGame> games)
    {
        try
        {
            var appState = ValveKeyValuesParser.Parse(File.ReadAllText(appManifestPath)).Child("AppState");
            var appId = appState?.Value("appid");
            var name = appState?.Value("name");
            var installDir = appState?.Value("installdir");
            if (string.IsNullOrWhiteSpace(appId) || string.IsNullOrWhiteSpace(name))
            {
                return;
            }

            var workshopRoot = Path.Combine(steamApps, "workshop", "content", appId);
            var workshopManifest = Path.Combine(steamApps, "workshop", $"appworkshop_{appId}.acf");
            if (!Directory.Exists(workshopRoot) && !File.Exists(workshopManifest))
            {
                return;
            }

            var metadata = ReadWorkshopMetadata(workshopManifest);
            var mods = DiscoverItems(appId, workshopRoot, metadata);
            var gameInstallPath = string.IsNullOrWhiteSpace(installDir)
                ? null
                : Path.Combine(steamApps, "common", installDir);

            games.Add(new ModGame(
                appId,
                name,
                gameInstallPath,
                ModProvider.SteamWorkshop,
                ModCapabilities.SteamDiscoveryOnly,
                mods,
                "steam-workshop-readonly",
                appId));
        }
        catch (IOException)
        {
            // Steam may be updating a manifest while we scan. Skip it and allow refresh.
        }
        catch (UnauthorizedAccessException)
        {
            // A protected library should not break discovery in other libraries.
        }
    }

    private static IReadOnlyList<InstalledMod> DiscoverItems(
        string appId,
        string workshopRoot,
        IReadOnlyDictionary<string, WorkshopMetadata> metadata)
    {
        if (!Directory.Exists(workshopRoot))
        {
            return [];
        }

        var mods = new List<InstalledMod>();
        foreach (var directory in Directory.EnumerateDirectories(workshopRoot))
        {
            var itemId = Path.GetFileName(directory);
            if (!ulong.TryParse(itemId, out _))
            {
                continue;
            }

            metadata.TryGetValue(itemId, out var item);
            var updated = item?.UpdatedAt;
            if (updated is null)
            {
                updated = Directory.GetLastWriteTimeUtc(directory);
            }

            mods.Add(new InstalledMod(
                itemId,
                appId,
                $"Workshop Item {itemId}",
                ModProvider.SteamWorkshop,
                directory,
                true,
                mods.Count,
                [],
                [],
                ModUpdateState.Current,
                UpdatedAt: updated,
                WorkshopUrl: $"https://steamcommunity.com/sharedfiles/filedetails/?id={itemId}",
                SizeBytes: item?.SizeBytes ?? CalculateDirectorySize(directory)));
        }

        return mods.OrderBy(mod => mod.LoadOrder).ToArray();
    }

    private static IReadOnlyDictionary<string, WorkshopMetadata> ReadWorkshopMetadata(string path)
    {
        if (!File.Exists(path))
        {
            return new Dictionary<string, WorkshopMetadata>();
        }

        try
        {
            var root = ValveKeyValuesParser.Parse(File.ReadAllText(path)).Child("AppWorkshop");
            var installed = root?.Child("WorkshopItemsInstalled");
            if (installed is null)
            {
                return new Dictionary<string, WorkshopMetadata>();
            }

            return installed.Children
                .Where(child => ulong.TryParse(child.Name, out _))
                .ToDictionary(
                    child => child.Name,
                    child => new WorkshopMetadata(
                        ParseLong(child.Value("size")),
                        ParseUnixTime(child.Value("timeupdated"))),
                    StringComparer.OrdinalIgnoreCase);
        }
        catch (IOException)
        {
            return new Dictionary<string, WorkshopMetadata>();
        }
    }

    private static IEnumerable<string> FindSteamLibraries()
    {
        var roots = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        AddIfPresent(roots, ReadRegistry(Registry.CurrentUser, @"Software\Valve\Steam", "SteamPath"));
        AddIfPresent(roots, ReadRegistry(Registry.LocalMachine, @"SOFTWARE\WOW6432Node\Valve\Steam", "InstallPath"));
        AddIfPresent(roots, ReadRegistry(Registry.LocalMachine, @"SOFTWARE\Valve\Steam", "InstallPath"));

        var primaryRoots = roots.ToArray();
        foreach (var root in primaryRoots)
        {
            var librariesPath = Path.Combine(root, "steamapps", "libraryfolders.vdf");
            if (!File.Exists(librariesPath))
            {
                continue;
            }

            ValveNode? libraries = null;
            try
            {
                libraries = ValveKeyValuesParser.Parse(File.ReadAllText(librariesPath)).Child("libraryfolders");
            }
            catch (IOException)
            {
                // Keep the primary Steam root.
            }

            if (libraries is null)
            {
                continue;
            }

            foreach (var child in libraries.Children)
            {
                AddIfPresent(roots, child.Value("path"));
            }
        }

        return roots;
    }

    private static string? ReadRegistry(RegistryKey hive, string subKey, string valueName)
    {
        try
        {
            return hive.OpenSubKey(subKey)?.GetValue(valueName) as string;
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or System.Security.SecurityException)
        {
            return null;
        }
    }

    private static void AddIfPresent(ISet<string> paths, string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        try
        {
            // Normalize Steam's escaped separators without corrupting UNC roots (\\server\share).
            var normalized = path.Replace('/', '\\').Trim();
            while (normalized.Contains(@"\\", StringComparison.Ordinal)
                   && !normalized.StartsWith(@"\\", StringComparison.Ordinal))
            {
                normalized = normalized.Replace(@"\\", @"\", StringComparison.Ordinal);
            }

            if (normalized.StartsWith(@"\\", StringComparison.Ordinal))
            {
                // Keep UNC paths as-is after slash normalization; GetFullPath can rewrite them incorrectly.
                paths.Add(normalized.TrimEnd('\\'));
                return;
            }

            paths.Add(Path.GetFullPath(normalized));
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
        {
            // Ignore malformed library paths from a partial Steam install.
        }
    }

    private static long CalculateDirectorySize(string path)
    {
        try
        {
            return Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories)
                .Sum(file =>
                {
                    try { return new FileInfo(file).Length; }
                    catch { return 0L; }
                });
        }
        catch
        {
            return 0;
        }
    }

    private static long ParseLong(string? value) =>
        long.TryParse(value, out var parsed) ? parsed : 0;

    private static DateTimeOffset? ParseUnixTime(string? value) =>
        long.TryParse(value, out var seconds)
            ? DateTimeOffset.FromUnixTimeSeconds(seconds)
            : null;

    private sealed record WorkshopMetadata(long SizeBytes, DateTimeOffset? UpdatedAt);
}
