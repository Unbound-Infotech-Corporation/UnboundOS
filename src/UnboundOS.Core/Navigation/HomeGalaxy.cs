namespace UnboundOS.Core.Navigation;

/// <summary>
/// Home is a Super Clean left-label stack on black.
/// C# owns which label is focused and how a category list opens.
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

    /// <summary>Left-to-right catalog neighbors (Options is a separate tab).</summary>
    public static IReadOnlyList<CubeDestination> VisualOrder { get; } =
    [
        CubeDestination.Hardware,
        CubeDestination.Files,
        CubeDestination.Network,
        CubeDestination.Session,
        CubeDestination.Tools,
        CubeDestination.Mods
    ];

    /// <summary>Top-to-bottom Super Clean labels, Options after Tools.</summary>
    public static IReadOnlyList<string> TabIds { get; } =
    [
        "Session", "Tools", "Settings", "Mods", "Network", "Files", "Hardware"
    ];

    public const int OptionsTabIndex = 2;

    /// <summary>Legacy spacing helper. Home tabs are laid out in the WebView.</summary>
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

    public static int TabIndexOf(CubeDestination destination, bool optionsTab = false)
    {
        if (optionsTab)
        {
            return OptionsTabIndex;
        }

        return destination switch
        {
            CubeDestination.Session => 0,
            CubeDestination.Tools => 1,
            CubeDestination.Mods => 3,
            CubeDestination.Network => 4,
            CubeDestination.Files => 5,
            CubeDestination.Hardware => 6,
            _ => 0
        };
    }

    public static (string Id, CubeDestination? Destination) TabAt(int index)
    {
        var i = CubeBrowse.Wrap(index, TabIds.Count);
        return i switch
        {
            0 => ("Session", CubeDestination.Session),
            1 => ("Tools", CubeDestination.Tools),
            2 => ("Settings", null),
            3 => ("Mods", CubeDestination.Mods),
            4 => ("Network", CubeDestination.Network),
            5 => ("Files", CubeDestination.Files),
            6 => ("Hardware", CubeDestination.Hardware),
            _ => ("Session", CubeDestination.Session)
        };
    }

    public static (string Id, CubeDestination? Destination) ShiftTab(
        CubeDestination current,
        bool optionsTab,
        int delta) =>
        TabAt(TabIndexOf(current, optionsTab) + delta);

    public static float NodeX(int index) => VisualX[Wrap(index)];

    public static HomeIdleAction? FromTurn(CubeTurn turn) =>
        turn switch
        {
            CubeTurn.Left => HomeIdleAction.PanLeft,
            CubeTurn.Right => HomeIdleAction.PanRight,
            CubeTurn.Up => HomeIdleAction.PanLeft,
            CubeTurn.Down => HomeIdleAction.PanRight,
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
        return $"{info.Title}. Up and down move the label list on Home. Enter opens this group. Escape returns to Home. The SET glyph opens Options. Settings and Profiles are the corner glyphs.";
    }

    public static string AnnounceOptions() =>
        "Options. Up and down move the label list on Home. Enter opens this group. Escape returns to Home. Settings and Profiles are the corner glyphs.";

    public static string AnnounceOverlay(CubeBrowseItem item, int index, int total) =>
        $"{item.Title}, {item.Meta}, {index + 1} of {total}. Enter opens. Up and down move the list. Escape returns to Home.";
}
