using UnboundOS.Core.Models;

namespace UnboundOS.Core.Shell;

public static class LastSessionPolicy
{
    public static readonly TimeSpan ResumeWindow = TimeSpan.FromHours(24);

    public static bool ShouldOfferResume(LastSessionRecord? record, DateTimeOffset now)
    {
        if (record is null || !record.ResumeAfterSleep)
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(record.LaunchUri) &&
            string.IsNullOrWhiteSpace(record.ExecutablePath))
        {
            return false;
        }

        return now - record.LastPlayedUtc <= ResumeWindow;
    }
}
