using System.Numerics;

namespace UnboundOS.Core.Navigation;

/// <summary>
/// Six Home destinations. On Home they sit as galaxy nodes. Settings and
/// Profiles remain the discreet corner glyphs; Overlay stays inner-page chrome.
/// </summary>
public enum CubeDestination
{
    Session = 0,
    Tools = 1,
    Hardware = 2,
    Network = 3,
    Mods = 4,
    Files = 5
}

public enum CubeTurn
{
    Left = 0,
    Right = 1,
    Up = 2,
    Down = 3
}

/// <summary>
/// How a destination opens. Games stay as a translucent list over the galaxy.
/// Tools/Mods stay as a mosaic when items exist. Config nodes land on a list page.
/// </summary>
public enum CubeOpenKind
{
    List = 0,
    Carousel = 1,
    Mosaic = 2
}

public readonly record struct CubeHit(bool Activates, CubeTurn? Turn)
{
    public static CubeHit Activate { get; } = new(true, null);

    public static CubeHit Rotate(CubeTurn turn) => new(false, turn);
}

public sealed record CubeFaceInfo(
    CubeDestination Destination,
    string NavTag,
    string Title,
    string Kicker,
    string Monogram,
    string Meta,
    string Hint,
    string Glyph,
    CubeOpenKind OpenKind);

/// <summary>
/// Discrete cube pose: four equatorial yaw steps and a ±90° pitch for Mods/Files.
/// </summary>
public readonly record struct CubePose(int YawSteps, int PitchSteps)
{
    public static CubePose Home { get; } = new(0, 0);

    public int YawSteps { get; } = ((YawSteps % 4) + 4) % 4;

    public int PitchSteps { get; } = Math.Clamp(PitchSteps, -1, 1);

    public float YawDegrees => YawSteps * 90f;

    public float PitchDegrees => PitchSteps * 90f;

    public CubeDestination Front
    {
        get
        {
            if (PitchSteps < 0)
            {
                return CubeDestination.Mods;
            }

            if (PitchSteps > 0)
            {
                return CubeDestination.Files;
            }

            return YawSteps switch
            {
                1 => CubeDestination.Tools,
                2 => CubeDestination.Hardware,
                3 => CubeDestination.Network,
                _ => CubeDestination.Session
            };
        }
    }

    public CubePose Turn(CubeTurn turn) =>
        turn switch
        {
            CubeTurn.Left => new(YawSteps - 1, PitchSteps),
            CubeTurn.Right => new(YawSteps + 1, PitchSteps),
            CubeTurn.Up => new(YawSteps, PitchSteps - 1),
            CubeTurn.Down => new(YawSteps, PitchSteps + 1),
            _ => this
        };
}

public static class CubeCatalog
{
    public static IReadOnlyList<CubeDestination> Faces { get; } =
    [
        CubeDestination.Session,
        CubeDestination.Tools,
        CubeDestination.Network,
        CubeDestination.Mods,
        CubeDestination.Files,
        CubeDestination.Hardware
    ];

    public static CubeFaceInfo Info(CubeDestination destination) =>
        destination switch
        {
            CubeDestination.Session => new(
                destination, "Session", "Games", "PLAY", "G", "PLAY",
                "Installed library. Up opens the list over the galaxy. Enter launches.",
                "games", CubeOpenKind.Carousel),
            CubeDestination.Tools => new(
                destination, "Tools", "Tools", "KIT", "T", "OPEN",
                "OBS, Vortex, Discord, Playnite, utilities.",
                "tools", CubeOpenKind.Mosaic),
            CubeDestination.Network => new(
                destination, "Network", "Network", "LINK", "N", "SPLIT",
                "Prefer a game NIC. Park bulk traffic.",
                "network", CubeOpenKind.List),
            CubeDestination.Mods => new(
                destination, "Mods", "Mods", "MOD", "M", "OPEN",
                "Workshop and Vortex discovery. Vortex stays in charge.",
                "mods", CubeOpenKind.Mosaic),
            CubeDestination.Files => new(
                destination, "Files", "Files", "FS", "F", "BROWSE",
                "Daily folders. Explorer stays for anticheat.",
                "files", CubeOpenKind.List),
            CubeDestination.Hardware => new(
                destination, "Hardware", "Hardware", "HW", "H", "READ",
                "Inventory from this PC. Optional official HWiNFO.",
                "hardware", CubeOpenKind.List),
            _ => throw new ArgumentOutOfRangeException(nameof(destination))
        };

    public static string NavTag(CubeDestination destination) => Info(destination).NavTag;

    public static CubeOpenKind OpenKind(CubeDestination destination) => Info(destination).OpenKind;

    public static bool StaysInCube(CubeDestination destination) =>
        OpenKind(destination) is CubeOpenKind.Carousel or CubeOpenKind.Mosaic;

    public static string Announce(CubeDestination destination) =>
        HomeGalaxy.Announce(destination);

    public static string AnnounceItem(CubeBrowseItem item, int index, int total) =>
        HomeGalaxy.AnnounceOverlay(item, index, total);
}

/// <summary>Pointer, keyboard, and gamepad mapping for Home (galaxy nodes).</summary>
public static class CubeInput
{
    public const float DefaultPixelsPerQuarterTurn = 96f;
    public const float DefaultEdge = 0.32f;
    public const float DefaultFlickPixelsPerSecond = 640f;

    public static CubeTurn? FromKey(string? key) =>
        key switch
        {
            "Left" or "GamepadDPadLeft" or "GamepadLeftThumbstickLeft" => CubeTurn.Left,
            "Right" or "GamepadDPadRight" or "GamepadLeftThumbstickRight" => CubeTurn.Right,
            "Up" or "GamepadDPadUp" or "GamepadLeftThumbstickUp" => CubeTurn.Up,
            "Down" or "GamepadDPadDown" or "GamepadLeftThumbstickDown" => CubeTurn.Down,
            _ => null
        };

    public static bool IsActivateKey(string? key) =>
        key is "Enter" or "Space" or "GamepadA";

    public static bool IsBackKey(string? key) =>
        key is "Escape" or "Back" or "GamepadB";

    public static CubeHit HitFromNormalizedPoint(float nx, float ny, float edge = DefaultEdge)
    {
        if (float.IsNaN(nx) || float.IsNaN(ny))
        {
            return CubeHit.Activate;
        }

        nx = Math.Clamp(nx, -1f, 1f);
        ny = Math.Clamp(ny, -1f, 1f);
        if (Math.Abs(nx) < edge && Math.Abs(ny) < edge)
        {
            return CubeHit.Activate;
        }

        if (Math.Abs(nx) >= Math.Abs(ny))
        {
            return CubeHit.Rotate(nx < 0 ? CubeTurn.Left : CubeTurn.Right);
        }

        return CubeHit.Rotate(ny < 0 ? CubeTurn.Up : CubeTurn.Down);
    }

    public static (float Yaw, float Pitch) PreviewDrag(
        CubePose start,
        float deltaX,
        float deltaY,
        float pixelsPerQuarterTurn = DefaultPixelsPerQuarterTurn)
    {
        var scale = pixelsPerQuarterTurn <= 0 ? DefaultPixelsPerQuarterTurn : pixelsPerQuarterTurn;
        var yaw = start.YawDegrees - (deltaX / scale) * 90f;
        var pitch = Math.Clamp(start.PitchDegrees - (deltaY / scale) * 90f, -100f, 100f);
        return (yaw, pitch);
    }

    public static CubePose SnapFromDegrees(float yawDegrees, float pitchDegrees)
    {
        var yawSteps = (int)Math.Round(yawDegrees / 90f, MidpointRounding.AwayFromZero);
        var pitchSteps = (int)Math.Round(Math.Clamp(pitchDegrees, -90f, 90f) / 90f, MidpointRounding.AwayFromZero);
        return new CubePose(yawSteps, pitchSteps);
    }

    public static CubeTurn? FlickTurn(float velocityX, float velocityY, float threshold = DefaultFlickPixelsPerSecond)
    {
        if (Math.Max(Math.Abs(velocityX), Math.Abs(velocityY)) < threshold)
        {
            return null;
        }

        if (Math.Abs(velocityX) >= Math.Abs(velocityY))
        {
            return velocityX < 0 ? CubeTurn.Right : CubeTurn.Left;
        }

        return velocityY > 0 ? CubeTurn.Up : CubeTurn.Down;
    }

    public static float IdleYawDegrees(double seconds, float amplitude = 2.2f) =>
        amplitude * MathF.Sin((float)(seconds * 0.55));

    public static float NearestEquivalentDegrees(float current, float target)
    {
        var nearest = target;
        while (nearest - current > 180f)
        {
            nearest -= 360f;
        }

        while (current - nearest > 180f)
        {
            nearest += 360f;
        }

        return nearest;
    }

    public static float SpringStep(
        float current,
        float target,
        ref float velocity,
        float deltaSeconds,
        float stiffness = 86f,
        float damping = 15f)
    {
        if (deltaSeconds <= 0)
        {
            return current;
        }

        var accel = (target - current) * stiffness - velocity * damping;
        velocity += accel * deltaSeconds;
        return current + velocity * deltaSeconds;
    }
}

/// <summary>WinUI Composition / XAML face placement. Y-down, +Z toward the viewer.</summary>
public static class CubeLayout
{
    public static Vector3 FaceOutward(CubeDestination face) =>
        face switch
        {
            CubeDestination.Session => new Vector3(0, 0, 1),
            CubeDestination.Tools => new Vector3(1, 0, 0),
            CubeDestination.Hardware => new Vector3(0, 0, -1),
            CubeDestination.Network => new Vector3(-1, 0, 0),
            CubeDestination.Mods => new Vector3(0, -1, 0),
            CubeDestination.Files => new Vector3(0, 1, 0),
            _ => Vector3.UnitZ
        };

    public static Vector3 FaceCenter(CubeDestination face, float half) =>
        FaceOutward(face) * half;

    public static Matrix4x4 FaceLocal(CubeDestination face, float half)
    {
        var translate = Matrix4x4.CreateTranslation(0, 0, half);
        return face switch
        {
            CubeDestination.Session => translate,
            CubeDestination.Tools => translate * Matrix4x4.CreateRotationY(MathF.PI / 2f),
            CubeDestination.Hardware => translate * Matrix4x4.CreateRotationY(MathF.PI),
            CubeDestination.Network => translate * Matrix4x4.CreateRotationY(-MathF.PI / 2f),
            CubeDestination.Mods => translate * Matrix4x4.CreateRotationX(MathF.PI / 2f),
            CubeDestination.Files => translate * Matrix4x4.CreateRotationX(-MathF.PI / 2f),
            _ => translate
        };
    }

    public static Matrix4x4 CubeRotation(float yawDegrees, float pitchDegrees, float idleYawDegrees = 0)
    {
        var yaw = -(yawDegrees + idleYawDegrees) * (MathF.PI / 180f);
        var pitch = pitchDegrees * (MathF.PI / 180f);
        return Matrix4x4.CreateRotationY(yaw) * Matrix4x4.CreateRotationX(pitch);
    }

    public static Matrix4x4 Perspective(float depth = 860f)
    {
        var matrix = Matrix4x4.Identity;
        matrix.M34 = -1f / Math.Max(depth, 200f);
        return matrix;
    }

    public static Matrix4x4 Centered(Matrix4x4 rotation, float width, float height)
    {
        var cx = width * 0.5f;
        var cy = height * 0.5f;
        return Matrix4x4.CreateTranslation(-cx, -cy, 0) *
               rotation *
               Matrix4x4.CreateTranslation(cx, cy, 0);
    }

    public static float FacingCamera(CubeDestination face, Matrix4x4 cubeRotation)
    {
        var normal = Vector3.TransformNormal(FaceOutward(face), cubeRotation);
        if (normal.LengthSquared() < 0.0001f)
        {
            return 0;
        }

        return Vector3.Dot(Vector3.Normalize(normal), Vector3.UnitZ);
    }

    public static int DepthIndex(CubeDestination face, Matrix4x4 cubeRotation, float half) =>
        (int)MathF.Round(Vector3.Transform(FaceCenter(face, half), cubeRotation).Z * 100f);

    public static float FaceOpacity(float facing) =>
        0.28f + 0.72f * Math.Clamp((facing + 0.15f) / 1.15f, 0f, 1f);

    public static float SpecularOpacity(float facing, float idleYawDegrees) =>
        Math.Clamp(facing, 0f, 1f) * (0.07f + 0.03f * MathF.Abs(idleYawDegrees) / 2.2f);
}
