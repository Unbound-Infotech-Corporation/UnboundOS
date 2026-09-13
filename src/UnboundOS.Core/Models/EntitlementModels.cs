namespace UnboundOS.Core.Models;

public sealed record EntitlementSnapshot(
    bool IsPro,
    DateTimeOffset? LastValidatedUtc,
    DateTimeOffset? GraceExpiresUtc,
    int ActivationsUsed,
    int MaxActivations,
    string? KeyPrefix)
{
    public static EntitlementSnapshot Free { get; } = new(false, null, null, 0, 0, null);

    public bool IsProEffective(DateTimeOffset utcNow) =>
        IsPro && (GraceExpiresUtc is null || utcNow <= GraceExpiresUtc);
}

public sealed record ActivationOutcome(
    bool Succeeded,
    bool UsedOfflineGrace,
    string Message,
    EntitlementSnapshot Snapshot);

public static class FeatureAccess
{
    public const string FreeProfileId = "living-room";
    public static readonly TimeSpan OfflineGrace = TimeSpan.FromDays(14);

    public static bool IsPro(EntitlementSnapshot snapshot, DateTimeOffset utcNow) =>
        snapshot.IsProEffective(utcNow);

    public static bool CanUseProfile(EntitlementSnapshot snapshot, SessionProfile profile, DateTimeOffset utcNow) =>
        IsPro(snapshot, utcNow) || profile.Id.Equals(FreeProfileId, StringComparison.OrdinalIgnoreCase);

    public static bool CanUseDualNic(EntitlementSnapshot snapshot, DateTimeOffset utcNow) =>
        IsPro(snapshot, utcNow);

    public static bool CanUseStreamTools(EntitlementSnapshot snapshot, DateTimeOffset utcNow) =>
        IsPro(snapshot, utcNow);

    public static bool CanUseModProfiles(EntitlementSnapshot snapshot, DateTimeOffset utcNow) =>
        IsPro(snapshot, utcNow);

    public static bool CanUseCustomProfiles(EntitlementSnapshot snapshot, DateTimeOffset utcNow) =>
        IsPro(snapshot, utcNow);

    public static bool CanUseAdvancedRestore(EntitlementSnapshot snapshot, DateTimeOffset utcNow) =>
        IsPro(snapshot, utcNow);
}
