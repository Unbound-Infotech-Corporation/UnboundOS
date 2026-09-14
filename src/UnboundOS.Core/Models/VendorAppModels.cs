namespace UnboundOS.Core.Models;

public enum VendorAppRole
{
    Display = 0,
    Overclock = 1
}

public static class VendorAppIds
{
    public const string NvidiaApp = "nvidia-app";
    public const string NvidiaControlPanel = "nvidia-control-panel";
    public const string AmdAdrenalin = "amd-adrenalin";
    public const string IntelArc = "intel-arc";
    public const string AmdRyzenMaster = "amd-ryzen-master";
    public const string IntelXtu = "intel-xtu";
}

public sealed record VendorApp(
    string Id,
    string DisplayName,
    string Vendor,
    VendorAppRole Role,
    bool IsInstalled,
    string? ExecutablePath,
    ToolGetPath GetPath)
{
    public string StatusLabel => IsInstalled ? "OPEN" : "GET";

    public string AvailabilityLabel => IsInstalled ? "Installed" : "Get";

    public string Monogram =>
        string.IsNullOrWhiteSpace(DisplayName)
            ? "+"
            : char.ToUpperInvariant(DisplayName.Trim()[0]).ToString();
}
