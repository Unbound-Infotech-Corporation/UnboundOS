namespace UnboundOS.Core.Models;

/// <summary>Persisted UnboundOS shell preferences (LocalAppData JSON).</summary>
public sealed record ShellSettings
{
    public bool InterfaceMotionEnabled { get; init; } = true;

    public static ShellSettings CreateDefault() => new();
}

public enum MotionSuppression
{
    None = 0,
    UserDisabled = 1,
    SystemDisabled = 2,
    SessionLive = 3
}
