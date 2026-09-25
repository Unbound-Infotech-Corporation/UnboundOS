using Microsoft.Win32;
using UnboundOS.Core.Abstractions;
using UnboundOS.Core.Models;

namespace UnboundOS.Infrastructure.Session;

/// <summary>
/// Reversible HKCU Game Mode / Game DVR / visual-effects posture.
/// HAGS is HKLM and opt-in. Never writes Defender, Update, VBS, or BCDEdit.
/// </summary>
public sealed class WindowsGamingSkinnyPolicy : IGamingSkinnyPolicy
{
    public Task<GamingSkinnySnapshot> ApplyAsync(SessionProfile profile, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(profile);
        ct.ThrowIfCancellationRequested();

        var actions = new List<string>();
        var snap = new GamingSkinnySnapshot();

        if (!OperatingSystem.IsWindows())
        {
            CollectPlan(profile, actions);
            actions.Add("Registry apply skipped (not Windows).");
            return Task.FromResult(snap with { Actions = actions });
        }

        try
        {
            var gameMode = ReadDword(Registry.CurrentUser, GameBarPath, "AutoGameModeEnabled");
            var allowMode = ReadDword(Registry.CurrentUser, GameBarPath, "AllowAutoGameMode");
            var dvr = ReadDword(Registry.CurrentUser, GameConfigPath, "GameDVR_Enabled");
            var capture = ReadDword(Registry.CurrentUser, GameDvrPath, "AppCaptureEnabled");
            var fx = ReadDword(Registry.CurrentUser, VisualFxPath, "VisualFXSetting");

            snap = new GamingSkinnySnapshot
            {
                GameModeEnabled = gameMode,
                AllowAutoGameMode = allowMode,
                GameDvrEnabled = dvr,
                AppCaptureEnabled = capture,
                VisualFxSetting = fx
            };

            if (profile.ApplyGameMode)
            {
                WriteDword(Registry.CurrentUser, GameBarPath, "AutoGameModeEnabled", 1);
                WriteDword(Registry.CurrentUser, GameBarPath, "AllowAutoGameMode", 1);
                actions.Add("Game Mode on for this session.");
            }

            if (profile.DisableGameDvr)
            {
                WriteDword(Registry.CurrentUser, GameConfigPath, "GameDVR_Enabled", 0);
                WriteDword(Registry.CurrentUser, GameDvrPath, "AppCaptureEnabled", 0);
                actions.Add("Game DVR / background capture off for this session.");
            }

            if (profile.VisualEffectsPerformance)
            {
                WriteDword(Registry.CurrentUser, VisualFxPath, "VisualFXSetting", 2);
                actions.Add("Visual effects set to Performance for this session.");
            }

            actions.Add("Defender, Windows Update, and VBS were not touched.");
        }
        catch (Exception ex)
        {
            CollectPlan(profile, actions);
            actions.Add($"Skinny registry apply incomplete: {ex.Message}");
        }

        return Task.FromResult(snap with { Actions = actions });
    }

    public Task<(bool Succeeded, string Message)> RestoreAsync(
        GamingSkinnySnapshot? snapshot,
        CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        if (snapshot is null)
        {
            return Task.FromResult((true, "No skinny snapshot to restore."));
        }

        if (!OperatingSystem.IsWindows())
        {
            return Task.FromResult((true, "Skinny restore skipped (not Windows)."));
        }

        try
        {
            RestoreDword(Registry.CurrentUser, GameBarPath, "AutoGameModeEnabled", snapshot.GameModeEnabled);
            RestoreDword(Registry.CurrentUser, GameBarPath, "AllowAutoGameMode", snapshot.AllowAutoGameMode);
            RestoreDword(Registry.CurrentUser, GameConfigPath, "GameDVR_Enabled", snapshot.GameDvrEnabled);
            RestoreDword(Registry.CurrentUser, GameDvrPath, "AppCaptureEnabled", snapshot.AppCaptureEnabled);
            RestoreDword(Registry.CurrentUser, VisualFxPath, "VisualFXSetting", snapshot.VisualFxSetting);
            return Task.FromResult((true, "Restored Game Mode / Game DVR / visual effects."));
        }
        catch (Exception ex)
        {
            return Task.FromResult((false, $"Skinny restore incomplete: {ex.Message}"));
        }
    }

    public Task<(bool Succeeded, string Message)> TrySetHagsAsync(bool enabled, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        if (!OperatingSystem.IsWindows())
        {
            return Task.FromResult((true, enabled
                ? "HAGS preference saved. Apply on Windows and test frametimes — not forced."
                : "HAGS preference saved off. Apply on Windows and test frametimes — not forced."));
        }

        try
        {
            WriteDword(Registry.LocalMachine, HagsPath, "HwSchMode", enabled ? 2 : 1);
            return Task.FromResult((true, enabled
                ? "HAGS set on. Test frametimes in your titles — not forced. Reboot if the GPU driver asks."
                : "HAGS set off. Test frametimes after the change — not forced. Reboot if the GPU driver asks."));
        }
        catch (UnauthorizedAccessException)
        {
            return Task.FromResult((false,
                "HAGS needs an elevated write to HKLM. Preference is saved; run UnboundOS elevated to apply. Test frametimes — not forced."));
        }
        catch (Exception ex)
        {
            return Task.FromResult((false,
                $"Could not write HAGS: {ex.Message} Test frametimes — not forced."));
        }
    }

    private static void CollectPlan(SessionProfile profile, List<string> actions)
    {
        if (profile.ApplyGameMode)
        {
            actions.Add("Game Mode on for this session.");
        }

        if (profile.DisableGameDvr)
        {
            actions.Add("Game DVR / background capture off for this session.");
        }

        if (profile.VisualEffectsPerformance)
        {
            actions.Add("Visual effects set to Performance for this session.");
        }

        actions.Add("Defender, Windows Update, and VBS were not touched.");
    }

    private static int? ReadDword(RegistryKey root, string path, string name)
    {
        using var key = root.OpenSubKey(path);
        var value = key?.GetValue(name);
        return value is int i ? i : null;
    }

    private static void WriteDword(RegistryKey root, string path, string name, int value)
    {
        using var key = root.CreateSubKey(path, true);
        key?.SetValue(name, value, RegistryValueKind.DWord);
    }

    private static void RestoreDword(RegistryKey root, string path, string name, int? value)
    {
        if (value is null)
        {
            return;
        }

        WriteDword(root, path, name, value.Value);
    }

    private const string GameBarPath = @"SOFTWARE\Microsoft\GameBar";
    private const string GameConfigPath = @"System\GameConfigStore";
    private const string GameDvrPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\GameDVR";
    private const string VisualFxPath = @"Software\Microsoft\Windows\CurrentVersion\Explorer\VisualEffects";
    private const string HagsPath = @"SYSTEM\CurrentControlSet\Control\GraphicsDrivers";
}
