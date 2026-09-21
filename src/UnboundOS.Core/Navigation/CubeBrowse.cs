using UnboundOS.Core.Models;

namespace UnboundOS.Core.Navigation;

/// <summary>
/// Maps discovered library/tools/mods into cube browse blocks.
/// Games stay a carousel. Tools and Mods become a mosaic when items exist.
/// Focus cycling uses staggered springs (see RearrangeDelaySeconds / RearrangePush).
/// </summary>
public static class CubeBrowse
{
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
