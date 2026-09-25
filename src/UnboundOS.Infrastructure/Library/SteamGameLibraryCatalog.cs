using UnboundOS.Core.Abstractions;
using UnboundOS.Core.Models;
using UnboundOS.Infrastructure.Mods;

namespace UnboundOS.Infrastructure.Library;

/// <summary>
/// Installed Steam games from local appmanifest files. Read-only; no account scrape.
/// </summary>
public sealed class SteamGameLibraryCatalog(SteamLibraryDiscoverySettings? settings = null) : IGameLibraryCatalog
{
    private static readonly HashSet<string> RuntimeAppIds = new(StringComparer.OrdinalIgnoreCase)
    {
        "228980", "1070560", "1391110", "1493710", "1628350"
    };

    private readonly SteamLibraryDiscoverySettings _settings = settings ?? new SteamLibraryDiscoverySettings();

    public Task<IReadOnlyList<LibraryGame>> DiscoverAsync(CancellationToken cancellationToken = default) =>
        Task.Run(() => Discover(cancellationToken), cancellationToken);

    internal IReadOnlyList<LibraryGame> Discover(CancellationToken cancellationToken = default)
    {
        var games = new List<LibraryGame>();
        foreach (var library in SteamLibraryLocator.Find(
                     _settings.ExtraLibraryFolders,
                     _settings.UseDefaultWindowsLocations))
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
                if (TryRead(steamApps, manifestPath) is { } game)
                {
                    games.Add(game);
                }
            }
        }

        return games
            .GroupBy(game => game.Id, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .OrderBy(game => game.DisplayName, StringComparer.CurrentCultureIgnoreCase)
            .ToArray();
    }

    private static LibraryGame? TryRead(string steamApps, string manifestPath)
    {
        try
        {
            var appState = ValveKeyValuesParser.Parse(File.ReadAllText(manifestPath)).Child("AppState");
            var appId = appState?.Value("appid");
            var name = appState?.Value("name");
            var installDir = appState?.Value("installdir");
            var stateFlags = appState?.Value("StateFlags");
            if (string.IsNullOrWhiteSpace(appId) || string.IsNullOrWhiteSpace(name))
            {
                return null;
            }

            if (IsRuntime(appId, name))
            {
                return null;
            }

            if (!string.IsNullOrWhiteSpace(stateFlags) &&
                int.TryParse(stateFlags, out var flags) &&
                (flags & 4) == 0)
            {
                return null;
            }

            var installPath = string.IsNullOrWhiteSpace(installDir)
                ? null
                : Path.Combine(steamApps, "common", installDir);

            return new LibraryGame(
                appId,
                name,
                GameStore.Steam,
                installPath,
                $"steam://rungameid/{appId}",
                null,
                null,
                null,
                [name.Replace(" ", "", StringComparison.Ordinal)]);
        }
        catch (IOException)
        {
            return null;
        }
        catch (UnauthorizedAccessException)
        {
            return null;
        }
    }

    private static bool IsRuntime(string appId, string name)
    {
        if (RuntimeAppIds.Contains(appId))
        {
            return true;
        }

        var lower = name.ToLowerInvariant();
        return lower.Contains("steamworks", StringComparison.Ordinal) ||
               lower.Contains("proton", StringComparison.Ordinal) ||
               lower.Contains("runtime", StringComparison.Ordinal) ||
               lower.Contains("steam linux", StringComparison.Ordinal) ||
               lower.Contains("source sdk", StringComparison.Ordinal) ||
               lower.Contains("dedicated server", StringComparison.Ordinal);
    }
}
