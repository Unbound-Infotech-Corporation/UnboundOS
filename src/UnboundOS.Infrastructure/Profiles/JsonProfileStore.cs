using System.Text.Json;
using System.Text.Json.Serialization;
using UnboundOS.Core.Abstractions;
using UnboundOS.Core.Models;

namespace UnboundOS.Infrastructure.Profiles;

public sealed class JsonProfileStore : IProfileStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly string _path;

    public JsonProfileStore(string? rootDirectory = null)
    {
        var root = rootDirectory ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Unbound Infotech Corporation",
            "UnboundOS");
        Directory.CreateDirectory(root);
        _path = Path.Combine(root, "profiles.json");
    }

    public async Task EnsureDefaultsAsync(CancellationToken ct = default)
    {
        if (File.Exists(_path))
        {
            return;
        }

        await SaveAsync(CreateDefaults(), ct).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<SessionProfile>> LoadAsync(CancellationToken ct = default)
    {
        await EnsureDefaultsAsync(ct).ConfigureAwait(false);
        await using var stream = File.OpenRead(_path);
        var profiles = await JsonSerializer.DeserializeAsync<List<SessionProfile>>(stream, JsonOptions, ct)
            .ConfigureAwait(false);
        return profiles is { Count: > 0 } ? profiles : CreateDefaults();
    }

    public async Task SaveAsync(IEnumerable<SessionProfile> profiles, CancellationToken ct = default)
    {
        await using var stream = File.Create(_path);
        await JsonSerializer.SerializeAsync(stream, profiles.ToList(), JsonOptions, ct).ConfigureAwait(false);
    }

    public static IReadOnlyList<SessionProfile> CreateDefaults() =>
    [
        new SessionProfile
        {
            Id = "competitive",
            Name = "Competitive Edge",
            Description = "Strip overlays and chat. Game Mode on, Game DVR off, Ultimate Performance while live. Explorer stays for EAC/Vanguard. Restore on exit.",
            Kind = ProfileKind.Competitive,
            DisableGameDvr = true,
            VisualEffectsPerformance = true,
            TerminateProcessNames =
            [
                "OneDrive", "Microsoft.SharePoint", "SkypeApp", "SkypeBridge",
                "YourPhone", "PhoneExperienceHost", "Widgets", "msedge",
                "chrome", "firefox", "Discord", "EpicGamesLauncher", "steamwebhelper",
                "AdobeUpdateService", "CCXProcess", "Creative Cloud", "iCUE",
                "ArmouryCrate", "RGBFusion", "LogiOverlay", "Overwolf", "Rainmeter"
            ],
            ProtectProcessNames = ["Steam", "EasyAntiCheat", "RiotClientServices", "vgtray", "vgc", "Vortex"],
            GameProcessHints = ["cs2", "valorant", "r5apex", "fortniteclient-win64-shipping"]
        },
        new SessionProfile
        {
            Id = "streamer",
            Name = "Streamer Split",
            Description = "Game NIC + stream NIC. Keep OBS/Discord/capture. Game Mode on; Game DVR stays. Ultrawide crop plan for Twitch 1080p60.",
            Kind = ProfileKind.Streamer,
            DisableGameDvr = false,
            VisualEffectsPerformance = true,
            TerminateProcessNames =
            [
                "OneDrive", "Microsoft.SharePoint", "YourPhone", "Widgets",
                "AdobeUpdateService", "CCXProcess", "iCUE", "ArmouryCrate"
            ],
            ProtectProcessNames = ["obs64", "obs32", "Streamlabs OBS", "discord", "Vortex", "Steam", "Rainmeter", "MusicBee"],
            StreamProcessHints = ["obs64", "obs32", "Streamlabs OBS"],
            Stream = new StreamPreferences
            {
                SourceWidth = 5110,
                SourceHeight = 1400,
                OutputWidth = 1920,
                OutputHeight = 1080,
                OutputFps = 60,
                TargetBitrateKbps = 6000,
                SharpenAmount = 0.35
            }
        },
        new SessionProfile
        {
            Id = "living-room",
            Name = "Living Room Shell",
            Description = "Big-picture calm. Game Mode on. Visual effects stay pretty. Fewer popups, console-like focus.",
            Kind = ProfileKind.LivingRoom,
            DisableGameDvr = false,
            VisualEffectsPerformance = false,
            TerminateProcessNames =
            [
                "OneDrive", "Widgets", "YourPhone", "Microsoft.SharePoint",
                "Teams", "ms-teams", "Outlook", "HxOutlook"
            ],
            ProtectProcessNames = ["Steam", "Playnite.DesktopApp", "Playnite.FullscreenApp", "Vortex", "Rainmeter", "MusicBee"]
        }
    ];
}
