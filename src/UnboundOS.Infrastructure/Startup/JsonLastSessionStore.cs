using System.Text.Json;
using UnboundOS.Core;
using UnboundOS.Core.Abstractions;
using UnboundOS.Core.Models;

namespace UnboundOS.Infrastructure.Startup;

public sealed class JsonLastSessionStore : ILastSessionResume
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private readonly string _path;

    public JsonLastSessionStore(string? path = null)
    {
        _path = path ?? Path.Combine(UnboundPaths.Root, "last-session.json");
    }

    public Task<LastSessionRecord?> LoadAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!File.Exists(_path))
        {
            return Task.FromResult<LastSessionRecord?>(null);
        }

        try
        {
            var record = JsonSerializer.Deserialize<LastSessionRecord>(File.ReadAllText(_path), JsonOptions);
            return Task.FromResult(record);
        }
        catch (Exception)
        {
            return Task.FromResult<LastSessionRecord?>(null);
        }
    }

    public Task SaveAsync(LastSessionRecord record, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var dir = Path.GetDirectoryName(_path);
        if (!string.IsNullOrWhiteSpace(dir))
        {
            Directory.CreateDirectory(dir);
        }

        File.WriteAllText(_path, JsonSerializer.Serialize(record, JsonOptions));
        return Task.CompletedTask;
    }

    public Task ClearAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (File.Exists(_path))
        {
            File.Delete(_path);
        }

        return Task.CompletedTask;
    }
}
