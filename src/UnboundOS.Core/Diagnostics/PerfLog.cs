namespace UnboundOS.Core.Diagnostics;

/// <summary>
/// Off-by-default timing log. Enable with <c>UNBOUNDOS_PERF=1</c>.
/// When off, <see cref="Measure"/> is a no-op (no allocations besides the
/// cached singleton). Never call this from a per-frame compositor path.
/// </summary>
public static class PerfLog
{
    private static readonly bool Enabled = ReadEnabled();
    private static readonly NoopDisposable Noop = new();
    private static readonly object Gate = new();
    private static string? _logPath;

    public static bool IsEnabled => Enabled;

    public static string FilePath => _logPath ??= Path.Combine(UnboundPaths.Root, "perf.log");

    public static IDisposable Measure(string name)
    {
        if (!Enabled)
        {
            return Noop;
        }

        return new Scope(name);
    }

    public static void Event(string name, long milliseconds)
    {
        if (!Enabled)
        {
            return;
        }

        Write($"{name} {milliseconds}ms");
    }

    private static bool ReadEnabled()
    {
        var value = Environment.GetEnvironmentVariable("UNBOUNDOS_PERF");
        return value is "1" or "true" or "TRUE" or "yes" or "on";
    }

    private static void Write(string line)
    {
        try
        {
            var stamped = $"{DateTime.UtcNow:O} {line}{Environment.NewLine}";
            lock (Gate)
            {
                File.AppendAllText(FilePath, stamped);
            }
        }
        catch (IOException)
        {
            // Logging must never break the shell.
        }
        catch (UnauthorizedAccessException)
        {
            // ignored
        }
    }

    private sealed class Scope : IDisposable
    {
        private readonly string _name;
        private readonly long _start = Environment.TickCount64;
        private bool _done;

        public Scope(string name) => _name = name;

        public void Dispose()
        {
            if (_done)
            {
                return;
            }

            _done = true;
            Event(_name, Environment.TickCount64 - _start);
        }
    }

    private sealed class NoopDisposable : IDisposable
    {
        public void Dispose()
        {
        }
    }
}
