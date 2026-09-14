namespace UnboundOS.Core.Models;

public static class DesktopToolIds
{
    public const string Obs = "obs";
    public const string Vortex = "vortex";
    public const string Discord = "discord";
    public const string Playnite = "playnite";
    public const string Steam = "steam";
    public const string NotepadPlusPlus = "notepadplusplus";
    public const string SevenZip = "sevenzip";
}

public enum DesktopToolGroup
{
    Kit,
    Utility
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
    DesktopToolGroup Group = DesktopToolGroup.Kit,
    bool HasObsRecipe = false)
{
    public string AvailabilityLabel => IsInstalled ? "Installed" : "Get";

    public string StatusLabel => IsInstalled ? "OPEN" : "GET";

    public string Monogram =>
        string.IsNullOrWhiteSpace(DisplayName)
            ? "+"
            : char.ToUpperInvariant(DisplayName.Trim()[0]).ToString();
}

public sealed record ToolLaunchResult(bool Succeeded, string Message)
{
    public static ToolLaunchResult Ok(string message) => new(true, message);

    public static ToolLaunchResult Fail(string message) => new(false, message);
}
