namespace UnboundOS.Core.Home;

/// <summary>
/// First-party Home utility widgets. Quiet chrome over the galaxy —
/// not a Rainmeter clone. Addons append extras via
/// <see cref="UnboundOS.Core.Abstractions.IHomeWidgetSource"/>.
/// </summary>
public enum HomeWidgetAppearance
{
    Glass = 0,
    Dim = 1,
    Compact = 2
}

public sealed record HomeWidgetDefinition(
    string Id,
    string Title,
    string Kicker,
    string Kind);

public sealed record HomeWidgetPlacement
{
    public string Id { get; init; } = "";

    /// <summary>Normalized left (0–1).</summary>
    public double X { get; init; }

    /// <summary>Normalized top (0–1).</summary>
    public double Y { get; init; }

    public bool? Visible { get; init; }

    public bool IsVisible => Visible ?? true;
}

public static class HomeWidgets
{
    public const string Clock = "clock";
    public const string Cpu = "cpu";
    public const string Gpu = "gpu";
    public const string Package = "package";
    public const string Load = "load";

    /// <summary>Left-half category list / scrim. Defaults stay to the right of this.</summary>
    public const double ListKeepoutX = 0.48;

    public static IReadOnlyList<HomeWidgetDefinition> BuiltIn { get; } =
    [
        new(Clock, "Clock", "TIME", "clock"),
        new(Cpu, "CPU temperature", "CPU", "temp"),
        new(Gpu, "GPU temperature", "GPU", "temp"),
        new(Package, "Package temperature", "PKG", "temp"),
        new(Load, "CPU load", "LOAD", "load")
    ];

    public static IReadOnlyList<HomeWidgetPlacement> Defaults { get; } =
    [
        new() { Id = Clock, X = 0.84, Y = 0.04 },
        new() { Id = Cpu, X = 0.84, Y = 0.16 },
        new() { Id = Gpu, X = 0.84, Y = 0.28 },
        new() { Id = Package, X = 0.84, Y = 0.40 },
        new() { Id = Load, X = 0.84, Y = 0.52 }
    ];

    public static bool ClearsList(HomeWidgetPlacement placement) =>
        Clamp(placement).X >= ListKeepoutX;

    public static HomeWidgetPlacement DefaultOf(string id) =>
        Defaults.FirstOrDefault(item => item.Id == id) ?? new() { Id = id, X = 0.4, Y = 0.4 };

    public static HomeWidgetAppearance ParseAppearance(string? value) =>
        value?.Trim().ToLowerInvariant() switch
        {
            "dim" => HomeWidgetAppearance.Dim,
            "compact" => HomeWidgetAppearance.Compact,
            _ => HomeWidgetAppearance.Glass
        };

    public static string AppearanceToken(HomeWidgetAppearance appearance) =>
        appearance switch
        {
            HomeWidgetAppearance.Dim => "dim",
            HomeWidgetAppearance.Compact => "compact",
            _ => "glass"
        };

    public static string FillHex(HomeWidgetAppearance appearance) =>
        appearance switch
        {
            HomeWidgetAppearance.Dim => "#559098A0",
            HomeWidgetAppearance.Compact => "#73C4CCD4",
            _ => "#8CC8D0D8"
        };

    public static string StrokeHex(HomeWidgetAppearance appearance) =>
        appearance switch
        {
            HomeWidgetAppearance.Dim => "#4490A0B0",
            _ => "#66E4E8EC"
        };

    public static string InkHex => "#1C2228";

    public static HomeWidgetPlacement Clamp(HomeWidgetPlacement placement)
    {
        ArgumentNullException.ThrowIfNull(placement);
        return placement with
        {
            Id = string.IsNullOrWhiteSpace(placement.Id) ? Clock : placement.Id.Trim(),
            X = Math.Clamp(placement.X, 0, 1),
            Y = Math.Clamp(placement.Y, 0, 1)
        };
    }

    public static IReadOnlyList<HomeWidgetPlacement> Merge(IReadOnlyList<HomeWidgetPlacement>? stored)
    {
        var byId = new Dictionary<string, HomeWidgetPlacement>(StringComparer.OrdinalIgnoreCase);
        foreach (var item in Defaults)
        {
            byId[item.Id] = item;
        }

        if (stored is not null)
        {
            foreach (var item in stored)
            {
                if (string.IsNullOrWhiteSpace(item.Id))
                {
                    continue;
                }

                byId[item.Id] = Clamp(item);
            }
        }

        return [.. byId.Values];
    }

    public static HomeWidgetPlacement Place(IReadOnlyList<HomeWidgetPlacement> placements, string id)
    {
        foreach (var item in placements)
        {
            if (string.Equals(item.Id, id, StringComparison.OrdinalIgnoreCase))
            {
                return Clamp(item);
            }
        }

        return Clamp(DefaultOf(id));
    }

    public static IReadOnlyList<HomeWidgetPlacement> WithPosition(
        IReadOnlyList<HomeWidgetPlacement> current,
        string id,
        double x,
        double y)
    {
        var next = Merge(current).ToList();
        var clamped = Clamp(new HomeWidgetPlacement { Id = id, X = x, Y = y, Visible = Place(next, id).Visible });
        var at = next.FindIndex(item => string.Equals(item.Id, id, StringComparison.OrdinalIgnoreCase));
        if (at >= 0)
        {
            next[at] = clamped;
        }
        else
        {
            next.Add(clamped);
        }

        return next;
    }
}
