using UnboundOS.Core.Models;

namespace UnboundOS.Core.Navigation;

/// <summary>
/// Maps discovered library/tools/mods into Home category lists.
/// Games stay the Steam library. Tools and Mods use their catalogs.
/// Config nodes open an honest page list over Home.
/// </summary>
public static class CubeBrowse
{
    public static IReadOnlyList<CubeBrowseItem> ForNode(
        CubeDestination destination,
        IReadOnlyList<LibraryGame> games,
        IReadOnlyList<DesktopTool> tools,
        IReadOnlyList<ModGame> mods) =>
        destination switch
        {
            CubeDestination.Session => Games(games, tools),
            CubeDestination.Tools => Pad(Tools(tools), destination),
            CubeDestination.Mods => Pad(Mods(mods), destination),
            _ => Config(destination)
        };

    public static IReadOnlyList<CubeBrowseItem> Games(
        IReadOnlyList<LibraryGame> games,
        IReadOnlyList<DesktopTool> tools)
    {
        var items = games.Select(GameItem).ToList();
        if (items.Count > 0)
        {
            return items;
        }

        items.Add(new("session", "Session engine", "PAGE", "page", "G"));
        foreach (var id in new[] { DesktopToolIds.Steam, DesktopToolIds.Playnite })
        {
            var tool = tools.FirstOrDefault(candidate =>
                string.Equals(candidate.Id, id, StringComparison.OrdinalIgnoreCase));
            if (tool is not null)
            {
                items.Add(ToolItem(tool));
            }
        }

        return items;
    }

    public static IReadOnlyList<CubeBrowseItem> Tools(IReadOnlyList<DesktopTool> tools) =>
        tools.Select(ToolItem).ToArray();

    public static IReadOnlyList<CubeBrowseItem> Mods(IReadOnlyList<ModGame> games) =>
        games.Select(game => new CubeBrowseItem(
            game.GameId,
            game.DisplayName,
            game.ProviderLabel,
            "mod",
            Mark(game.DisplayName))).ToArray();

    /// <summary>
    /// Home SET / Options list. Keep titles in sync with SettingsViewModel.Groups.
    /// Enter still lands on the native Settings page for the real toggles.
    /// </summary>
    public static IReadOnlyList<CubeBrowseItem> Options() =>
    [
        new("motion", "Interface motion", "SET", "settings", "M"),
        new("hud", "Home HUD", "SET", "settings", "H"),
        new("skinny", "Session skinny", "SET", "settings", "K"),
        new("updates", "Updates", "SET", "settings", "U"),
        new("display", "Display", "SET", "settings", "D"),
        new("overclock", "Overclocking", "SET", "settings", "O"),
        new("startup", "Startup audit", "SET", "settings", "S"),
        new("cleanup", "Finish setup", "SET", "settings", "C")
    ];

    public static IReadOnlyList<CubeBrowseItem> Config(CubeDestination destination)
    {
        var info = CubeCatalog.Info(destination);
        return destination switch
        {
            CubeDestination.Network =>
            [
                new("network", "Network director", "PAGE", "page", "N"),
                new("split", "Prefer a game NIC", "SPLIT", "page", "S"),
                new("bulk", "Park bulk traffic", "SPLIT", "page", "B")
            ],
            CubeDestination.Files =>
            [
                new("files", "Daily folders", "PAGE", "page", "F"),
                new("explorer", "Explorer stays for anticheat", "SAFE", "page", "E")
            ],
            CubeDestination.Hardware =>
            [
                new("hardware", "This PC inventory", "PAGE", "page", "H"),
                new("hwinfo", "Optional official HWiNFO", "READ", "page", "W")
            ],
            _ => [PageItem(destination, info.Title, info.Meta)]
        };
    }

    private static IReadOnlyList<CubeBrowseItem> Pad(
        IReadOnlyList<CubeBrowseItem> items,
        CubeDestination destination)
    {
        if (items.Count > 0)
        {
            return items;
        }

        var info = CubeCatalog.Info(destination);
        return [PageItem(destination, info.Title, "PAGE")];
    }

    private static CubeBrowseItem PageItem(CubeDestination destination, string title, string meta) =>
        new(destination.ToString().ToLowerInvariant(), title, meta, "page", Mark(title));

    public static int Wrap(int index, int count)
    {
        if (count <= 0)
        {
            return 0;
        }

        return ((index % count) + count) % count;
    }

    /// <summary>
    /// Stagger before a neighbor brick springs to its new packed pose.
    /// Focused brick starts immediately. Keep in sync with cube-scene.js.
    /// </summary>
    public static double RearrangeDelaySeconds(double distanceFromFocus, bool isFocus)
    {
        if (isFocus)
        {
            return 0;
        }

        return 0.022 + Math.Clamp(distanceFromFocus, 0, 1.8) * 0.048;
    }

    /// <summary>
    /// How far a neighbor yields aside as the focused brick takes mass.
    /// Carousel (Games) pushes more than mosaic. Keep in sync with cube-scene.js.
    /// </summary>
    public static double RearrangePush(double distanceFromFocus, bool carousel)
    {
        var mag = carousel ? 0.12 : 0.07;
        return mag * (0.35 + Math.Exp(-(distanceFromFocus * distanceFromFocus) / 0.55));
    }

    private static CubeBrowseItem GameItem(LibraryGame game) =>
        new(game.Id, game.DisplayName, game.Store.ToString().ToUpperInvariant(), "game", Mark(game.DisplayName));

    private static CubeBrowseItem ToolItem(DesktopTool tool) =>
        new(tool.Id, tool.DisplayName, tool.StatusLabel, "tool", Mark(tool.DisplayName));

    private static string Mark(string title)
    {
        var trimmed = title.Trim();
        return string.IsNullOrEmpty(trimmed) ? "+" : char.ToUpperInvariant(trimmed[0]).ToString();
    }
}
