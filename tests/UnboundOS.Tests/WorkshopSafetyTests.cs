using UnboundOS.Core.Abstractions;
using UnboundOS.Core.Models;
using UnboundOS.Infrastructure.Mods;

namespace UnboundOS.Tests;

public sealed class ModProfileManagerTests
{
    [Fact]
    public async Task SteamWorkshopWithoutGameAdapter_IsExplicitlyReadOnly()
    {
        var backup = new RecordingBackupService();
        var manager = new ModProfileManager([new SteamWorkshopReadOnlyAdapter()], backup);
        var game = CreateGame(
            ModProvider.SteamWorkshop,
            ModCapabilities.SteamDiscoveryOnly,
            "steam-workshop-readonly");
        var profile = CreateProfile(game.GameId);

        var result = await manager.ApplyAsync(game, profile);

        Assert.False(result.Succeeded);
        Assert.Contains("discovered locally", result.Message);
        Assert.False(backup.Created);
    }

    [Fact]
    public async Task TransactionalAdapter_BacksUpBeforeWriting()
    {
        var backup = new RecordingBackupService();
        var adapter = new RecordingAdapter(backup, shouldFail: false);
        var manager = new ModProfileManager([adapter], backup);
        var game = CreateGame(ModProvider.Local, adapter.GetCapabilities(null!), adapter.Id);

        var result = await manager.ApplyAsync(game, CreateProfile(game.GameId));

        Assert.True(result.Succeeded);
        Assert.True(backup.Created);
        Assert.True(adapter.Applied);
        Assert.False(backup.Restored);
    }

    [Fact]
    public async Task TransactionalAdapter_RollsBackWhenWriteFails()
    {
        var backup = new RecordingBackupService();
        var adapter = new RecordingAdapter(backup, shouldFail: true);
        var manager = new ModProfileManager([adapter], backup);
        var game = CreateGame(ModProvider.Local, adapter.GetCapabilities(null!), adapter.Id);

        var result = await manager.ApplyAsync(game, CreateProfile(game.GameId));

        Assert.False(result.Succeeded);
        Assert.True(backup.Created);
        Assert.True(backup.Restored);
        Assert.Contains("rolled back", result.Message);
    }

    private static ModGame CreateGame(
        ModProvider provider,
        ModCapabilities capabilities,
        string adapterId) =>
        new("game", "Test Game", @"C:\Games\Test", provider, capabilities, [], adapterId);

    private static ModProfile CreateProfile(string gameId) =>
        new("profile", gameId, "Test Profile", [], DateTimeOffset.UtcNow);

    private sealed class RecordingAdapter(IModBackupService backups, bool shouldFail)
        : TransactionalFileModAdapter(backups)
    {
        public override string Id => "recording";
        public bool Applied { get; private set; }

        public override bool CanHandle(ModGame game) => game.AdapterId == Id;

        public override ModCapabilities GetCapabilities(ModGame game) =>
            new(true, true, true, false, false, false, ModManagementLevel.LoadOrder, "Test adapter");

        protected override IReadOnlyList<string> GetConfigurationPaths(ModGame game) =>
            [@"C:\Games\Test\mods.json"];

        protected override Task ApplyCoreAsync(
            ModGame game,
            ModProfile profile,
            CancellationToken cancellationToken)
        {
            Applied = true;
            return shouldFail
                ? Task.FromException(new IOException("Simulated write failure"))
                : Task.CompletedTask;
        }
    }

    private sealed class RecordingBackupService : IModBackupService
    {
        private readonly ModConfigurationBackup _backup =
            new("backup", "game", "recording", @"C:\Backup", DateTimeOffset.UtcNow, "test");

        public bool Created { get; private set; }
        public bool Restored { get; private set; }

        public Task<ModConfigurationBackup> CreateAsync(
            string gameId,
            string adapterId,
            IReadOnlyList<string> configurationPaths,
            string reason,
            CancellationToken cancellationToken = default)
        {
            Created = true;
            return Task.FromResult(_backup);
        }

        public Task<ModConfigurationBackup?> GetLatestAsync(
            string gameId,
            string adapterId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<ModConfigurationBackup?>(_backup);

        public Task RestoreAsync(
            ModConfigurationBackup backup,
            CancellationToken cancellationToken = default)
        {
            Restored = true;
            return Task.CompletedTask;
        }
    }
}

public sealed class ValveKeyValuesParserTests
{
    [Fact]
    public void Parse_HandlesNestedSteamLibraryFormatAndEscapes()
    {
        const string vdf =
            """
            "libraryfolders"
            {
                "0"
                {
                    "path" "C:\\Program Files (x86)\\Steam"
                    "apps"
                    {
                        "123" "1"
                    }
                }
                // Steam comments are ignored
                "1" { "path" "D:\\Games\\SteamLibrary" }
            }
            """;

        var root = ValveKeyValuesParser.Parse(vdf);
        var libraries = root.Child("libraryfolders");

        Assert.NotNull(libraries);
        Assert.Equal(@"C:\Program Files (x86)\Steam", libraries.Child("0")?.Value("path"));
        Assert.Equal("1", libraries.Child("0")?.Child("apps")?.Value("123"));
        Assert.Equal(@"D:\Games\SteamLibrary", libraries.Child("1")?.Value("path"));
    }
}