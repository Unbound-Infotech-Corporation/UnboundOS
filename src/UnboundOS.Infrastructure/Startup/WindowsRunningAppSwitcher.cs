using System.Runtime.InteropServices;
using Diag = System.Diagnostics;
using System.Text;
using UnboundOS.Core.Abstractions;
using UnboundOS.Core.Models;

namespace UnboundOS.Infrastructure.Startup;

public sealed class WindowsRunningAppSwitcher : IRunningAppSwitcher
{
    public Task<IReadOnlyList<RunningApp>> ListAsync(CancellationToken cancellationToken = default)
    {
        if (!OperatingSystem.IsWindows())
        {
            return Task.FromResult<IReadOnlyList<RunningApp>>([]);
        }

        var found = new List<RunningApp>();
        EnumWindows((hwnd, _) =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!IsWindowVisible(hwnd) || GetWindowTextLength(hwnd) == 0)
            {
                return true;
            }

            var title = ReadTitle(hwnd);
            if (string.IsNullOrWhiteSpace(title))
            {
                return true;
            }

            GetWindowThreadProcessId(hwnd, out var pid);
            string processName;
            try
            {
                processName = Diag.Process.GetProcessById((int)pid).ProcessName;
            }
            catch (Exception)
            {
                processName = "?";
            }

            if (processName.Equals("explorer", StringComparison.OrdinalIgnoreCase) &&
                title.Equals("Program Manager", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            found.Add(new RunningApp(
                hwnd.ToString(),
                title,
                processName,
                (int)pid,
                IsZoomed(hwnd)));
            return true;
        }, IntPtr.Zero);

        return Task.FromResult<IReadOnlyList<RunningApp>>(found);
    }

    public Task<(bool Succeeded, string Message)> ActivateAsync(RunningApp app, CancellationToken cancellationToken = default)
    {
        if (!OperatingSystem.IsWindows() || !nint.TryParse(app.Id, out var hwnd))
        {
            return Task.FromResult((false, "Activate runs on Windows."));
        }

        ShowWindow(hwnd, 9);
        SetForegroundWindow(hwnd);
        return Task.FromResult((true, $"Focused {app.Title}."));
    }

    public Task<(bool Succeeded, string Message)> CloseAsync(RunningApp app, CancellationToken cancellationToken = default)
    {
        if (!OperatingSystem.IsWindows() || !nint.TryParse(app.Id, out var hwnd))
        {
            return Task.FromResult((false, "Close runs on Windows."));
        }

        PostMessage(hwnd, 0x0010, IntPtr.Zero, IntPtr.Zero);
        return Task.FromResult((true, $"Close sent to {app.Title}."));
    }

    private static string ReadTitle(nint hwnd)
    {
        var length = GetWindowTextLength(hwnd);
        var buffer = new StringBuilder(length + 1);
        GetWindowText(hwnd, buffer, buffer.Capacity);
        return buffer.ToString();
    }

    private delegate bool EnumWindowsProc(nint hWnd, nint lParam);

    [DllImport("user32.dll")]
    private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, nint lParam);

    [DllImport("user32.dll")]
    private static extern bool IsWindowVisible(nint hWnd);

    [DllImport("user32.dll")]
    private static extern int GetWindowTextLength(nint hWnd);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetWindowText(nint hWnd, StringBuilder lpString, int nMaxCount);

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(nint hWnd, out uint lpdwProcessId);

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(nint hWnd);

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(nint hWnd, int nCmdShow);

    [DllImport("user32.dll")]
    private static extern bool IsZoomed(nint hWnd);

    [DllImport("user32.dll")]
    private static extern bool PostMessage(nint hWnd, uint msg, nint wParam, nint lParam);
}
