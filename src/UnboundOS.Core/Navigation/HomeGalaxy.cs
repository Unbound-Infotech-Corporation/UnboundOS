namespace UnboundOS.Core.Navigation;

/// <summary>
/// Home is a linear galaxy. Nodes sit along a luminous bar. C# owns
/// which node is focused and whether the games overlay is open.
/// </summary>
public enum HomeIdleAction
{
    PanLeft = 0,
    PanRight = 1,
    OpenGames = 2,
    OpenSettings = 3
}

public static class HomeGalaxy
{
    /// <summary>Left-to-right cores along the ribbon.</summary>
    public static IReadOnlyList<CubeDestination> Nodes { get; } =
    [
        CubeDestination.Session,
        CubeDestination.Tools,
        CubeDestination.Mods,
        CubeDestination.Network,
        CubeDestination.Files,
        CubeDestination.Hardware
    ];

    public const float NodeSpacing = 2.85f;

    public static int Count => Nodes.Count;

    public static int Wrap(int index) => CubeBrowse.Wrap(index, Count);

    public static CubeDestination DestinationAt(int index) => Nodes[Wrap(index)];

    public static int IndexOf(CubeDestination destination)
    {
        for (var i = 0; i < Nodes.Count; i++)
        {
            if (Nodes[i] == destination)
            {
                return i;
            }
        }

        return 0;
    }

    public static CubeDestination Neighbor(CubeDestination current, int delta) =>
        DestinationAt(IndexOf(current) + delta);

    public static float NodeX(int index) => (Wrap(index) - (Count - 1) * 0.5f) * NodeSpacing;

    public static HomeIdleAction? FromTurn(CubeTurn turn) =>
        turn switch
        {
            CubeTurn.Left => HomeIdleAction.PanLeft,
            CubeTurn.Right => HomeIdleAction.PanRight,
            CubeTurn.Up => HomeIdleAction.OpenGames,
            CubeTurn.Down => HomeIdleAction.OpenSettings,
            _ => null
        };

    public static string Announce(CubeDestination node)
    {
        var info = CubeCatalog.Info(node);
        return $"{info.Title} node. Up opens the games list over the galaxy. Down opens Settings. Left and right shift nodes. Enter opens this group. Settings and Profiles are the corner glyphs.";
    }

    public static string AnnounceOverlay(CubeBrowseItem item, int index, int total) =>
        $"{item.Title}, {item.Meta}, {index + 1} of {total}. Enter launches. Left and right cycle. Escape or Down returns to the galaxy.";
}
