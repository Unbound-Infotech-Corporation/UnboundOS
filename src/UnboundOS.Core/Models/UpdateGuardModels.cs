namespace UnboundOS.Core.Models;

public sealed record UpdateGuardTarget
{
    public string ProductVersion { get; init; } = "Windows 11";
    public string DisplayVersion { get; init; } = "";
    public string Edition { get; init; } = "";
    public bool HomeEdition { get; init; }
}

public sealed record UpdateGuardStatus
{
    /// <summary>unknown, current, pending, or needs restart.</summary>
    public string QualityLabel { get; init; } = "unknown";

    public string Detail { get; init; } = "";
    public string ShellVersion { get; init; } = "";
    public string TargetRelease { get; init; } = "";
}
