using UnboundOS.Core.Shell;

namespace UnboundOS.Infrastructure.Startup;

public static class WatchdogCommand
{
    public static bool IsWatchdog(IEnumerable<string>? args) =>
        HasFlag(args, WatchdogPolicy.WatchdogFlag, "/watchdog");

    public static bool IsRestore(IEnumerable<string>? args) =>
        HasFlag(args, WatchdogPolicy.RestoreFlag, "/restore-explorer");

    private static bool HasFlag(IEnumerable<string>? args, string flag, string slash)
    {
        if (args is null)
        {
            return false;
        }

        return args.Any(arg =>
            string.Equals(arg, flag, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(arg, slash, StringComparison.OrdinalIgnoreCase));
    }
}
