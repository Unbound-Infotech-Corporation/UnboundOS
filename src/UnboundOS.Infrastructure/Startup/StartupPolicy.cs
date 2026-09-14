using UnboundOS.Core.Models;
using UnboundOS.Infrastructure.Process;

namespace UnboundOS.Infrastructure.Startup;

/// <summary>
/// Classifies startup entries. Protected names are never Review.
/// Spec: docs/os-spec.md §2.
/// </summary>
public static class StartupPolicy
{
    private static readonly string[] ProtectedTokens =
    [
        "EasyAntiCheat", "EasyAntiCheat_EOS", "BEService", "BattlEye",
        "vgtray", "vgc", "vgm", "RiotClientServices", "FACEIT",
        "nvcontainer", "NVDisplay.Container", "NVIDIA Display",
        "amdfend", "atieclxx", "atiesrxx", "RadeonSoftware", "AMD Ryzen Master",
        "igfxCUI", "igfxEM", "ArcControl",
        "Vortex", "obs64", "obs32", "Streamlabs OBS", "obs-studio"
    ];

    private static readonly string[] ReviewTokens =
    [
        "Overwolf", "Razer Cortex", "CCleaner", "McAfee", "Norton",
        "CandyCrush", "Spotify", "iTunesHelper", "AdobeARM", "GoogleUpdate",
        "DiscordOverlay", "NVIDIA Overlay", "GameBar", "XboxGameBar"
    ];

    public static StartupEntry Classify(StartupCandidate candidate, IReadOnlySet<string> pinnedIds)
    {
        ArgumentNullException.ThrowIfNull(candidate);
        ArgumentNullException.ThrowIfNull(pinnedIds);

        var haystack = $"{candidate.Name} {candidate.Command}";
        if (IsProtected(haystack))
        {
            return ToEntry(
                candidate,
                StartupDisposition.Protected,
                "Protected (anticheat, GPU vendor, Vortex, OBS, or HardProtect).",
                pinned: false);
        }

        if (pinnedIds.Contains(candidate.Id))
        {
            return ToEntry(candidate, StartupDisposition.Pinned, "Pinned by you.", pinned: true);
        }

        if (LooksLikeBloat(haystack))
        {
            return ToEntry(
                candidate,
                StartupDisposition.Review,
                "Common overlay/bloat name. Pin to keep, or apply recommended to drop user Run / Startup folder items.",
                pinned: false);
        }

        return ToEntry(candidate, StartupDisposition.Keep, "Not flagged. Left running unless you pin or review it.", pinned: false);
    }

    public static bool IsProtected(string haystack)
    {
        if (string.IsNullOrWhiteSpace(haystack))
        {
            return false;
        }

        if (ProtectedTokens.Any(token => haystack.Contains(token, StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }

        foreach (var token in haystack.Split([' ', '\\', '/', '"', ',', ';', '.'], StringSplitOptions.RemoveEmptyEntries))
        {
            if (ProcessGuardian.IsAlwaysProtected(token))
            {
                return true;
            }
        }

        return false;
    }

    public static bool LooksLikeBloat(string haystack) =>
        !string.IsNullOrWhiteSpace(haystack) &&
        ReviewTokens.Any(token => haystack.Contains(token, StringComparison.OrdinalIgnoreCase));

    public static string MakeId(StartupSource source, string name, string command)
    {
        var raw = $"{source}|{name.Trim()}|{command.Trim()}";
        return Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(raw)))[..16];
    }

    private static StartupEntry ToEntry(
        StartupCandidate candidate,
        StartupDisposition disposition,
        string reason,
        bool pinned) =>
        new(
            candidate.Id,
            candidate.Source,
            candidate.Name,
            candidate.Command,
            candidate.Location,
            disposition,
            reason,
            pinned);
}
