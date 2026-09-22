using System.Text.Json;
using System.Text.Json.Serialization;

namespace UnboundOS.Core.Navigation;

/// <summary>
/// JSON contract between the WinUI shell and the packaged WebGL Home host.
/// C# owns node focus, overlay, and page navigation. The scene owns rendering.
/// </summary>
public sealed record CubeFacePayload(
    string Id,
    string Title,
    string Kicker,
    string Monogram,
    string Meta,
    string Hint,
    string Accent,
    string Seam,
    string Core,
    string Plate,
    string Glyph,
    string OpenKind);

public sealed record CubeSceneState
{
    public int V { get; init; } = 1;

    public string Type { get; init; } = "state";

    public float Yaw { get; init; }

    public float Pitch { get; init; }

    public float RestYaw { get; init; } = CubeAtmosphere.RestYawDegrees;

    public float RestPitch { get; init; } = CubeAtmosphere.RestPitchDegrees;

    public string Front { get; init; } = nameof(CubeDestination.Session);

    public bool Motion { get; init; } = true;

    public bool Burst { get; init; }

    public string Accent { get; init; } = CubeAtmosphere.Palette(CubeDestination.Session).AccentHex;

    public string Seam { get; init; } = CubeAtmosphere.Palette(CubeDestination.Session).SeamHex;

    public string Core { get; init; } = CubeAtmosphere.Palette(CubeDestination.Session).CoreHex;

    public string Plate { get; init; } = CubeAtmosphere.Palette(CubeDestination.Session).PlateHex;

    public string Title { get; init; } = "Session";

    public string Kicker { get; init; } = "SYS";

    public string Monogram { get; init; } = "S";

    public string Hint { get; init; } = "";

    public string Phase { get; init; } = "idle";

    public string Mode { get; init; } = "list";

    public int Focus { get; init; }

    public int Node { get; init; }

    public string Overlay { get; init; } = "none";

    public IReadOnlyList<CubeFacePayload> Faces { get; init; } = [];

    public IReadOnlyList<CubeBrowseItem> Items { get; init; } = [];
}

public sealed record CubeHostMessage
{
    public int V { get; init; } = 1;

    public string Type { get; init; } = "";

    public string? Turn { get; init; }

    public string? Face { get; init; }

    public float Dx { get; init; }

    public float Dy { get; init; }

    public float Vx { get; init; }

    public float Vy { get; init; }

    public int Index { get; init; }

    public string? Item { get; init; }
}

public sealed record CubeBrowseItem(
    string Id,
    string Title,
    string Meta,
    string Kind,
    string Glyph);

public sealed record CubeHostCommand
{
    public int V { get; init; } = 1;

    public string Type { get; init; } = "open";

    public string Front { get; init; } = nameof(CubeDestination.Session);

    public bool Motion { get; init; } = true;

    public string Mode { get; init; } = "list";

    public bool Stay { get; init; }

    public int Focus { get; init; }

    public int Node { get; init; }

    public string Origin { get; init; } = "top";

    public IReadOnlyList<CubeBrowseItem> Items { get; init; } = [];
}

public static class CubeBridge
{
    public const string VirtualHost = "unboundos.cube";

    public const string IndexUrl = "https://unboundos.cube/Cube/index.html";

    public const string AssetFolder = "Assets";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public static IReadOnlyList<CubeFacePayload> CatalogPayload() =>
        CubeCatalog.Faces.Select(Payload).ToArray();

    public static CubeFacePayload Payload(CubeDestination destination)
    {
        var info = CubeCatalog.Info(destination);
        var accent = CubeAtmosphere.Palette(destination);
        return new(
            destination.ToString(),
            info.Title,
            info.Kicker,
            info.Monogram,
            info.Meta,
            info.Hint,
            accent.AccentHex,
            accent.SeamHex,
            accent.CoreHex,
            accent.PlateHex,
            info.Glyph,
            info.OpenKind.ToString().ToLowerInvariant());
    }

    public static CubeSceneState State(
        CubePose pose,
        float visualYaw,
        float visualPitch,
        bool motion,
        bool burst)
    {
        var info = CubeCatalog.Info(pose.Front);
        var accent = CubeAtmosphere.Palette(pose.Front);
        return new()
        {
            Yaw = visualYaw,
            Pitch = visualPitch,
            RestYaw = CubeAtmosphere.RestYawDegrees,
            RestPitch = CubeAtmosphere.RestPitchDegrees,
            Front = pose.Front.ToString(),
            Motion = motion,
            Burst = burst,
            Accent = accent.AccentHex,
            Seam = accent.SeamHex,
            Core = accent.CoreHex,
            Plate = accent.PlateHex,
            Title = info.Title,
            Kicker = info.Kicker,
            Monogram = info.Monogram,
            Hint = info.Hint,
            Mode = info.OpenKind.ToString().ToLowerInvariant(),
            Node = HomeGalaxy.IndexOf(pose.Front),
            Overlay = "none",
            Faces = CatalogPayload()
        };
    }

    public static string ToJson(CubeSceneState state) =>
        JsonSerializer.Serialize(state, JsonOptions);

    public static string ToJson(CubeHostCommand command) =>
        JsonSerializer.Serialize(command, JsonOptions);

    public static CubeHostCommand Open(
        CubeDestination front,
        bool motion,
        IReadOnlyList<CubeBrowseItem>? items = null,
        int focus = 0,
        string origin = "top")
    {
        var list = items ?? [];
        var kind = CubeCatalog.OpenKind(front);
        return new()
        {
            Type = "open",
            Front = front.ToString(),
            Motion = motion,
            Mode = kind.ToString().ToLowerInvariant(),
            Stay = list.Count > 0,
            Focus = focus,
            Node = HomeGalaxy.IndexOf(front),
            Origin = origin is "bottom" ? "bottom" : "top",
            Items = list
        };
    }

    public static CubeHostCommand Focus(int index) =>
        new() { Type = "focus", Focus = index };

    public static CubeHostCommand Reset(CubeDestination front, bool motion) =>
        new()
        {
            Type = "reset",
            Front = front.ToString(),
            Motion = motion,
            Node = HomeGalaxy.IndexOf(front)
        };

    public static bool TryRead(string? json, out CubeHostMessage message)
    {
        message = new CubeHostMessage();
        if (string.IsNullOrWhiteSpace(json))
        {
            return false;
        }

        try
        {
            var parsed = JsonSerializer.Deserialize<CubeHostMessage>(json, JsonOptions);
            if (parsed is null || string.IsNullOrWhiteSpace(parsed.Type))
            {
                return false;
            }

            message = parsed;
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    public static CubeTurn? ParseTurn(string? turn) =>
        Enum.TryParse<CubeTurn>(turn, ignoreCase: true, out var value) ? value : null;

    public static CubeDestination? ParseFace(string? face) =>
        Enum.TryParse<CubeDestination>(face, ignoreCase: true, out var value) ? value : null;
}
