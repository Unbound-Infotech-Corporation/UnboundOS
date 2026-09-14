using UnboundOS.Core.Abstractions;
using UnboundOS.Core.Models;
using UnboundOS.Infrastructure.Tools;

namespace UnboundOS.Infrastructure.Vendor;

public sealed record VendorAppDiscoverySettings
{
    public bool UseDefaultWindowsLocations { get; init; } = true;

    public IReadOnlyDictionary<string, string?> ForcedExecutables { get; init; } =
        new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
}

/// <summary>
/// Discovers GPU vendor display apps and manufacturer OC tools. Launch only.
/// Spec: docs/os-spec.md §4–5.
/// </summary>
public sealed class VendorAppCatalog(VendorAppDiscoverySettings? settings = null) : IVendorAppCatalog
{
    public static ToolGetPath NvidiaAppGet { get; } =
        new("Official site", "https://www.nvidia.com/en-us/software/nvidia-app/");

    public static ToolGetPath NvidiaControlGet { get; } =
        new("Official site", "https://www.nvidia.com/en-us/drivers/");

    public static ToolGetPath AmdAdrenalinGet { get; } =
        new("Official site", "https://www.amd.com/en/products/software/adrenalin.html");

    public static ToolGetPath IntelArcGet { get; } =
        new("Official site", "https://www.intel.com/content/www/us/en/products/docs/discrete-gpus/arc/software/arc-control.html");

    public static ToolGetPath RyzenMasterGet { get; } =
        new("Official site", "https://www.amd.com/en/products/software/ryzen-master.html");

    public static ToolGetPath IntelXtuGet { get; } =
        new("Official site", "https://www.intel.com/content/www/us/en/download/17881/intel-extreme-tuning-utility-intel-xtu.html");

    private readonly VendorAppDiscoverySettings _settings = settings ?? new VendorAppDiscoverySettings();

    public Task<IReadOnlyList<VendorApp>> DiscoverAsync(CancellationToken cancellationToken = default) =>
        Task.Run(Discover, cancellationToken);

    internal IReadOnlyList<VendorApp> Discover() =>
    [
        Create(VendorAppIds.NvidiaApp, "NVIDIA App", "NVIDIA", VendorAppRole.Display, FindNvidiaApp(), NvidiaAppGet),
        Create(VendorAppIds.NvidiaControlPanel, "NVIDIA Control Panel", "NVIDIA", VendorAppRole.Display, FindNvidiaControlPanel(), NvidiaControlGet),
        Create(VendorAppIds.AmdAdrenalin, "AMD Adrenalin", "AMD", VendorAppRole.Display, FindAmdAdrenalin(), AmdAdrenalinGet),
        Create(VendorAppIds.IntelArc, "Intel Arc Control", "Intel", VendorAppRole.Display, FindIntelArc(), IntelArcGet),
        Create(VendorAppIds.NvidiaApp + "-oc", "NVIDIA App (tune)", "NVIDIA", VendorAppRole.Overclock, FindNvidiaApp(), NvidiaAppGet),
        Create(VendorAppIds.AmdAdrenalin + "-oc", "AMD Adrenalin / WattMan", "AMD", VendorAppRole.Overclock, FindAmdAdrenalin(), AmdAdrenalinGet),
        Create(VendorAppIds.AmdRyzenMaster, "AMD Ryzen Master", "AMD", VendorAppRole.Overclock, FindRyzenMaster(), RyzenMasterGet),
        Create(VendorAppIds.IntelXtu, "Intel Extreme Tuning (XTU)", "Intel", VendorAppRole.Overclock, FindIntelXtu(), IntelXtuGet)
    ];

    private VendorApp Create(
        string id,
        string display,
        string vendor,
        VendorAppRole role,
        string? executable,
        ToolGetPath getPath)
    {
        var installed = !string.IsNullOrWhiteSpace(executable) && File.Exists(executable);
        return new VendorApp(
            id,
            display,
            vendor,
            role,
            installed,
            installed ? executable : null,
            getPath);
    }

    private string? FindNvidiaApp() => Find(
        VendorAppIds.NvidiaApp,
        [
            Path.Combine("NVIDIA Corporation", "NVIDIA App", "CEF", "NVIDIA App.exe"),
            Path.Combine("NVIDIA Corporation", "NVIDIA Overlay", "NVIDIA App.exe"),
            "NVIDIA App.exe"
        ],
        "NVIDIA App",
        "NVIDIA App.exe");

    private string? FindNvidiaControlPanel() => Find(
        VendorAppIds.NvidiaControlPanel,
        [
            Path.Combine("Windows", "System32", "nvcplui.exe"),
            Path.Combine("NVIDIA Corporation", "Control Panel Client", "nvcplui.exe")
        ],
        "NVIDIA Control Panel",
        "nvcplui.exe");

    private string? FindAmdAdrenalin() => Find(
        VendorAppIds.AmdAdrenalin,
        [
            Path.Combine("AMD", "CNext", "CNext", "RadeonSoftware.exe"),
            Path.Combine("AMD", "CNext", "RadeonSoftware.exe"),
            "RadeonSoftware.exe"
        ],
        "AMD Software",
        "RadeonSoftware.exe");

    private string? FindIntelArc() => Find(
        VendorAppIds.IntelArc,
        [
            Path.Combine("Intel", "Intel Arc Control", "ArcControl.exe"),
            Path.Combine("Intel", "Arc Control", "ArcControl.exe"),
            "ArcControl.exe"
        ],
        "Intel Arc",
        "ArcControl.exe");

    private string? FindRyzenMaster() => Find(
        VendorAppIds.AmdRyzenMaster,
        [
            Path.Combine("AMD", "RyzenMaster", "AMD Ryzen Master.exe"),
            "AMD Ryzen Master.exe"
        ],
        "Ryzen Master",
        "AMD Ryzen Master.exe");

    private string? FindIntelXtu() => Find(
        VendorAppIds.IntelXtu,
        [
            Path.Combine("Intel", "Intel Extreme Tuning Utility", "XTU.exe"),
            Path.Combine("Intel", "Intel Extreme Tuning Utility", "XtuCli.exe"),
            "XTU.exe"
        ],
        "Extreme Tuning",
        "XTU.exe");

    private string? Find(string id, string[] relative, string uninstallToken, string exeName)
    {
        if (_settings.ForcedExecutables.TryGetValue(id, out var forced))
        {
            return string.IsNullOrWhiteSpace(forced) ? null : forced;
        }

        // NVIDIA App (tune) shares the display executable.
        if (id.EndsWith("-oc", StringComparison.Ordinal) &&
            _settings.ForcedExecutables.TryGetValue(id[..^3], out var shared) &&
            !string.IsNullOrWhiteSpace(shared))
        {
            return shared;
        }

        if (!_settings.UseDefaultWindowsLocations)
        {
            return null;
        }

        var systemRoot = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
        var extra = relative
            .Select(path => Path.IsPathRooted(path) ? path : Path.Combine(systemRoot, path))
            .Where(path => path.Contains("System32", StringComparison.OrdinalIgnoreCase));

        return DesktopAppLocator.FindFirstExisting(
            DesktopAppLocator.Combine(DesktopAppLocator.ProgramRoots(), relative)
                .Concat(extra)
                .Concat(DesktopAppLocator.UninstallExecutables(uninstallToken)
                    .SelectMany(path => DesktopAppLocator.ExpandInstallLocation(path, exeName)))
                .Concat(DesktopAppLocator.StartMenuExecutables(exeName)));
    }
}
