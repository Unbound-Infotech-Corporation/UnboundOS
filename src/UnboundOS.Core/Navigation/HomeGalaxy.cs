namespace UnboundOS.Core.Navigation;

/// <summary>
/// Home is a black studio field with vertical category tabs.
/// C# owns which tab is focused and how a category list opens.
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

    /// <summary>LTR studio tabs, including Options between Games and Tools.</summary>
    public static IReadOnlyList<string> TabIds { get; } =
    [
        "Hardware", "Files", "Network", "Session", "Settings", "Tools", "Mods"
    ];

    public const int OptionsTabIndex = 4;

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
            CubeDestination.Hardware => 0,
            CubeDestination.Files => 1,
            CubeDestination.Network => 2,
            CubeDestination.Session => 3,
            CubeDestination.Tools => 5,
            CubeDestination.Mods => 6,
            _ => 3
        };
    }

    public static (string Id, CubeDestination? Destination) TabAt(int index)
    {
        var i = CubeBrowse.Wrap(index, TabIds.Count);
        return i switch
        {
            0 => ("Hardware", CubeDestination.Hardware),
            1 => ("Files", CubeDestination.Files),
            2 => ("Network", CubeDestination.Network),
            3 => ("Session", CubeDestination.Session),
            4 => ("Settings", null),
            5 => ("Tools", CubeDestination.Tools),
            6 => ("Mods", CubeDestination.Mods),
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
        return $"{info.Title} tab. Up opens this list from the bottom over Home. Down opens this list from the top. Left and right move between tabs. Enter opens this group. The SET glyph opens Options. Settings and Profiles are the corner glyphs.";
    }

    public static string AnnounceOptions() =>
        "Options tab. Up opens this list from the bottom. Down opens this list from the top. Left and right move between tabs. Enter opens this group. Settings and Profiles are the corner glyphs.";

    public static string AnnounceOverlay(CubeBrowseItem item, int index, int total) =>
        $"{item.Title}, {item.Meta}, {index + 1} of {total}. Enter opens. Up and down move the list. Escape returns to Home.";
}
