using System.Text.Json.Serialization;
using UnboundOS.Core.Home;

namespace UnboundOS.Core.Models;

/// <summary>Persisted UnboundOS shell preferences (LocalAppData JSON).</summary>
public sealed record ShellSettings
{
    public bool InterfaceMotionEnabled { get; init; } = true;

    /// <summary>Null means default on (older settings.json omit these keys).</summary>
    public bool? HomeHudEnabled { get; init; }

    public bool? HomeClockEnabled { get; init; }

    public bool? HomeTempsEnabled { get; init; }

    /// <summary>glass (default), dim, or compact. Unknown values fall back to glass.</summary>
    public string? HomeWidgetAppearance { get; init; }

    public List<HomeWidgetPlacement>? HomeWidgetPlacements { get; init; }

    /// <summary>Null means leave Windows HAGS alone. True/false is an informed toggle.</summary>
    public bool? HardwareGpuScheduling { get; init; }

    [JsonIgnore]
    public bool ShowHomeHud => HomeHudEnabled ?? true;

    [JsonIgnore]
    public bool ShowHomeClock => HomeClockEnabled ?? true;

    [JsonIgnore]
    public bool ShowHomeTemps => HomeTempsEnabled ?? true;

    [JsonIgnore]
    public HomeWidgetAppearance WidgetLook => HomeWidgets.ParseAppearance(HomeWidgetAppearance);

    public static ShellSettings CreateDefault() => new();
}

public enum MotionSuppression
{
    None = 0,
    UserDisabled = 1,
    SystemDisabled = 2,
    SessionLive = 3
}
