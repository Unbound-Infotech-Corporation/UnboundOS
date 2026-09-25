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
    public const string HwInfo = "hwinfo";
    public const string Rainmeter = "rainmeter";
    public const string Phenix = "phenix";
    public const string MinimalisticClock = "minimalistic-clock";
    public const string MusicBee = "musicbee";
    public const string Visualizers = "visualizers";
    public const string Store = "store";
    public const string Xbox = "xbox";
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
    bool HasObsRecipe = false,
    bool OpensViaUri = false,
    ToolGetPath? LaunchPath = null)
{
    public bool CanOpen => IsInstalled || OpensViaUri;

    public string AvailabilityLabel => CanOpen ? "Installed" : "Get";

    public string StatusLabel => CanOpen ? "OPEN" : "GET";

    public ToolGetPath OpenPath => LaunchPath ?? GetPath;

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
