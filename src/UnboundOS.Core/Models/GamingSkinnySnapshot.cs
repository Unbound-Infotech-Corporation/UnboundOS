namespace UnboundOS.Core.Models;

/// <summary>Prior HKCU values so a session can restore desktop posture.</summary>
public sealed record GamingSkinnySnapshot
{
    public int? GameModeEnabled { get; init; }
    public int? AllowAutoGameMode { get; init; }
    public int? GameDvrEnabled { get; init; }
    public int? AppCaptureEnabled { get; init; }
    public int? VisualFxSetting { get; init; }
    public IReadOnlyList<string> Actions { get; init; } = [];
}
