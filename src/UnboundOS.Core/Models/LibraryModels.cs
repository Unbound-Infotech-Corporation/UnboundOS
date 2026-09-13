namespace UnboundOS.Core.Models;

public enum GameStore
{
    Steam = 0,
    Epic = 1
}

public sealed record LibraryGame(
    string Id,
    string DisplayName,
    GameStore Store,
    string? InstallPath,
    string LaunchUri,
    string? ExecutablePath,
    string? ProcessName,
    string? ArtworkUrl,
    IReadOnlyList<string> ProcessHints);

public sealed class SessionEnterContext
{
    public required SessionProfile Profile { get; init; }
    public bool ApplyTrafficSeparation { get; init; } = true;
    public bool ApplyAdvancedOptimizations { get; init; }
    public IReadOnlyList<string> ExtraProtectProcessNames { get; init; } = [];
    public string? GameExecutablePath { get; init; }
    public string? GameProcessName { get; init; }
    public string? GameInstallPath { get; init; }
    public IReadOnlyList<string> GameProcessHints { get; init; } = [];
}

public static class PlatformLaunchProtections
{
    public static IReadOnlyList<string> For(GameStore store) => store switch
    {
        GameStore.Steam => ["Steam", "steamwebhelper", "GameOverlayUI"],
        GameStore.Epic =>
        [
            "EpicGamesLauncher",
            "EpicWebHelper",
            "UnrealCEFSubProcess",
            "EasyAntiCheat_EOS",
            "EOSOverlayRenderer-Win64-Shipping"
        ],
        _ => []
    };

    public static SessionProfile ApplyTo(SessionProfile profile, GameStore store)
    {
        var extra = For(store);
        var protect = profile.ProtectProcessNames
            .Concat(extra)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var terminate = profile.TerminateProcessNames
            .Where(name => protect.All(p => !p.Equals(name, StringComparison.OrdinalIgnoreCase)))
            .ToArray();

        return profile.WithProcessLists(terminate, protect);
    }
}
