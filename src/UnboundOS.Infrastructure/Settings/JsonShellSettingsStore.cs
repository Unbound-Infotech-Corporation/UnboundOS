using System.Text.Json;
using UnboundOS.Core.Abstractions;
using UnboundOS.Core.Models;

namespace UnboundOS.Infrastructure.Settings;

public sealed class JsonShellSettingsStore : IShellSettingsStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly string _path;

    public JsonShellSettingsStore(string? rootDirectory = null)
    {
        var root = rootDirectory ?? System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Unbound Infotech Corporation",
            "UnboundOS");
        Directory.CreateDirectory(root);
        _path = System.IO.Path.Combine(root, "settings.json");
    }

    public string FilePath => _path;

    public async Task<ShellSettings> LoadAsync(CancellationToken ct = default)
    {
        if (!File.Exists(_path))
        {
            var created = ShellSettings.CreateDefault();
            await SaveAsync(created, ct).ConfigureAwait(false);
            return created;
        }

        await using var stream = File.OpenRead(_path);
        var loaded = await JsonSerializer.DeserializeAsync<ShellSettings>(stream, JsonOptions, ct)
            .ConfigureAwait(false);
        return loaded ?? ShellSettings.CreateDefault();
    }

    public async Task SaveAsync(ShellSettings settings, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(settings);
        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(_path)!);
        await using var stream = File.Create(_path);
        await JsonSerializer.SerializeAsync(stream, settings, JsonOptions, ct).ConfigureAwait(false);
    }
}
