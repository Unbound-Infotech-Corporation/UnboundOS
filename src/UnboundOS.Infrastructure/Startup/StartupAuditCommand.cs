namespace UnboundOS.Infrastructure.Startup;

/// <summary>
/// Headless daily-audit flag for the WinUnbound image scheduled task.
/// Spec: docs/os-spec.md §2 and docs/startup-audit-task.xml.
/// </summary>
public static class StartupAuditCommand
{
    public const string Flag = "--audit-startup";

    public static bool IsRequested(IEnumerable<string>? args)
    {
        if (args is null)
        {
            return false;
        }

        return args.Any(arg =>
            string.Equals(arg, Flag, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(arg, "/audit-startup", StringComparison.OrdinalIgnoreCase));
    }
}
