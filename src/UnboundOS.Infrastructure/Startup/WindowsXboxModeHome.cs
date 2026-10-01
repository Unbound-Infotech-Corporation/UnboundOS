using System.Text.Json;
using UnboundOS.Core;
using UnboundOS.Core.Abstractions;
using UnboundOS.Core.Models;
using UnboundOS.Core.Shell;
using Diag = System.Diagnostics;

namespace UnboundOS.Infrastructure.Startup;

/// <summary>
/// Stage 1 Xbox-mode home toggle. Sideload is best-effort. Any failure
/// stays on Stage 0 (HKCU Run + fullscreen). Never writes HKLM FSE switches.
/// </summary>
public sealed class WindowsXboxModeHome : IXboxModeHome
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    private readonly string _preferencePath;
    private readonly IShellAutostart _autostart;
    private readonly Func<string, bool> _openUri;
    private readonly Func<bool> _packageProbe;

    public WindowsXboxModeHome(
        string? preferencePath = null,
        IShellAutostart? autostart = null,
        Func<string, bool>? openUri = null,
        Func<bool>? packageProbe = null)
    {
        _preferencePath = preferencePath ?? Path.Combine(UnboundPaths.Root, XboxModePolicy.PreferenceFileName);
        _autostart = autostart ?? new WindowsShellAutostart();
        _openUri = openUri ?? DefaultOpenUri;
        _packageProbe = packageProbe ?? ProbeLivePackage;
    }

    public bool IsWanted => LoadPreference().Wanted;

    public XboxModeHomeSnapshot Probe()
    {
        var pref = LoadPreference();
        var package = SafePackage();
        var state = XboxModePolicy.Resolve(pref.Wanted, package, pref.UserConfirmedSelected, _autostart.IsEnabled);
        var detail = string.IsNullOrWhiteSpace(pref.Detail)
            ? XboxModePolicy.Describe(state)
            : pref.Detail + " " + XboxModePolicy.Describe(state);
        return new(state, pref.Wanted, pref.FullscreenAtStartup, package, pref.UserConfirmedSelected, detail.Trim());
    }

    public async Task<(bool Succeeded, string Message)> SetWantedAsync(bool wanted, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            if (wanted)
            {
                var auto = await _autostart.SetEnabledAsync(true, cancellationToken).ConfigureAwait(false);
                var package = SafePackage();
                var state = XboxModePolicy.Resolve(wanted: true, package, userConfirmedSelected: false, runAutostartOn: auto.Succeeded || _autostart.IsEnabled);
                SavePreference(new XboxModeHomePreference(
                    true,
                    true,
                    state.ToString(),
                    XboxModePolicy.Describe(state),
                    false,
                    DateTimeOffset.UtcNow));

                var opened = await OpenXboxModeSettingsAsync(cancellationToken).ConfigureAwait(false);
                var message = state == XboxModeHomeState.FallbackRun || state == XboxModeHomeState.PackageMissing
                    ? "Xbox mode home requested. Sideload or picker failed — Stage 0 Run + fullscreen is on. " + OsProductCopy.XboxModeHonesty
                    : "Xbox mode home requested. Pick UnboundOS in Settings → Gaming → Xbox mode if the picker exists. " + OsProductCopy.XboxModeHonesty;
                if (!opened.Succeeded)
                {
                    message += " " + opened.Message;
                }

                return (true, message);
            }

            SavePreference(new XboxModeHomePreference(
                false,
                false,
                XboxModeHomeState.Unavailable.ToString(),
                "Xbox mode home off. HKCU Run is unchanged.",
                false,
                DateTimeOffset.UtcNow));
            return (true, "Xbox mode home off. Fully reversible. Stage 0 Run is unchanged unless you turn autostart off.");
        }
        catch (Exception ex)
        {
            SavePreference(new XboxModeHomePreference(
                wanted,
                wanted,
                XboxModeHomeState.FallbackRun.ToString(),
                ex.Message,
                false,
                DateTimeOffset.UtcNow));
            return (true, "Xbox mode toggle fell back to Run + fullscreen. " + ex.Message);
        }
    }

    public Task<(bool Succeeded, string Message)> ConfirmSelectedAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var current = LoadPreference();
        if (!SafePackage())
        {
            return Task.FromResult((false, "Package is not present. Stay on FallbackRun."));
        }

        SavePreference(current with
        {
            UserConfirmedSelected = true,
            State = XboxModeHomeState.Selected.ToString(),
            Detail = XboxModePolicy.Describe(XboxModeHomeState.Selected),
            UpdatedUtc = DateTimeOffset.UtcNow
        });
        return Task.FromResult((true, XboxModePolicy.Describe(XboxModeHomeState.Selected)));
    }

    public Task<(bool Succeeded, string Message)> OpenXboxModeSettingsAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        foreach (var uri in XboxModePolicy.SettingsUris)
        {
            try
            {
                if (_openUri(uri))
                {
                    return Task.FromResult((true, "Opened " + uri));
                }
            }
            catch (Exception)
            {
                // Try the next documented-or-guessed URI.
            }
        }

        return Task.FromResult((false, "Could not open Xbox mode Settings. Use Settings → Gaming if the page exists."));
    }

    private XboxModeHomePreference LoadPreference()
    {
        try
        {
            if (File.Exists(_preferencePath))
            {
                var loaded = JsonSerializer.Deserialize<XboxModeHomePreference>(
                    File.ReadAllText(_preferencePath), JsonOptions);
                if (loaded is not null)
                {
                    return loaded;
                }
            }
        }
        catch (Exception)
        {
            // Missing or corrupt preference is Unavailable.
        }

        return new(false, false, nameof(XboxModeHomeState.Unavailable), "", false, DateTimeOffset.UnixEpoch);
    }

    private void SavePreference(XboxModeHomePreference preference)
    {
        var dir = Path.GetDirectoryName(_preferencePath);
        if (!string.IsNullOrWhiteSpace(dir))
        {
            Directory.CreateDirectory(dir);
        }

        File.WriteAllText(_preferencePath, JsonSerializer.Serialize(preference, JsonOptions));
    }

    private bool SafePackage()
    {
        try
        {
            return _packageProbe();
        }
        catch (Exception)
        {
            return false;
        }
    }

    private static bool ProbeLivePackage()
    {
        if (!OperatingSystem.IsWindows())
        {
            return false;
        }

        try
        {
            var start = new Diag.ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = "-NoProfile -Command \"Get-AppxPackage -Name UnboundInfotech.UnboundOS.FseHome | Select-Object -ExpandProperty PackageFullName\"",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using var process = Diag.Process.Start(start);
            var text = process?.StandardOutput.ReadToEnd() ?? "";
            process?.WaitForExit(4000);
            return text.Contains(XboxModePolicy.PackageIdentity, StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception)
        {
            return false;
        }
    }

    private static bool DefaultOpenUri(string uri)
    {
        if (!OperatingSystem.IsWindows())
        {
            return false;
        }

        var info = new Diag.ProcessStartInfo(uri) { UseShellExecute = true };
        return Diag.Process.Start(info) is not null;
    }
}
