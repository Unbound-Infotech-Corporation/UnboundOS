using UnboundOS.Core.Abstractions;
using UnboundOS.Core.Models;

namespace UnboundOS.Infrastructure.Library;

/// <summary>
/// Opens a library game through its store URI or executable. Does not rewrite SessionEngine.
/// </summary>
public sealed class LibraryLaunchService : ILibraryLaunchService
{
    private readonly Func<string, bool> _openUri;
    private readonly Func<string, string, bool> _start;

    public LibraryLaunchService()
        : this(DefaultOpenUri, DefaultStart)
    {
    }

    public LibraryLaunchService(Func<string, bool> openUri, Func<string, string, bool> start)
    {
        _openUri = openUri;
        _start = start;
    }

    public Task<SessionMutationResult> LaunchAsync(
        LibraryGame game,
        SessionProfile profile,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(game);
        _ = profile;

        if (!string.IsNullOrWhiteSpace(game.LaunchUri) && IsSafeLaunchUri(game.LaunchUri))
        {
            try
            {
                if (!_openUri(game.LaunchUri))
                {
                    return Task.FromResult(Fail($"Could not open {game.DisplayName}."));
                }
            }
            catch (Exception error)
            {
                return Task.FromResult(Fail($"{game.DisplayName} did not start: {error.Message}"));
            }

            return Task.FromResult(Ok($"Launched {game.DisplayName} through the {game.Store} library."));
        }

        if (!string.IsNullOrWhiteSpace(game.ExecutablePath) && File.Exists(game.ExecutablePath))
        {
            try
            {
                if (!_start(game.ExecutablePath, string.Empty))
                {
                    return Task.FromResult(Fail($"{game.DisplayName} did not start."));
                }
            }
            catch (Exception error)
            {
                return Task.FromResult(Fail($"{game.DisplayName} did not start: {error.Message}"));
            }

            return Task.FromResult(Ok($"Opened {game.DisplayName}."));
        }

        return Task.FromResult(Fail(
            $"{game.DisplayName} has no launch path. Use Session for the posture engine."));
    }

    public Task CancelWatchAsync() => Task.CompletedTask;

    public static bool IsSafeLaunchUri(string uri)
    {
        if (!Uri.TryCreate(uri, UriKind.Absolute, out var parsed))
        {
            return false;
        }

        return parsed.Scheme.Equals("steam", StringComparison.OrdinalIgnoreCase) ||
               parsed.Scheme.Equals("com.epicgames.launcher", StringComparison.OrdinalIgnoreCase) ||
               parsed.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase);
    }

    private static SessionMutationResult Ok(string message) =>
        new() { Succeeded = true, Message = message, Actions = [message] };

    private static SessionMutationResult Fail(string message) =>
        new() { Succeeded = false, Message = message, Actions = [message] };

    private static bool DefaultOpenUri(string uri)
    {
        var info = new System.Diagnostics.ProcessStartInfo(uri) { UseShellExecute = true };
        return System.Diagnostics.Process.Start(info) is not null;
    }

    private static bool DefaultStart(string executable, string arguments)
    {
        var info = new System.Diagnostics.ProcessStartInfo(executable)
        {
            Arguments = arguments,
            UseShellExecute = true
        };
        return System.Diagnostics.Process.Start(info) is not null;
    }
}
