using UnboundOS.Core.Models;

namespace UnboundOS.Core.Shell;

/// <summary>One-game-at-a-time focus list. Close is per process, not a taskbar.</summary>
public static class OneGameSwitcher
{
    public static IReadOnlyList<RunningApp> FocusList(IEnumerable<RunningApp> apps) =>
        apps
            .Where(app => app.IsFullscreenLikely)
            .GroupBy(app => app.ProcessName, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .ToArray();
}
