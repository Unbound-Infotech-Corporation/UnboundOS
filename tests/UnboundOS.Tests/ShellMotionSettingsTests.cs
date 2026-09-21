using UnboundOS.Core.Abstractions;
using UnboundOS.Core.Models;
using UnboundOS.Infrastructure.Settings;

namespace UnboundOS.Tests;

public sealed class ShellMotionSettingsTests
{
    [Fact]
    public async Task SettingsStore_DefaultsMotionOnAndPersistsOff()
    {
        using var temp = new TempFolder();
        var store = new JsonShellSettingsStore(temp.Path);

        var first = await store.LoadAsync();
        Assert.True(first.InterfaceMotionEnabled);
        Assert.True(File.Exists(store.FilePath));
        var json = await File.ReadAllTextAsync(store.FilePath);
        Assert.Contains("interfaceMotionEnabled", json, StringComparison.Ordinal);

        await store.SaveAsync(first with { InterfaceMotionEnabled = false });
        var loaded = await store.LoadAsync();
        Assert.False(loaded.InterfaceMotionEnabled);
    }

    [Fact]
    public async Task SettingsStore_HomeHudDefaultsOnWhenKeysMissing_AndPersistsOff()
    {
        using var temp = new TempFolder();
        var store = new JsonShellSettingsStore(temp.Path);
        await File.WriteAllTextAsync(store.FilePath, """{ "interfaceMotionEnabled": true }""");

        var loaded = await store.LoadAsync();
        Assert.True(loaded.ShowHomeHud);
        Assert.True(loaded.ShowHomeClock);
        Assert.True(loaded.ShowHomeTemps);
        Assert.DoesNotContain("homeHudEnabled", await File.ReadAllTextAsync(store.FilePath), StringComparison.Ordinal);

        var hud = new HomeHudSettings(store);
        await hud.InitializeAsync();
        Assert.True(hud.HudEnabled);
        await hud.SetHudEnabledAsync(false);
        await hud.SetClockEnabledAsync(false);
        await hud.SetTempsEnabledAsync(true);

        var saved = await store.LoadAsync();
        Assert.False(saved.ShowHomeHud);
        Assert.False(saved.ShowHomeClock);
        Assert.True(saved.ShowHomeTemps);
        var json = await File.ReadAllTextAsync(store.FilePath);
        Assert.Contains("homeHudEnabled", json, StringComparison.Ordinal);
        Assert.Contains("homeClockEnabled", json, StringComparison.Ordinal);
        Assert.True(json.Contains("true", StringComparison.Ordinal));
    }

    [Fact]
    public async Task MotionToggle_DoesNotClobberHomeHud()
    {
        using var temp = new TempFolder();
        var store = new JsonShellSettingsStore(temp.Path);
        var hud = new HomeHudSettings(store);
        await hud.InitializeAsync();
        await hud.SetClockEnabledAsync(false);

        var policy = new UiMotionPolicy(
            store,
            new DelegateSystemAnimationPreference(() => true),
            new StubSession { State = SessionState.Idle });
        await policy.InitializeAsync();
        await policy.SetUserWantsMotionAsync(false);

        var loaded = await store.LoadAsync();
        Assert.False(loaded.InterfaceMotionEnabled);
        Assert.False(loaded.ShowHomeClock);
        Assert.True(loaded.ShowHomeHud);
        Assert.True(loaded.ShowHomeTemps);
    }

    [Fact]
    public async Task MotionPolicy_DefaultAllowsMotion()
    {
        using var temp = new TempFolder();
        var policy = CreatePolicy(temp.Path, systemOn: true, SessionState.Idle);
        await policy.InitializeAsync();

        Assert.True(policy.UserWantsMotion);
        Assert.True(policy.AllowMotion);
        Assert.Equal(MotionSuppression.None, policy.Suppression);
        Assert.Contains("hairline", policy.StatusText, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("galaxy", policy.StatusText, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("sheen", policy.StatusText, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("glow", policy.StatusText, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task MotionPolicy_HonorsInAppToggle()
    {
        using var temp = new TempFolder();
        var policy = CreatePolicy(temp.Path, systemOn: true, SessionState.Idle);
        await policy.InitializeAsync();
        await policy.SetUserWantsMotionAsync(false);

        Assert.False(policy.UserWantsMotion);
        Assert.False(policy.AllowMotion);
        Assert.Equal(MotionSuppression.UserDisabled, policy.Suppression);
        Assert.Contains("off", policy.StatusText, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task MotionPolicy_HonorsWindowsAnimationOff()
    {
        using var temp = new TempFolder();
        var policy = CreatePolicy(temp.Path, systemOn: false, SessionState.Idle);
        await policy.InitializeAsync();

        Assert.True(policy.UserWantsMotion);
        Assert.False(policy.AllowMotion);
        Assert.Equal(MotionSuppression.SystemDisabled, policy.Suppression);
        Assert.Contains("Windows", policy.StatusText, StringComparison.Ordinal);
    }

    [Fact]
    public async Task MotionPolicy_PausesDuringLiveSessionWithoutHidingToggle()
    {
        using var temp = new TempFolder();
        var session = new StubSession { State = SessionState.Active };
        var policy = CreatePolicy(temp.Path, systemOn: true, session);
        await policy.InitializeAsync();

        Assert.True(policy.UserWantsMotion);
        Assert.True(policy.IsSessionLive);
        Assert.False(policy.AllowMotion);
        Assert.Equal(MotionSuppression.SessionLive, policy.Suppression);
        Assert.Contains("toggle", policy.StatusText, StringComparison.OrdinalIgnoreCase);

        session.Set(SessionState.Idle);
        Assert.True(policy.AllowMotion);
        Assert.Equal(MotionSuppression.None, policy.Suppression);
    }

    [Fact]
    public async Task MotionPolicy_UserOffBeatsSessionAndSystem()
    {
        using var temp = new TempFolder();
        var policy = CreatePolicy(temp.Path, systemOn: false, SessionState.Active);
        await policy.InitializeAsync();
        await policy.SetUserWantsMotionAsync(false);

        Assert.Equal(MotionSuppression.UserDisabled, policy.Suppression);
        Assert.False(policy.AllowMotion);
    }

    private static UiMotionPolicy CreatePolicy(string root, bool systemOn, SessionState state) =>
        CreatePolicy(root, systemOn, new StubSession { State = state });

    private static UiMotionPolicy CreatePolicy(string root, bool systemOn, StubSession session) =>
        new(
            new JsonShellSettingsStore(root),
            new DelegateSystemAnimationPreference(() => systemOn),
            session);

    private sealed class StubSession : ISessionEngine
    {
        public SessionState State { get; set; } = SessionState.Idle;

        public SessionProfile? ActiveProfile => null;

        public SessionSnapshot? LastSnapshot => null;

        public event EventHandler<SessionState>? StateChanged;

        public void Set(SessionState state)
        {
            State = state;
            StateChanged?.Invoke(this, state);
        }

        public Task<SessionMutationResult> EnterAsync(SessionProfile profile, CancellationToken ct = default) =>
            Task.FromResult(new SessionMutationResult { Succeeded = false, Message = "stub" });

        public Task<SessionMutationResult> ExitAsync(CancellationToken ct = default) =>
            Task.FromResult(new SessionMutationResult { Succeeded = false, Message = "stub" });
    }

    private sealed class TempFolder : IDisposable
    {
        public string Path { get; } = System.IO.Path.Combine(
            System.IO.Path.GetTempPath(),
            "UnboundOS-motion-" + Guid.NewGuid().ToString("N"));

        public TempFolder() => Directory.CreateDirectory(Path);

        public void Dispose()
        {
            try
            {
                Directory.Delete(Path, recursive: true);
            }
            catch (IOException)
            {
                // best effort
            }
        }
    }
}
