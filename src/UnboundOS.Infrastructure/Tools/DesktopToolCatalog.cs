using Microsoft.Win32;
using UnboundOS.Core.Abstractions;
using UnboundOS.Core.Models;

namespace UnboundOS.Infrastructure.Tools;

/// <summary>
/// Read-only kit + utilities catalog. Missing tools stay listed as Get —
/// never a fake demo install. Utilities stay a short, obvious set.
/// </summary>
public sealed class DesktopToolCatalog(DesktopToolDiscoverySettings? settings = null) : IDesktopToolCatalog
{
    public static ToolGetPath ObsGetPath { get; } =
        new("Official site", "https://obsproject.com/download");

    public static ToolGetPath VortexGetPath { get; } =
        new("Official site", "https://www.nexusmods.com/about/vortex");

    public static ToolGetPath DiscordGetPath { get; } =
        new("Official site", "https://discord.com/download");

    public static ToolGetPath PlayniteGetPath { get; } =
        new("Official site", "https://playnite.link");

    public static ToolGetPath SteamGetPath { get; } =
        new("Official site", "https://store.steampowered.com/about/");

    public static ToolGetPath NotepadPlusPlusGetPath { get; } =
        new("Official site", "https://notepad-plus-plus.org/downloads/");

    public static ToolGetPath SevenZipGetPath { get; } =
        new("Official site", "https://www.7-zip.org/");

    public static ToolGetPath HwInfoGetPath { get; } =
        new("Official site", "https://www.hwinfo.com/download/");

    public static ToolGetPath RainmeterGetPath { get; } =
        new("Official site", "https://www.rainmeter.net/");

    /// <summary>Recommended starter skin over galaxy Home. Link only — do not ship the .rmskin.</summary>
    public static ToolGetPath PhenixGetPath { get; } =
        new("Phenix theme", "https://visualskins.com/skin/phenix");

    private readonly DesktopToolDiscoverySettings _settings = settings ?? new DesktopToolDiscoverySettings();

    public Task<IReadOnlyList<DesktopTool>> DiscoverAsync(CancellationToken cancellationToken = default) =>
        Task.Run(() => Discover(), cancellationToken);

    internal IReadOnlyList<DesktopTool> Discover() =>
    [
        Create(
            DesktopToolIds.Obs,
            "OBS Studio",
            "Capture and stream. Play native — crop in OBS, never lower the monitor.",
            FindObs(),
            ["obs64", "obs32", "Streamlabs OBS"],
            ObsGetPath,
            HasObsRecipe: true),
        Create(
            DesktopToolIds.Vortex,
            "Vortex",
            "Nexus Mods manager. UnboundOS discovers and opens it; Vortex stays in charge.",
            FindForcedOr(_settings.FindVortex),
            ["Vortex"],
            VortexGetPath),
        Create(
            DesktopToolIds.Discord,
            "Discord",
            "Voice and chat. Streamer protects it; Competitive can still close it.",
            FindDiscord(),
            ["Discord"],
            DiscordGetPath),
        Create(
            DesktopToolIds.Playnite,
            "Playnite",
            "Library shell for the games you already own.",
            FindPlaynite(),
            ["Playnite.DesktopApp", "Playnite.FullscreenApp"],
            PlayniteGetPath),
        Create(
            DesktopToolIds.Steam,
            "Steam",
            "Launch the client you already use. Workshop stays on the Mods page.",
            FindSteam(),
            ["Steam"],
            SteamGetPath),
        Create(
            DesktopToolIds.NotepadPlusPlus,
            "Notepad++",
            "Edit text, configs, and logs. Get opens the official downloads page.",
            FindNotepadPlusPlus(),
            ["notepad++"],
            NotepadPlusPlusGetPath,
            DesktopToolGroup.Utility),
        Create(
            DesktopToolIds.SevenZip,
            "7-Zip",
            "Open archives. Get opens the official 7-Zip page.",
            FindSevenZip(),
            ["7zFM", "7z"],
            SevenZipGetPath,
            DesktopToolGroup.Utility),
        Create(
            DesktopToolIds.HwInfo,
            "HWiNFO",
            "Optional sensors. UnboundOS does not bundle HWiNFO; Open launches the official app if installed.",
            FindHwInfo(),
            ["HWiNFO64", "HWiNFO32", "HWiNFO"],
            HwInfoGetPath,
            DesktopToolGroup.Utility),
        Create(
            DesktopToolIds.Rainmeter,
            "Rainmeter",
            "Desktop skins over the galaxy. Open launches Rainmeter if installed. Get is the official Rainmeter page — UnboundOS does not ship skins.",
            FindRainmeter(),
            ["Rainmeter"],
            RainmeterGetPath),
        Create(
            DesktopToolIds.Phenix,
            "Phenix",
            "Recommended Rainmeter starter over Home (not a clock-only skin). Get opens the official Phenix page. UnboundOS does not redistribute the .rmskin.",
            null,
            [],
            PhenixGetPath)
    ];

    private DesktopTool Create(
        string id,
        string display,
        string job,
        string? executable,
        IReadOnlyList<string> processNames,
        ToolGetPath getPath,
        DesktopToolGroup group = DesktopToolGroup.Kit,
        bool HasObsRecipe = false)
    {
        var installed = !string.IsNullOrWhiteSpace(executable) && File.Exists(executable);
        return new DesktopTool(
            id,
            display,
            job,
            installed,
            installed ? executable : null,
            processNames,
            getPath,
            group,
            HasObsRecipe);
    }

    private string? FindForcedOr(Func<string?> fallback)
    {
        if (TryForced(DesktopToolIds.Vortex, out var forced))
        {
            return forced;
        }

        return _settings.UseDefaultWindowsLocations ? fallback() : null;
    }

    private string? FindObs()
    {
        if (TryForced(DesktopToolIds.Obs, out var forced))
        {
            return forced;
        }

        if (!_settings.UseDefaultWindowsLocations)
        {
            return null;
        }

        var relative =
            new[]
            {
                Path.Combine("obs-studio", "bin", "64bit", "obs64.exe"),
                Path.Combine("obs-studio", "bin", "32bit", "obs32.exe"),
                Path.Combine("Streamlabs OBS", "Streamlabs OBS.exe"),
                Path.Combine("StreamlabsOBS", "Streamlabs OBS.exe")
            };

        return DesktopAppLocator.FindFirstExisting(
            DesktopAppLocator.Combine(DesktopAppLocator.ProgramRoots(), relative)
                .Concat(DesktopAppLocator.UninstallExecutables("OBS Studio", "Streamlabs OBS")
                    .SelectMany(path => DesktopAppLocator.ExpandInstallLocation(
                        path,
                        Path.Combine("bin", "64bit", "obs64.exe"),
                        Path.Combine("bin", "32bit", "obs32.exe"),
                        "obs64.exe",
                        "Streamlabs OBS.exe")))
                .Concat(DesktopAppLocator.StartMenuExecutables("obs64.exe", "obs32.exe")));
    }

    private string? FindDiscord()
    {
        if (TryForced(DesktopToolIds.Discord, out var forced))
        {
            return forced;
        }

        if (!_settings.UseDefaultWindowsLocations)
        {
            return null;
        }

        var local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var discordRoot = Path.Combine(local, "Discord");
        return DesktopAppLocator.FindFirstExisting(
            DesktopAppLocator.NewestAppFolderExecutables(discordRoot, "Discord.exe")
                .Concat(DesktopAppLocator.Combine(
                    DesktopAppLocator.ProgramRoots(),
                    Path.Combine("Discord", "Discord.exe"),
                    Path.Combine("Discord", "Update.exe")))
                .Concat(DesktopAppLocator.UninstallExecutables("Discord")
                    .SelectMany(path => DesktopAppLocator.ExpandInstallLocation(path, "Discord.exe", "Update.exe")))
                .Concat(DesktopAppLocator.StartMenuExecutables("Discord.exe")));
    }

    private string? FindPlaynite()
    {
        if (TryForced(DesktopToolIds.Playnite, out var forced))
        {
            return forced;
        }

        if (!_settings.UseDefaultWindowsLocations)
        {
            return null;
        }

        return DesktopAppLocator.FindFirstExisting(
            DesktopAppLocator.Combine(
                    DesktopAppLocator.ProgramRoots(),
                    Path.Combine("Playnite", "Playnite.DesktopApp.exe"),
                    Path.Combine("Playnite", "Playnite.FullscreenApp.exe"))
                .Concat(DesktopAppLocator.UninstallExecutables("Playnite")
                    .SelectMany(path => DesktopAppLocator.ExpandInstallLocation(
                        path,
                        "Playnite.DesktopApp.exe",
                        "Playnite.FullscreenApp.exe")))
                .Concat(DesktopAppLocator.StartMenuExecutables(
                    "Playnite.DesktopApp.exe",
                    "Playnite.FullscreenApp.exe")));
    }

    private string? FindSteam()
    {
        if (TryForced(DesktopToolIds.Steam, out var forced))
        {
            return forced;
        }

        if (!_settings.UseDefaultWindowsLocations)
        {
            return null;
        }

        var fromRegistry = new[]
        {
            ReadSteamPath(Registry.CurrentUser, @"Software\Valve\Steam", "SteamPath"),
            ReadSteamPath(Registry.LocalMachine, @"SOFTWARE\WOW6432Node\Valve\Steam", "InstallPath"),
            ReadSteamPath(Registry.LocalMachine, @"SOFTWARE\Valve\Steam", "InstallPath")
        }.Select(root => root is null ? null : Path.Combine(root, "steam.exe"));

        return DesktopAppLocator.FindFirstExisting(
            fromRegistry
                .Concat(DesktopAppLocator.Combine(
                    DesktopAppLocator.ProgramRoots(),
                    Path.Combine("Steam", "steam.exe")))
                .Concat(DesktopAppLocator.UninstallExecutables("Steam")
                    .SelectMany(path => DesktopAppLocator.ExpandInstallLocation(path, "steam.exe")))
                .Concat(DesktopAppLocator.StartMenuExecutables("steam.exe")));
    }

    private string? FindNotepadPlusPlus()
    {
        if (TryForced(DesktopToolIds.NotepadPlusPlus, out var forced))
        {
            return forced;
        }

        if (!_settings.UseDefaultWindowsLocations)
        {
            return null;
        }

        return DesktopAppLocator.FindFirstExisting(
            DesktopAppLocator.Combine(
                    DesktopAppLocator.ProgramRoots(),
                    Path.Combine("Notepad++", "notepad++.exe"))
                .Concat(DesktopAppLocator.UninstallExecutables("Notepad++")
                    .SelectMany(path => DesktopAppLocator.ExpandInstallLocation(path, "notepad++.exe")))
                .Concat(DesktopAppLocator.StartMenuExecutables("notepad++.exe")));
    }

    private string? FindSevenZip()
    {
        if (TryForced(DesktopToolIds.SevenZip, out var forced))
        {
            return forced;
        }

        if (!_settings.UseDefaultWindowsLocations)
        {
            return null;
        }

        return DesktopAppLocator.FindFirstExisting(
            DesktopAppLocator.Combine(
                    DesktopAppLocator.ProgramRoots(),
                    Path.Combine("7-Zip", "7zFM.exe"),
                    Path.Combine("7-Zip", "7z.exe"))
                .Concat(DesktopAppLocator.UninstallExecutables("7-Zip")
                    .SelectMany(path => DesktopAppLocator.ExpandInstallLocation(path, "7zFM.exe", "7z.exe")))
                .Concat(DesktopAppLocator.StartMenuExecutables("7zFM.exe", "7z.exe")));
    }

    private string? FindHwInfo()
    {
        if (TryForced(DesktopToolIds.HwInfo, out var forced))
        {
            return forced;
        }

        if (!_settings.UseDefaultWindowsLocations)
        {
            return null;
        }

        return DesktopAppLocator.FindFirstExisting(
            DesktopAppLocator.Combine(
                    DesktopAppLocator.ProgramRoots(),
                    Path.Combine("HWiNFO64", "HWiNFO64.exe"),
                    Path.Combine("HWiNFO32", "HWiNFO32.exe"),
                    Path.Combine("HWiNFO", "HWiNFO64.exe"))
                .Concat(DesktopAppLocator.UninstallExecutables("HWiNFO")
                    .SelectMany(path => DesktopAppLocator.ExpandInstallLocation(path, "HWiNFO64.exe", "HWiNFO32.exe")))
                .Concat(DesktopAppLocator.StartMenuExecutables("HWiNFO64.exe", "HWiNFO32.exe")));
    }

    private string? FindRainmeter()
    {
        if (TryForced(DesktopToolIds.Rainmeter, out var forced))
        {
            return forced;
        }

        if (!_settings.UseDefaultWindowsLocations)
        {
            return null;
        }

        return DesktopAppLocator.FindFirstExisting(
            DesktopAppLocator.Combine(
                    DesktopAppLocator.ProgramRoots(),
                    Path.Combine("Rainmeter", "Rainmeter.exe"))
                .Concat(DesktopAppLocator.UninstallExecutables("Rainmeter")
                    .SelectMany(path => DesktopAppLocator.ExpandInstallLocation(path, "Rainmeter.exe")))
                .Concat(DesktopAppLocator.StartMenuExecutables("Rainmeter.exe")));
    }

    private bool TryForced(string id, out string? path)
    {
        if (_settings.ForcedExecutables.TryGetValue(id, out var forced))
        {
            path = string.IsNullOrWhiteSpace(forced) ? null : forced;
            return true;
        }

        path = null;
        return false;
    }

    private static string? ReadSteamPath(RegistryKey hive, string subKey, string valueName)
    {
        try
        {
            return hive.OpenSubKey(subKey)?.GetValue(valueName) as string;
        }
        catch
        {
            return null;
        }
    }
}
