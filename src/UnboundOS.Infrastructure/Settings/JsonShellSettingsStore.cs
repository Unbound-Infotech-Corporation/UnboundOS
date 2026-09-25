using System.Text.Json;
using System.Text.Json.Serialization;
using UnboundOS.Core.Abstractions;
using UnboundOS.Core.Models;

namespace UnboundOS.Infrastructure.Settings;

public sealed class JsonShellSettingsStore : IShellSettingsStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly string _path;
    private readonly SemaphoreSlim _gate = new(1, 1);

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
        await _gate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            if (!File.Exists(_path))
            {
                var created = ShellSettings.CreateDefault();
                await WriteUnlockedAsync(created, ct).ConfigureAwait(false);
                return created;
            }

            await using var stream = File.OpenRead(_path);
            var loaded = await JsonSerializer.DeserializeAsync<ShellSettings>(stream, JsonOptions, ct)
                .ConfigureAwait(false);
            return loaded ?? ShellSettings.CreateDefault();
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task SaveAsync(ShellSettings settings, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(settings);
        await _gate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            await WriteUnlockedAsync(settings, ct).ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task WriteUnlockedAsync(ShellSettings settings, CancellationToken ct)
    {
        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(_path)!);
        await using var stream = File.Create(_path);
        await JsonSerializer.SerializeAsync(stream, settings, JsonOptions, ct).ConfigureAwait(false);
    }
}
