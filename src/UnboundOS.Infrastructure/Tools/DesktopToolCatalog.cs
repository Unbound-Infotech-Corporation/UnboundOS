using Microsoft.Win32;
using UnboundOS.Core.Abstractions;
using UnboundOS.Core.Models;

namespace UnboundOS.Infrastructure.Tools;

/// <summary>
/// Read-only kit catalog. Discovers OBS, Vortex, Discord, Playnite, and Steam.
/// Missing tools stay listed as Get — never a fake demo install.
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
            SteamGetPath)
    ];

    private DesktopTool Create(
        string id,
        string display,
        string job,
        string? executable,
        IReadOnlyList<string> processNames,
        ToolGetPath getPath,
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
