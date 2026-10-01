namespace UnboundOS.Infrastructure.Diagnostics;

/// <summary>Headless health export: UnboundOS.App.exe --health</summary>
public static class HealthCheckCommand
{
    public const string Flag = "--health";

    public static bool IsRequested(IEnumerable<string>? args)
    {
        if (args is null)
        {
            return false;
        }

        return args.Any(arg =>
            string.Equals(arg, Flag, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(arg, "/health", StringComparison.OrdinalIgnoreCase));
    }
}
