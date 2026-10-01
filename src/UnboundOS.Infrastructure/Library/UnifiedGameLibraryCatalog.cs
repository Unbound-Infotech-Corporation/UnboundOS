using System.Text.Json;
using UnboundOS.Core.Abstractions;
using UnboundOS.Core.Models;

namespace UnboundOS.Infrastructure.Library;

/// <summary>
/// Unified library: Steam (existing), Epic/GOG folder scans, Xbox/Store links, custom apps.
/// Read-only discovery. No account scrape.
/// </summary>
public sealed class UnifiedGameLibraryCatalog : IGameLibraryCatalog
{
    private readonly SteamGameLibraryCatalog _steam;
    private readonly JsonCustomLibraryStore _custom;
    private readonly UnifiedLibrarySettings _settings;

    public UnifiedGameLibraryCatalog(
        SteamGameLibraryCatalog? steam = null,
        JsonCustomLibraryStore? custom = null,
        UnifiedLibrarySettings? settings = null)
    {
        _steam = steam ?? new SteamGameLibraryCatalog();
        _custom = custom ?? new JsonCustomLibraryStore();
        _settings = settings ?? new UnifiedLibrarySettings();
    }

    public async Task<IReadOnlyList<LibraryGame>> DiscoverAsync(CancellationToken cancellationToken = default)
    {
        var games = new List<LibraryGame>();
        games.AddRange(await _steam.DiscoverAsync(cancellationToken).ConfigureAwait(false));
        games.AddRange(DiscoverEpic(cancellationToken));
        games.AddRange(DiscoverGog(cancellationToken));
        games.AddRange(XboxStoreLinks());
        foreach (var entry in await _custom.ListAsync(cancellationToken).ConfigureAwait(false))
        {
            games.Add(new LibraryGame(
                entry.Id,
                entry.DisplayName,
                GameStore.Custom,
                Path.GetDirectoryName(entry.ExecutablePath),
                entry.LaunchUri ?? "",
                entry.ExecutablePath,
                Path.GetFileNameWithoutExtension(entry.ExecutablePath),
                null,
                string.IsNullOrWhiteSpace(entry.DisplayName) ? [] : [entry.DisplayName.Replace(" ", "", StringComparison.Ordinal)]));
        }

        return games
            .GroupBy(game => game.Id, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .OrderBy(game => game.DisplayName, StringComparer.CurrentCultureIgnoreCase)
            .ToArray();
    }

    private IEnumerable<LibraryGame> DiscoverEpic(CancellationToken cancellationToken)
    {
        foreach (var folder in _settings.EpicManifestFolders)
        {
            if (!Directory.Exists(folder))
            {
                continue;
            }

            foreach (var file in Directory.EnumerateFiles(folder, "*.item"))
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (TryReadEpic(file) is { } game)
                {
                    yield return game;
                }
            }
        }
    }

    private IEnumerable<LibraryGame> DiscoverGog(CancellationToken cancellationToken)
    {
        foreach (var folder in _settings.GogGameFolders)
        {
            if (!Directory.Exists(folder))
            {
                continue;
            }

            foreach (var file in Directory.EnumerateFiles(folder, "goggame-*.info", SearchOption.AllDirectories))
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (TryReadGog(file) is { } game)
                {
                    yield return game;
                }
            }
        }
    }

    private static IEnumerable<LibraryGame> XboxStoreLinks() =>
    [
        new(
            "xbox-app",
            "Xbox app",
            GameStore.Xbox,
            null,
            "xbox:",
            null,
            null,
            null,
            ["Xbox"]),
        new(
            "ms-store",
            "Microsoft Store",
            GameStore.Xbox,
            null,
            "ms-windows-store://home",
            null,
            null,
            null,
            ["Store"])
    ];

    private static LibraryGame? TryReadEpic(string path)
    {
        try
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(path));
            var root = doc.RootElement;
            var name = ReadString(root, "DisplayName");
            var app = ReadString(root, "AppName") ?? ReadString(root, "MainGameAppName");
            var install = ReadString(root, "InstallLocation");
            var exe = ReadString(root, "LaunchExecutable");
            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(app))
            {
                return null;
            }

            var exePath = string.IsNullOrWhiteSpace(install) || string.IsNullOrWhiteSpace(exe)
                ? null
                : Path.Combine(install, exe);
            return new LibraryGame(
                "epic:" + app,
                name,
                GameStore.Epic,
                install,
                $"com.epicgames.launcher://apps/{Uri.EscapeDataString(app)}?action=launch&silent=true",
                exePath,
                Path.GetFileNameWithoutExtension(exe),
                null,
                [name.Replace(" ", "", StringComparison.Ordinal)]);
        }
        catch (Exception)
        {
            return null;
        }
    }

    private static LibraryGame? TryReadGog(string path)
    {
        try
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(path));
            var root = doc.RootElement;
            var name = ReadString(root, "name");
            var gameId = ReadString(root, "gameId") ?? Path.GetFileNameWithoutExtension(path);
            if (string.IsNullOrWhiteSpace(name))
            {
                return null;
            }

            return new LibraryGame(
                "gog:" + gameId,
                name,
                GameStore.Gog,
                Path.GetDirectoryName(path),
                $"goggalaxy://openGameView/{gameId}",
                null,
                null,
                null,
                [name.Replace(" ", "", StringComparison.Ordinal)]);
        }
        catch (Exception)
        {
            return null;
        }
    }

    private static string? ReadString(JsonElement root, string name) =>
        root.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;
}

public sealed class UnifiedLibrarySettings
{
    public IReadOnlyList<string> EpicManifestFolders { get; init; } = DefaultEpic();
    public IReadOnlyList<string> GogGameFolders { get; init; } = DefaultGog();

    private static IReadOnlyList<string> DefaultEpic()
    {
        if (!OperatingSystem.IsWindows())
        {
            return [];
        }

        var programData = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
        return [Path.Combine(programData, "Epic", "EpicGamesLauncher", "Data", "Manifests")];
    }

    private static IReadOnlyList<string> DefaultGog()
    {
        if (!OperatingSystem.IsWindows())
        {
            return [];
        }

        var pf = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
        return
        [
            Path.Combine(pf, "GOG Galaxy", "Games"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "GOG Galaxy", "Games")
        ];
    }
}
