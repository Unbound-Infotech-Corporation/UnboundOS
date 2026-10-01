using System.Text.Json;
using Diag = System.Diagnostics;
using UnboundOS.Core.Abstractions;
using UnboundOS.Core.Models;

namespace UnboundOS.Infrastructure.Startup;

public sealed class WindowsAppLauncherCatalog(IGameLibraryCatalog games) : IAppLauncherCatalog
{
    public async Task<IReadOnlyList<LauncherApp>> DiscoverAsync(CancellationToken cancellationToken = default)
    {
        var items = new List<LauncherApp>();
        items.AddRange(StartMenuShortcuts());
        items.AddRange(await SteamGamesAsync(cancellationToken).ConfigureAwait(false));
        items.AddRange(EpicGames());
        items.AddRange(GogGames());
        items.AddRange(StoreLinks());
        return items
            .GroupBy(app => app.Id, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .OrderBy(app => app.Title, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public Task<(bool Succeeded, string Message)> LaunchAsync(LauncherApp app, CancellationToken cancellationToken = default)
    {
        try
        {
            Diag.Process.Start(new Diag.ProcessStartInfo
            {
                FileName = app.Target,
                UseShellExecute = true
            });
            return Task.FromResult((true, $"Launched {app.Title}."));
        }
        catch (Exception ex)
        {
            return Task.FromResult((false, ex.Message));
        }
    }

    private async Task<IEnumerable<LauncherApp>> SteamGamesAsync(CancellationToken cancellationToken)
    {
        try
        {
            var list = await games.DiscoverAsync(cancellationToken).ConfigureAwait(false);
            return list.Select(game => new LauncherApp(
                "steam-" + game.Id,
                game.DisplayName,
                "Steam",
                game.LaunchUri,
                "game"));
        }
        catch (Exception)
        {
            return [];
        }
    }

    private static IEnumerable<LauncherApp> StartMenuShortcuts()
    {
        var roots = new[]
        {
            Environment.GetFolderPath(Environment.SpecialFolder.CommonStartMenu),
            Environment.GetFolderPath(Environment.SpecialFolder.StartMenu)
        };

        foreach (var root in roots.Where(path => !string.IsNullOrWhiteSpace(path) && Directory.Exists(path)))
        {
            IEnumerable<string> links;
            try
            {
                links = Directory.EnumerateFiles(root, "*.lnk", SearchOption.AllDirectories);
            }
            catch (Exception)
            {
                continue;
            }

            foreach (var link in links.Take(400))
            {
                var name = Path.GetFileNameWithoutExtension(link);
                if (string.IsNullOrWhiteSpace(name))
                {
                    continue;
                }

                yield return new LauncherApp("lnk-" + link.ToLowerInvariant(), name, "Start Menu", link, "app");
            }
        }
    }

    private static IEnumerable<LauncherApp> EpicGames()
    {
        var manifests = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            "Epic", "EpicGamesLauncher", "Data", "Manifests");
        if (!Directory.Exists(manifests))
        {
            yield break;
        }

        foreach (var file in Directory.EnumerateFiles(manifests, "*.item"))
        {
            JsonDocument doc;
            try
            {
                doc = JsonDocument.Parse(File.ReadAllText(file));
            }
            catch (Exception)
            {
                continue;
            }

            using (doc)
            {
                var name = doc.RootElement.TryGetProperty("DisplayName", out var display)
                    ? display.GetString()
                    : null;
                var appName = doc.RootElement.TryGetProperty("AppName", out var id)
                    ? id.GetString()
                    : Path.GetFileNameWithoutExtension(file);
                if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(appName))
                {
                    continue;
                }

                yield return new LauncherApp(
                    "epic-" + appName,
                    name,
                    "Epic",
                    "com.epicgames.launcher://apps/" + appName + "?action=launch&silent=true",
                    "game");
            }
        }
    }

    private static IEnumerable<LauncherApp> GogGames()
    {
        var gog = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            "GOG.com", "Galaxy", "Applications");
        if (!Directory.Exists(gog))
        {
            yield break;
        }

        foreach (var file in Directory.EnumerateFiles(gog, "*.info", SearchOption.AllDirectories).Take(80))
        {
            var name = Path.GetFileNameWithoutExtension(file);
            yield return new LauncherApp("gog-" + name, name, "GOG", file, "game");
        }
    }

    private static IEnumerable<LauncherApp> StoreLinks()
    {
        yield return new LauncherApp("store-xbox", "Xbox app", "Xbox", "msxbox://", "store");
        yield return new LauncherApp("store-ms", "Microsoft Store", "Store", "ms-windows-store://home", "store");
        yield return new LauncherApp("store-steam", "Steam store", "Steam", "steam://store", "store");
        yield return new LauncherApp("store-epic", "Epic Games Store", "Epic", "com.epicgames.launcher://store", "store");
    }
}
