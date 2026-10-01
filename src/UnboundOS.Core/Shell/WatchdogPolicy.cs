namespace UnboundOS.Core.Shell;

/// <summary>
/// Crash window for the shell watchdog. Pure clock math — no Win32.
/// After <see cref="CrashLimit"/> exits inside <see cref="Window"/>, fall back to Explorer.
/// </summary>
public static class WatchdogPolicy
{
    public const int CrashLimit = 3;

    public static readonly TimeSpan Window = TimeSpan.FromMinutes(2);

    public static readonly TimeSpan FastExit = TimeSpan.FromSeconds(15);

    public const string WatchdogFlag = "--watchdog";

    public const string RestoreFlag = "--restore-explorer";

    public const string CrashLogName = "watchdog-crashes.json";

    public const string WatchdogLogName = "watchdog.log";

    public static bool IsFastExit(TimeSpan runtime) => runtime < FastExit;

    public static bool IsCrashExit(int exitCode, TimeSpan runtime) =>
        exitCode != 0 || IsFastExit(runtime);

    public static IReadOnlyList<DateTimeOffset> Record(
        IReadOnlyList<DateTimeOffset> crashes,
        DateTimeOffset now)
    {
        var kept = crashes.Where(stamp => now - stamp <= Window).ToList();
        kept.Add(now);
        return kept;
    }

    public static bool ShouldFallback(IReadOnlyList<DateTimeOffset> crashes, DateTimeOffset now) =>
        crashes.Count(stamp => now - stamp <= Window) >= CrashLimit;

    public static string FallbackReason(IReadOnlyList<DateTimeOffset> crashes, DateTimeOffset now) =>
        ShouldFallback(crashes, now)
            ? $"{crashes.Count(stamp => now - stamp <= Window)} exits in {Window.TotalMinutes:0} minutes. Started explorer.exe."
            : "Under the crash limit.";
}
