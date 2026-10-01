using UnboundOS.Core.Models;

namespace UnboundOS.Core.Shell;

/// <summary>
/// Exact community FSE home-app contract (OmniConsole, AnyFSE, Playnite FSE,
/// FullScreenExperienceShell). Unpackaged exe cannot appear in Choose home app.
/// </summary>
public static class XboxModePolicy
{
    public const string PackageIdentity = "UnboundInfotech.UnboundOS.FseHome";
    public const string GamingAppExtension = "windows.gamingApp";
    public const string GamingHomeCapability = "Microsoft.appCategory.gamingHome_8wekyb3d8bbwe";
    public const string FullscreenFlag = "--fullscreen";
    public const string XboxHomeFlag = "--xbox-home";
    public const string PreferenceFileName = "xbox-mode-home.json";

    public static IReadOnlyList<string> SettingsUris { get; } =
    [
        "ms-settings:gaming-fullscreen",
        "ms-settings:gaming-xboxmode",
        "ms-settings:gaming"
    ];

    public static bool IsFullscreenRequested(IEnumerable<string>? args)
    {
        if (args is null)
        {
            return false;
        }

        return args.Any(arg =>
            string.Equals(arg, FullscreenFlag, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(arg, XboxHomeFlag, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(arg, "/fullscreen", StringComparison.OrdinalIgnoreCase));
    }

    public static XboxModeHomeState ParseState(string? value)
    {
        if (Enum.TryParse<XboxModeHomeState>(value, ignoreCase: true, out var state))
        {
            return state;
        }

        return XboxModeHomeState.Unavailable;
    }

    public static XboxModeHomeState Resolve(
        bool wanted,
        bool packagePresent,
        bool userConfirmedSelected,
        bool runAutostartOn)
    {
        if (userConfirmedSelected && packagePresent)
        {
            return XboxModeHomeState.Selected;
        }

        if (!wanted)
        {
            return packagePresent
                ? XboxModeHomeState.RegisteredNotSelected
                : XboxModeHomeState.Unavailable;
        }

        if (packagePresent)
        {
            return XboxModeHomeState.RegisteredNotSelected;
        }

        return runAutostartOn ? XboxModeHomeState.FallbackRun : XboxModeHomeState.PackageMissing;
    }

    public static string Describe(XboxModeHomeState state) =>
        state switch
        {
            XboxModeHomeState.Selected =>
                "Xbox mode home is Selected. Taskbar/Start stay deferred by Windows FSE.",
            XboxModeHomeState.RegisteredNotSelected =>
                "Package is registered. Pick UnboundOS in Settings → Gaming → Xbox mode. Until then, Run + fullscreen is the fallback.",
            XboxModeHomeState.PackageMissing =>
                "FSE package is missing. Sideload packaging/fse (Developer Mode) or stay on Run + fullscreen.",
            XboxModeHomeState.FallbackRun =>
                "FallbackRun: HKCU Run + fullscreen. Explorer is still the shell. FSE picker was missing or sideload failed.",
            _ =>
                "Xbox mode / FSE is Unavailable on this PC (Limited edition, not Windows, or probe failed). Stage 0 Run + fullscreen still works."
        };
}
