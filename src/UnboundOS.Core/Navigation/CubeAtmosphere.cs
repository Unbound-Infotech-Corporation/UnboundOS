using System.Globalization;
using System.Numerics;

namespace UnboundOS.Core.Navigation;

/// <summary>
/// Rest pose, destination palettes, and visual helpers for the Home cube.
/// Logical pose stays 90° snaps; the rest yaw/pitch is a product-shot 3/4 so
/// three faces read at once. Accents stay inside the Unbound Infotech family.
/// </summary>
public readonly record struct CubeAccent(
    string AccentHex,
    string SeamHex,
    string CoreHex,
    string PlateHex,
    string FogHex);

public static class CubeAtmosphere
{
    /// <summary>Classic 3/4: Session still leads, Tools on the right, Mods as the top plate.</summary>
    public const float RestYawDegrees = 28f;

    public const float RestPitchDegrees = -17f;

    public const float CameraFovDegrees = 36f;

    public static CubeAccent Palette(CubeDestination destination) =>
        destination switch
        {
            CubeDestination.Session => new("#00F0FF", "#00D0E8", "#1E40AF", "#0B121D", "#05070A"),
            CubeDestination.Tools => new("#00D4F0", "#0098C8", "#1E3A8A", "#0A1018", "#05070A"),
            CubeDestination.Network => new("#4F7CFF", "#1E40AF", "#0EA5E9", "#0B121D", "#05080F"),
            CubeDestination.Mods => new("#2EE9D0", "#0891B2", "#155E75", "#0A1214", "#05070A"),
            CubeDestination.Files => new("#7DD3FC", "#0369A1", "#1E40AF", "#0B121D", "#05070A"),
            CubeDestination.Hardware => new("#60A5FA", "#1D4ED8", "#00B4D8", "#10151E", "#05070A"),
            _ => Palette(CubeDestination.Session)
        };

    public static CubePose AimedAt(CubeDestination destination, CubePose current) =>
        destination switch
        {
            CubeDestination.Session => new(0, 0),
            CubeDestination.Tools => new(1, 0),
            CubeDestination.Hardware => new(2, 0),
            CubeDestination.Network => new(3, 0),
            CubeDestination.Mods => new(current.YawSteps, -1),
            CubeDestination.Files => new(current.YawSteps, 1),
            _ => current
        };

    public static Matrix4x4 ViewRotation(float yawDegrees, float pitchDegrees, float idleYawDegrees = 0)
    {
        var logical = CubeLayout.CubeRotation(yawDegrees, pitchDegrees, idleYawDegrees);
        var rest = CubeLayout.CubeRotation(RestYawDegrees, RestPitchDegrees);
        return logical * rest;
    }

    public static bool IsBrandFamilyHex(string hex)
    {
        if (hex is not { Length: 7 } || hex[0] != '#')
        {
            return false;
        }

        if (!int.TryParse(hex[1..3], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var r) ||
            !int.TryParse(hex[3..5], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var g) ||
            !int.TryParse(hex[5..7], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var b))
        {
            return false;
        }

        var max = Math.Max(r, Math.Max(g, b));

        // Stay cool-steel: cyan / cobalt / teal. Reject hot pink, carnival orange, lime.
        if (r > 200 && g < 90 && b > 140)
        {
            return false;
        }

        if (r > 220 && g > 80 && g < 180 && b < 80)
        {
            return false;
        }

        if (g > 200 && r < 120 && b < 80)
        {
            return false;
        }

        return max >= 8 && b >= r && g + 48 >= r;
    }
}
