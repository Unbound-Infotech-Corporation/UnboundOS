namespace UnboundOS.Core.Navigation;

/// <summary>
/// Home is an edge-on galaxy. Bright clusters along the disk are menu
/// nodes. C# owns which node is focused and how a category list opens.
/// </summary>
public enum HomeIdleAction
{
    PanLeft = 0,
    PanRight = 1,
    OpenListFromBottom = 2,
    OpenListFromTop = 3
}

public static class HomeGalaxy
{
    /// <summary>Catalog order. Visual X puts Games on the creamy core.</summary>
    public static IReadOnlyList<CubeDestination> Nodes { get; } =
    [
        CubeDestination.Session,
        CubeDestination.Tools,
        CubeDestination.Mods,
        CubeDestination.Network,
        CubeDestination.Files,
        CubeDestination.Hardware
    ];

    /// <summary>Left-to-right along the disk (blue arm → core → blue arm).</summary>
    public static IReadOnlyList<CubeDestination> VisualOrder { get; } =
    [
        CubeDestination.Hardware,
        CubeDestination.Files,
        CubeDestination.Network,
        CubeDestination.Session,
        CubeDestination.Tools,
        CubeDestination.Mods
    ];

    /// <summary>
    /// World X for catalog index. Session sits on the bright core;
    /// other nodes lock to luminous clusters along the plane.
    /// </summary>
    private static readonly float[] VisualX = [0f, 1.14f, 2.22f, -1.14f, -2.22f, -3.30f];

    public const float NodeSpacing = 1.14f;

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

    public static int VisualIndexOf(CubeDestination destination)
    {
        for (var i = 0; i < VisualOrder.Count; i++)
        {
            if (VisualOrder[i] == destination)
            {
                return i;
            }
        }

        return IndexOf(CubeDestination.Session);
    }

    public static CubeDestination Neighbor(CubeDestination current, int delta) =>
        VisualOrder[Wrap(VisualIndexOf(current) + delta)];

    public static float NodeX(int index) => VisualX[Wrap(index)];

    public static HomeIdleAction? FromTurn(CubeTurn turn) =>
        turn switch
        {
            CubeTurn.Left => HomeIdleAction.PanLeft,
            CubeTurn.Right => HomeIdleAction.PanRight,
            CubeTurn.Up => HomeIdleAction.OpenListFromBottom,
            CubeTurn.Down => HomeIdleAction.OpenListFromTop,
            _ => null
        };

    public static int ListStartIndex(int count, bool fromBottom)
    {
        if (count <= 0)
        {
            return 0;
        }

        return fromBottom ? count - 1 : 0;
    }

    public static string Announce(CubeDestination node)
    {
        var info = CubeCatalog.Info(node);
        return $"{info.Title} node. Up opens this list from the bottom over the galaxy. Down opens this list from the top. Left and right shift nodes. Enter opens this group. Settings and Profiles are the corner glyphs.";
    }

    public static string AnnounceOverlay(CubeBrowseItem item, int index, int total) =>
        $"{item.Title}, {item.Meta}, {index + 1} of {total}. Enter opens. Up and down move the list. Escape returns to the galaxy.";
}
