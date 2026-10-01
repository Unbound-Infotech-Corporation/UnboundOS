using UnboundOS.Core.Models;

namespace UnboundOS.Core.Shell;

/// <summary>
/// Settings people actually open. First-party where we have a page;
/// otherwise a live <c>ms-settings:</c> URI so nothing is a dead end.
/// </summary>
public static class OsSettingsCatalog
{
    public static IReadOnlyList<OsSettingsEntry> All { get; } =
    [
        new("network", "Wi-Fi / Ethernet", "Adapter list from this PC. Pairing UI is Windows.",
            "ms-settings:network", true),
        new("bluetooth", "Bluetooth", "Pairing stays in Windows Settings.",
            "ms-settings:bluetooth", false),
        new("audio", "Audio", "Volume keys in the shell. Device picker is Windows.",
            "ms-settings:sound", true),
        new("display", "Display", "Deep GPU options go to NVIDIA App / AMD Adrenalin.",
            "ms-settings:display", true),
        new("controllers", "Controllers", "Xbox / game-bar pairing is Windows.",
            "ms-settings:gaming-gamebar", false),
        new("power", "Power plans", "powercfg list / set. Sleep UI is Windows.",
            "ms-settings:powersleep", true),
        new("storage", "Storage", "Drive free space in Files. Storage Sense is Windows.",
            "ms-settings:storagesense", true),
        new("apps", "Apps", "Installed list + uninstall string. Store is Windows.",
            "ms-settings:appsfeatures", true),
        new("time", "Time / language", "Clock is in the shell chrome. Region is Windows.",
            "ms-settings:dateandtime", false),
        new("language", "Language", "Input language is Windows.",
            "ms-settings:regionlanguage", false),
        new("updates", "Windows Update", "Update Guard: quality on, feature deferred.",
            "ms-settings:windowsupdate", true),
        new("accounts", "User accounts", "Sign-in and auto sign-in stay Windows (netplwiz).",
            "ms-settings:accounts", false),
        new("xboxmode", "Xbox mode / FSE", "Choose home app if the picker exists. Failure stays on Run + fullscreen.",
            "ms-settings:gaming-fullscreen", true),
        new("signin", "Auto sign-in", "Windows netplwiz / Sign-in options. UnboundOS does not ship a credential provider.",
            "ms-settings:signinoptions", false)
    ];

    public static OsSettingsEntry? Find(string id) =>
        All.FirstOrDefault(entry => string.Equals(entry.Id, id, StringComparison.OrdinalIgnoreCase));
}
