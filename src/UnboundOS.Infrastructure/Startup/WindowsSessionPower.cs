using System.Runtime.InteropServices;
using Diag = System.Diagnostics;
using UnboundOS.Core.Abstractions;

namespace UnboundOS.Infrastructure.Startup;

public sealed class WindowsSessionPower : ISessionPower
{
    public Task<(bool Succeeded, string Message)> LockAsync(CancellationToken cancellationToken = default) =>
        RunWindows(() =>
        {
            LockWorkStation();
            return "Locked.";
        });

    public Task<(bool Succeeded, string Message)> SleepAsync(CancellationToken cancellationToken = default) =>
        RunWindows(() =>
        {
            SetSuspendState(hibernate: false, forceCritical: false, disableWakeEvent: false);
            return "Sleep requested.";
        });

    public Task<(bool Succeeded, string Message)> RestartAsync(CancellationToken cancellationToken = default) =>
        Start("shutdown", "/r /t 0", "Restart requested.");

    public Task<(bool Succeeded, string Message)> ShutdownAsync(CancellationToken cancellationToken = default) =>
        Start("shutdown", "/s /t 0", "Shutdown requested.");

    public Task<(bool Succeeded, string Message)> SignOutAsync(CancellationToken cancellationToken = default) =>
        Start("shutdown", "/l", "Sign-out requested.");

    public Task<(bool Succeeded, string Message)> SwitchUserAsync(CancellationToken cancellationToken = default) =>
        Start("tsdiscon", "", "Switch-user requested.");

    private static Task<(bool Succeeded, string Message)> RunWindows(Func<string> work)
    {
        if (!OperatingSystem.IsWindows())
        {
            return Task.FromResult((false, "Power actions run on Windows."));
        }

        try
        {
            return Task.FromResult((true, work()));
        }
        catch (Exception ex)
        {
            return Task.FromResult((false, ex.Message));
        }
    }

    private static Task<(bool Succeeded, string Message)> Start(string file, string args, string ok)
    {
        if (!OperatingSystem.IsWindows())
        {
            return Task.FromResult((false, "Power actions run on Windows."));
        }

        try
        {
            Diag.Process.Start(new Diag.ProcessStartInfo
            {
                FileName = file,
                Arguments = args,
                UseShellExecute = true
            });
            return Task.FromResult((true, ok));
        }
        catch (Exception ex)
        {
            return Task.FromResult((false, ex.Message));
        }
    }

    [DllImport("user32.dll")]
    private static extern bool LockWorkStation();

    [DllImport("powrprof.dll", SetLastError = true)]
    private static extern bool SetSuspendState(bool hibernate, bool forceCritical, bool disableWakeEvent);
}
