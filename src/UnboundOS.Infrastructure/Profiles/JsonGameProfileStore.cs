using System.Text.Json;
using UnboundOS.Core;
using UnboundOS.Core.Abstractions;
using UnboundOS.Core.Models;

namespace UnboundOS.Infrastructure.Profiles;

public sealed class JsonGameProfileStore : IGameProfileStore
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private readonly string _path;

    public JsonGameProfileStore(string? path = null)
    {
        _path = path ?? Path.Combine(UnboundPaths.Root, "game-profiles.json");
    }

    public async Task<GameProfile> LoadAsync(string gameId, CancellationToken cancellationToken = default)
    {
        var all = await ReadAllAsync(cancellationToken).ConfigureAwait(false);
        return all.TryGetValue(gameId, out var profile)
            ? profile
            : new GameProfile(gameId, false, 0, false, "");
    }

    public async Task SaveAsync(GameProfile profile, CancellationToken cancellationToken = default)
    {
        var all = await ReadAllAsync(cancellationToken).ConfigureAwait(false);
        all[profile.GameId] = profile;
        var dir = Path.GetDirectoryName(_path);
        if (!string.IsNullOrWhiteSpace(dir))
        {
            Directory.CreateDirectory(dir);
        }

        File.WriteAllText(_path, JsonSerializer.Serialize(all, JsonOptions));
    }

    private Task<Dictionary<string, GameProfile>> ReadAllAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!File.Exists(_path))
        {
            return Task.FromResult(new Dictionary<string, GameProfile>(StringComparer.OrdinalIgnoreCase));
        }

        try
        {
            var loaded = JsonSerializer.Deserialize<Dictionary<string, GameProfile>>(File.ReadAllText(_path), JsonOptions);
            return Task.FromResult(loaded ?? new Dictionary<string, GameProfile>(StringComparer.OrdinalIgnoreCase));
        }
        catch (Exception)
        {
            return Task.FromResult(new Dictionary<string, GameProfile>(StringComparer.OrdinalIgnoreCase));
        }
    }
}
