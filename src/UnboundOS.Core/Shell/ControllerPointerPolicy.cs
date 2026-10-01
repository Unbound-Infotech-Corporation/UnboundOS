using UnboundOS.Core.Models;

namespace UnboundOS.Core.Shell;

/// <summary>
/// Stick-to-pointer mapping for UnboundOS UI only. No driver and no game inject.
/// </summary>
public static class ControllerPointerPolicy
{
    public const int DefaultSpeed = 12;

    public static ControllerPointerDelta FromStick(
        double x,
        double y,
        bool primary,
        bool secondary,
        int speed = DefaultSpeed)
    {
        var dead = 0.18;
        var dx = Math.Abs(x) < dead ? 0 : (int)Math.Round(x * speed);
        var dy = Math.Abs(y) < dead ? 0 : (int)Math.Round(-y * speed);
        return new(dx, dy, primary, secondary);
    }
}
