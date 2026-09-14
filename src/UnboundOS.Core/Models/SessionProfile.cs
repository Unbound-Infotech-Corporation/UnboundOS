namespace UnboundOS.Core.Models;

public sealed class SessionProfile
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public string Description { get; init; } = string.Empty;
    public ProfileKind Kind { get; init; } = ProfileKind.Custom;
    public IReadOnlyList<string> TerminateProcessNames { get; init; } = [];
    public IReadOnlyList<string> ProtectProcessNames { get; init; } = [];
    public string? PreferredGameAdapterId { get; init; }
    public string? PreferredStreamAdapterId { get; init; }
    public IReadOnlyList<string> GameProcessHints { get; init; } = [];
    public IReadOnlyList<string> StreamProcessHints { get; init; } = ["obs64", "obs32", "Streamlabs OBS"];
    public StreamPreferences Stream { get; init; } = new();
    public bool EnableHighPerformancePowerHint { get; init; } = true;
    public bool PauseWindowsUpdateOrchestrator { get; init; }

    public string Monogram =>
        string.IsNullOrWhiteSpace(Name)
            ? "P"
            : char.ToUpperInvariant(Name.Trim()[0]).ToString();

    public string KindLabel => Kind.ToString().ToUpperInvariant();

    public SessionProfile WithProcessLists(
        IReadOnlyList<string> terminateProcessNames,
        IReadOnlyList<string> protectProcessNames) =>
        new()
        {
            Id = Id,
            Name = Name,
            Description = Description,
            Kind = Kind,
            TerminateProcessNames = terminateProcessNames,
            ProtectProcessNames = protectProcessNames,
            PreferredGameAdapterId = PreferredGameAdapterId,
            PreferredStreamAdapterId = PreferredStreamAdapterId,
            GameProcessHints = GameProcessHints,
            StreamProcessHints = StreamProcessHints,
            Stream = Stream,
            EnableHighPerformancePowerHint = EnableHighPerformancePowerHint,
            PauseWindowsUpdateOrchestrator = PauseWindowsUpdateOrchestrator
        };
}

public sealed class StreamPreferences
{
    public int SourceWidth { get; init; } = 5110;
    public int SourceHeight { get; init; } = 1400;
    public int OutputWidth { get; init; } = 1920;
    public int OutputHeight { get; init; } = 1080;
    public double OutputFps { get; init; } = 60;
    public int TargetBitrateKbps { get; init; } = 6000;
    public string CropMode { get; init; } = "Center16x9";
    public double SharpenAmount { get; init; } = 0.35;
}
