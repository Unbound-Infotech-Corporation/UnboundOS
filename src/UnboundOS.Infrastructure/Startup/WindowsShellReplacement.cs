using Microsoft.Win32;
using UnboundOS.Core;
using UnboundOS.Core.Abstractions;
using UnboundOS.Core.Shell;

namespace UnboundOS.Infrastructure.Startup;

/// <summary>
/// Per-user shell replacement. Writes HKCU Winlogon Shell only — never HKLM.
/// </summary>
public sealed class WindowsShellReplacement : IShellReplacement
{
    public const string WinlogonKey = @"Software\Microsoft\Windows NT\CurrentVersion\Winlogon";
    public const string ShellValueName = "Shell";
    public const string MachineWinlogonKey = @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Winlogon";

    private readonly string? _executablePath;

    public WindowsShellReplacement(string? executablePath = null)
    {
        _executablePath = executablePath;
    }

    public bool IsEnabled
    {
        get
        {
            if (!OperatingSystem.IsWindows())
            {
                return false;
            }

            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(WinlogonKey, writable: false);
                var value = key?.GetValue(ShellValueName) as string;
                return LooksLikeUnbound(value);
            }
            catch (Exception)
            {
                return false;
            }
        }
    }

    public Task<(bool Succeeded, string Message)> SetEnabledAsync(bool enabled, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!OperatingSystem.IsWindows())
        {
            return Task.FromResult((false, OsProductCopy.ShellReplacementHonesty));
        }

        try
        {
            using var key = Registry.CurrentUser.CreateSubKey(WinlogonKey, writable: true);
            if (key is null)
            {
                return Task.FromResult((false, "Could not open HKCU Winlogon."));
            }

            if (!enabled)
            {
                key.DeleteValue(ShellValueName, throwOnMissingValue: false);
                return Task.FromResult((true, "Explorer is the shell again (HKCU Shell removed). Sign out to apply."));
            }

            var path = ResolveExecutable();
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            {
                return Task.FromResult((false, "UnboundOS.App.exe was not found. Publish a Release build first. Explorer was not changed."));
            }

            key.SetValue(ShellValueName, $"{Quote(path)} {WatchdogPolicy.WatchdogFlag}", RegistryValueKind.String);
            return Task.FromResult((true, "HKCU Shell= watchdog. Hold Shift at sign-in for Explorer. " + OsProductCopy.ShellReplacementHonesty));
        }
        catch (Exception ex)
        {
            return Task.FromResult((false, $"Could not change HKCU Shell: {ex.Message}"));
        }
    }

    public static bool LooksLikeUnbound(string? value) =>
        !string.IsNullOrWhiteSpace(value) &&
        value.Contains("UnboundOS", StringComparison.OrdinalIgnoreCase);

    private string? ResolveExecutable()
    {
        if (!string.IsNullOrWhiteSpace(_executablePath))
        {
            return _executablePath;
        }

        var fromBase = Path.Combine(AppContext.BaseDirectory, "UnboundOS.App.exe");
        if (File.Exists(fromBase))
        {
            return fromBase;
        }

        return Environment.ProcessPath;
    }

    private static string Quote(string path) =>
        path.Contains(' ', StringComparison.Ordinal) ? $"\"{path}\"" : path;
}
