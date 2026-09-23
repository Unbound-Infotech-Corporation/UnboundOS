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

    /// <summary>Turn Windows Game Mode on for the session. Default on.</summary>
    public bool ApplyGameMode { get; init; } = true;

    /// <summary>Competitive: Game DVR / background capture off. Streamer keeps capture.</summary>
    public bool DisableGameDvr { get; init; }

    /// <summary>Visual effects → Performance while the session is live.</summary>
    public bool VisualEffectsPerformance { get; init; }

    public string Monogram =>
        string.IsNullOrWhiteSpace(Name)
            ? "P"
            : char.ToUpperInvariant(Name.Trim()[0]).ToString();

    public string KindLabel => Kind.ToString().ToUpperInvariant();

    public string SkinnySummary
    {
        get
        {
            var bits = new List<string>
            {
                ApplyGameMode ? "Game Mode on" : "Game Mode left alone",
                DisableGameDvr ? "Game DVR off" : "Game DVR left on",
                VisualEffectsPerformance ? "visual effects Performance" : "visual effects left pretty",
                EnableHighPerformancePowerHint ? "Ultimate Performance while live" : "power plan left alone"
            };
            return string.Join(" · ", bits) + ". HAGS is an Options toggle, not forced. Defender / Update / VBS stay on.";
        }
    }

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
            PauseWindowsUpdateOrchestrator = PauseWindowsUpdateOrchestrator,
            ApplyGameMode = ApplyGameMode,
            DisableGameDvr = DisableGameDvr,
            VisualEffectsPerformance = VisualEffectsPerformance
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
