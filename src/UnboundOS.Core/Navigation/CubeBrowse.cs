using UnboundOS.Core.Models;

namespace UnboundOS.Core.Navigation;

/// <summary>
/// Maps discovered library/tools/mods into cube browse blocks.
/// Games stay a carousel. Tools and Mods become a mosaic when items exist.
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
