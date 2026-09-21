using System.Text.Json.Serialization;

namespace UnboundOS.Core.Models;

/// <summary>Persisted UnboundOS shell preferences (LocalAppData JSON).</summary>
public sealed record ShellSettings
{
    public bool InterfaceMotionEnabled { get; init; } = true;

    /// <summary>Null means default on (older settings.json omit these keys).</summary>
    public bool? HomeHudEnabled { get; init; }

    public bool? HomeClockEnabled { get; init; }

    public bool? HomeTempsEnabled { get; init; }

    [JsonIgnore]
    public bool ShowHomeHud => HomeHudEnabled ?? true;

    [JsonIgnore]
    public bool ShowHomeClock => HomeClockEnabled ?? true;

    [JsonIgnore]
    public bool ShowHomeTemps => HomeTempsEnabled ?? true;

    public static ShellSettings CreateDefault() => new();
}

public enum MotionSuppression
{
    None = 0,
    UserDisabled = 1,
    SystemDisabled = 2,
    SessionLive = 3
}
