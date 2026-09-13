using System.Text.Json;
using System.Text.Json.Serialization;
using UnboundOS.Core.Abstractions;
using UnboundOS.Core.Models;

namespace UnboundOS.Infrastructure.Mods;

public sealed class JsonModProfileStore : IModProfileStore
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly string _path;

    public JsonModProfileStore()
    {
        var root = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Unbound Infotech Corporation",
            "UnboundOS");
        Directory.CreateDirectory(root);
        _path = Path.Combine(root, "mod-profiles.json");
    }

    public async Task<IReadOnlyList<ModProfile>> LoadAsync(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(_path))
        {
            return [];
        }

        await using var stream = File.OpenRead(_path);
        return await JsonSerializer.DeserializeAsync<List<ModProfile>>(stream, Options, cancellationToken)
            .ConfigureAwait(false) ?? [];
    }

    public async Task SaveAsync(
        IReadOnlyList<ModProfile> profiles,
        CancellationToken cancellationToken = default)
    {
        var temporaryPath = $"{_path}.{Guid.NewGuid():N}.tmp";
        await using (var stream = File.Create(temporaryPath))
        {
            await JsonSerializer.SerializeAsync(stream, profiles, Options, cancellationToken)
                .ConfigureAwait(false);
        }

        File.Move(temporaryPath, _path, overwrite: true);
    }
}
