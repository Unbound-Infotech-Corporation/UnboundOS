using System.Globalization;
using System.Numerics;

namespace UnboundOS.Core.Navigation;

/// <summary>
/// Rest pose, destination palettes, and visual helpers (legacy cube math
/// still used to aim HomeGalaxy nodes).
/// Logical pose stays 90° snaps; the rest yaw/pitch is a product-shot 3/4 so
/// three faces read at once. Accents stay inside the organic Home family
/// (bone, bruise, bile, blood, ash, sparse vein light) — not electric cyan.
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

    /// <summary>WebGL open/disassemble duration. Motion-off skips this.</summary>
    public const int OpenDurationMs = 1080;

    public static CubeAccent Palette(CubeDestination destination) =>
        destination switch
        {
            CubeDestination.Session => new("#6FA896", "#8A6B4A", "#5C2A2E", "#2A221C", "#0A0808"),
            CubeDestination.Tools => new("#7A8B6A", "#6B4A38", "#4A3428", "#1C1814", "#0A0808"),
            CubeDestination.Network => new("#6A5A78", "#8A6B4A", "#3A2A38", "#1A1418", "#0C080A"),
            CubeDestination.Mods => new("#8A7A58", "#5C3A32", "#4A2E28", "#181410", "#0A0808"),
            CubeDestination.Files => new("#9A8A72", "#6A5848", "#3A3028", "#1A1612", "#0A0808"),
            CubeDestination.Hardware => new("#7A4A42", "#8A7060", "#2A1C1A", "#141010", "#0A0808"),
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
        var min = Math.Min(r, Math.Min(g, b));
        var chroma = max - min;

        // Hot pink, carnival orange, acid lime, electric neon cyan, neon magenta.
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

        if (b > 200 && g > 180 && r < 90 && chroma > 160)
        {
            return false;
        }

        if (r < 40 && g > 220 && b > 230)
        {
            return false;
        }

        if (r > 220 && b > 220 && g < 80)
        {
            return false;
        }

        if (max > 210 && chroma > 180)
        {
            return false;
        }

        return max >= 8;
    }
}
