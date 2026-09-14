namespace UnboundOS.Core.Models;

public static class DesktopToolIds
{
    public const string Obs = "obs";
    public const string Vortex = "vortex";
    public const string Discord = "discord";
    public const string Playnite = "playnite";
    public const string Steam = "steam";
}

public sealed record ToolGetPath(string Label, string Uri);

public sealed record DesktopTool(
    string Id,
    string DisplayName,
    string Job,
    bool IsInstalled,
    string? ExecutablePath,
    IReadOnlyList<string> ProcessNames,
    ToolGetPath GetPath,
    bool HasObsRecipe = false)
{
    public string AvailabilityLabel => IsInstalled ? "Installed" : "Get";
}

public sealed record ToolLaunchResult(bool Succeeded, string Message)
{
    public static ToolLaunchResult Ok(string message) => new(true, message);

    public static ToolLaunchResult Fail(string message) => new(false, message);
}
