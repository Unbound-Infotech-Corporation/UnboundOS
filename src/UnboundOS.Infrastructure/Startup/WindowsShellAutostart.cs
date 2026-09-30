using Microsoft.Win32;
using UnboundOS.Core;
using UnboundOS.Core.Abstractions;

namespace UnboundOS.Infrastructure.Startup;

/// <summary>
/// HKCU Run autostart. Never writes the Explorer Shell= value.
/// </summary>
public sealed class WindowsShellAutostart : IShellAutostart
{
    public const string ValueName = "UnboundOS";
    public const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";

    private readonly string? _executablePath;

    public WindowsShellAutostart(string? executablePath = null)
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
                using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: false);
                var value = key?.GetValue(ValueName) as string;
                return !string.IsNullOrWhiteSpace(value);
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
            return Task.FromResult((false, OsProductCopy.AutostartHonesty));
        }

        try
        {
            using var key = Registry.CurrentUser.CreateSubKey(RunKeyPath, writable: true);
            if (key is null)
            {
                return Task.FromResult((false, "Could not open HKCU Run."));
            }

            if (!enabled)
            {
                key.DeleteValue(ValueName, throwOnMissingValue: false);
                return Task.FromResult((true, "Autostart off. UnboundOS will not start at sign-in. Explorer is still the Windows shell."));
            }

            var path = ResolveExecutable();
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            {
                return Task.FromResult((false, "UnboundOS.App.exe was not found. Publish a Release build first."));
            }

            key.SetValue(ValueName, Quote(path), RegistryValueKind.String);
            return Task.FromResult((true, "Autostart on via HKCU Run. UnboundOS launches as a normal app. Explorer is not replaced. No Shell=."));
        }
        catch (Exception ex)
        {
            return Task.FromResult((false, $"Could not change autostart: {ex.Message}"));
        }
    }

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
