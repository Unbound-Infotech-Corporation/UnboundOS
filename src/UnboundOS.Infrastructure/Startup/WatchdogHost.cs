using System.Runtime.InteropServices;
using Diag = System.Diagnostics;
using System.Text.Json;
using UnboundOS.Core;
using UnboundOS.Core.Shell;

namespace UnboundOS.Infrastructure.Startup;

/// <summary>
/// Headless supervisor. Restarts the UI; falls back to explorer.exe after
/// 3 crashes in 2 minutes, if Shift is held, or if the exe is missing.
/// </summary>
public static class WatchdogHost
{
    private const int VkShift = 0x10;

    public static int Run(string? executablePath = null)
    {
        var exe = executablePath
                  ?? Path.Combine(AppContext.BaseDirectory, "UnboundOS.App.exe");
        var logPath = Path.Combine(UnboundPaths.Root, WatchdogPolicy.WatchdogLogName);
        var crashPath = Path.Combine(UnboundPaths.Root, WatchdogPolicy.CrashLogName);

        if (IsShiftHeld())
        {
            Log(logPath, "Shift held at launch. Starting explorer.exe.");
            StartExplorer();
            return 0;
        }

        if (!File.Exists(exe))
        {
            Log(logPath, $"Missing {exe}. Starting explorer.exe.");
            StartExplorer();
            return 0;
        }

        while (true)
        {
            var started = DateTimeOffset.UtcNow;
            var code = LaunchUi(exe);
            var runtime = DateTimeOffset.UtcNow - started;
            if (!WatchdogPolicy.IsCrashExit(code, runtime))
            {
                Log(logPath, $"UI exited {code} after {runtime.TotalSeconds:0}s. Watching again.");
                continue;
            }

            var crashes = ReadCrashes(crashPath);
            crashes = WatchdogPolicy.Record(crashes, DateTimeOffset.UtcNow).ToList();
            WriteCrashes(crashPath, crashes);
            Log(logPath, $"UI crash/exit {code} after {runtime.TotalSeconds:0}s. {WatchdogPolicy.FallbackReason(crashes, DateTimeOffset.UtcNow)}");
            if (WatchdogPolicy.ShouldFallback(crashes, DateTimeOffset.UtcNow))
            {
                StartExplorer();
                return 0;
            }
        }
    }

    public static async Task RestoreExplorerAsync()
    {
        var replacement = new WindowsShellReplacement();
        await replacement.SetEnabledAsync(false).ConfigureAwait(false);
        StartExplorer();
    }

    public static void StartExplorer()
    {
        try
        {
            Diag.Process.Start(new Diag.ProcessStartInfo
            {
                FileName = "explorer.exe",
                UseShellExecute = true
            });
        }
        catch (Exception)
        {
            // Last resort already failed — user uses Task Manager.
        }
    }

    private static int LaunchUi(string exe)
    {
        try
        {
            using var process = Diag.Process.Start(new Diag.ProcessStartInfo
            {
                FileName = exe,
                WorkingDirectory = Path.GetDirectoryName(exe),
                UseShellExecute = false
            });
            if (process is null)
            {
                return 1;
            }

            process.WaitForExit();
            return process.ExitCode;
        }
        catch (Exception)
        {
            return 1;
        }
    }

    private static List<DateTimeOffset> ReadCrashes(string path)
    {
        try
        {
            if (!File.Exists(path))
            {
                return [];
            }

            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<List<DateTimeOffset>>(json) ?? [];
        }
        catch (Exception)
        {
            return [];
        }
    }

    private static void WriteCrashes(string path, IReadOnlyList<DateTimeOffset> crashes)
    {
        try
        {
            File.WriteAllText(path, JsonSerializer.Serialize(crashes));
        }
        catch (IOException)
        {
            // ignored
        }
        catch (UnauthorizedAccessException)
        {
            // ignored
        }
    }

    private static void Log(string path, string line)
    {
        try
        {
            File.AppendAllText(path, $"{DateTime.UtcNow:O} {line}{Environment.NewLine}");
        }
        catch (IOException)
        {
            // ignored
        }
        catch (UnauthorizedAccessException)
        {
            // ignored
        }
    }

    private static bool IsShiftHeld()
    {
        if (!OperatingSystem.IsWindows())
        {
            return false;
        }

        try
        {
            return (GetAsyncKeyState(VkShift) & 0x8000) != 0;
        }
        catch (Exception)
        {
            return false;
        }
    }

    [DllImport("user32.dll")]
    private static extern short GetAsyncKeyState(int vKey);
}
