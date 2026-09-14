using UnboundOS.Core;
using UnboundOS.Core.Abstractions;
using UnboundOS.Core.Models;
using UnboundOS.Infrastructure;
using UnboundOS.Infrastructure.Files;
using UnboundOS.Infrastructure.Hardware;
using UnboundOS.Infrastructure.Setup;
using UnboundOS.Infrastructure.Startup;
using UnboundOS.Infrastructure.Tools;
using UnboundOS.Infrastructure.Vendor;
using Microsoft.Extensions.DependencyInjection;

namespace UnboundOS.Tests;

public sealed class OsSpecSliceTests
{
    [Fact]
    public void ProductCopy_StaysHonestAboutExplorerAndOverclock()
    {
        Assert.Contains("does not replace Explorer", OsProductCopy.FilesHonesty, StringComparison.Ordinal);
        Assert.DoesNotContain("Shell=", OsProductCopy.FilesHonesty.Replace("set Shell=", string.Empty, StringComparison.Ordinal));
        Assert.Contains("does not replace Explorer or set Shell=", OsProductCopy.FilesHonesty, StringComparison.Ordinal);
        Assert.DoesNotContain("Night City", OsProductCopy.FilesHonesty, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("never writes GPU or CPU clocks", OsProductCopy.OverclockHonesty, StringComparison.Ordinal);
        Assert.Contains("Windows image owns the full OOBE wipe", OsProductCopy.CleanupHonesty, StringComparison.Ordinal);
        Assert.Contains("never silently disables", OsProductCopy.StartupHonesty, StringComparison.Ordinal);
        Assert.Contains("does not bundle HWiNFO", OsProductCopy.HardwareHonesty, StringComparison.Ordinal);
        Assert.Contains("registry, DriveInfo", OsProductCopy.HardwareHonesty, StringComparison.Ordinal);
        Assert.DoesNotContain("Night City", OsProductCopy.OverclockHonesty, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void StartupAuditCommand_RecognizesHeadlessFlag()
    {
        Assert.True(StartupAuditCommand.IsRequested(["UnboundOS.App.exe", "--audit-startup"]));
        Assert.True(StartupAuditCommand.IsRequested(["/audit-startup"]));
        Assert.False(StartupAuditCommand.IsRequested(["UnboundOS.App.exe"]));
        Assert.False(StartupAuditCommand.IsRequested(null));
    }

    [Fact]
    public void StartupPolicy_ProtectsExplorerVendorAndAnticheat()
    {
        var none = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        Assert.Equal(
            StartupDisposition.Protected,
            StartupPolicy.Classify(
                new StartupCandidate("explorer", StartupSource.RunKeyUser, "explorer", @"C:\Windows\explorer.exe", "HKCU"),
                none).Disposition);
        Assert.Equal(
            StartupDisposition.Protected,
            StartupPolicy.Classify(
                new StartupCandidate("nv", StartupSource.RunKeyMachine, "nvcontainer", @"C:\Windows\System32\nvcontainer.exe", "HKLM"),
                none).Disposition);
        Assert.Equal(
            StartupDisposition.Protected,
            StartupPolicy.Classify(
                new StartupCandidate("obs", StartupSource.StartupFolder, "obs64", @"C:\Program Files\obs-studio\bin\64bit\obs64.exe", "Startup"),
                none).Disposition);
        var overlay = StartupPolicy.Classify(
            new StartupCandidate("ow", StartupSource.RunKeyUser, "Overwolf", @"C:\Overwolf\Overwolf.exe", "HKCU"),
            none);
        Assert.Equal(StartupDisposition.Review, overlay.Disposition);
        Assert.True(overlay.CanApplyDisable);

        var machineBloat = StartupPolicy.Classify(
            new StartupCandidate("cc", StartupSource.RunKeyMachine, "CCleaner", @"C:\CCleaner\CCleaner.exe", "HKLM"),
            none);
        Assert.Equal(StartupDisposition.Review, machineBloat.Disposition);
        Assert.False(machineBloat.CanApplyDisable);

        var scheduled = StartupPolicy.Classify(
            new StartupCandidate("task", StartupSource.ScheduledTask, "Overwolf", @"C:\Windows\System32\Tasks\Overwolf", "TASK"),
            none);
        Assert.Equal(StartupDisposition.Review, scheduled.Disposition);
        Assert.False(scheduled.CanApplyDisable);
    }

    [Fact]
    public async Task HardwareInventory_DoesNotInventSensors()
    {
        var snapshot = await new OsHardwareInventory().SampleAsync();
        Assert.Empty(snapshot.Sensors);
        Assert.Empty(snapshot.MemoryModules);
        Assert.False(string.IsNullOrWhiteSpace(snapshot.CpuName));
        Assert.Contains("does not bundle HWiNFO", snapshot.SourceNote, StringComparison.Ordinal);
    }

    [Fact]
    public void FileBrowser_IncludeDrives_AddsReadyVolumes()
    {
        using var temp = new TempTree();
        var home = temp.Dir("home");
        var browser = new LocalFileBrowser(new FileBrowserSettings
        {
            Places = [new FilePlace("home", "Home", home)],
            IncludeDrives = true,
            ListDrives = DriveInfo.GetDrives
        });

        var page = browser.OpenPlaces();
        Assert.Contains(page.Places, place => place.Id == "home");
        Assert.Contains(page.Places, place => place.Id.StartsWith("drive-", StringComparison.Ordinal));
    }

    [Fact]
    public void VendorCatalog_FakePaths_MarksInstalledDisplayAndOc()
    {
        using var temp = new TempTree();
        var nvidia = temp.File("NVIDIA App.exe");
        var ryzen = temp.File("AMD Ryzen Master.exe");

        var catalog = new VendorAppCatalog(new VendorAppDiscoverySettings
        {
            UseDefaultWindowsLocations = false,
            ForcedExecutables = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
            {
                [VendorAppIds.NvidiaApp] = nvidia,
                [VendorAppIds.AmdRyzenMaster] = ryzen,
                [VendorAppIds.AmdAdrenalin] = null,
                [VendorAppIds.IntelArc] = null,
                [VendorAppIds.IntelXtu] = null,
                [VendorAppIds.NvidiaControlPanel] = null
            }
        });

        var apps = catalog.Discover();
        var displayNvidia = Assert.Single(apps, app => app.Id == VendorAppIds.NvidiaApp && app.Role == VendorAppRole.Display);
        Assert.True(displayNvidia.IsInstalled);
        Assert.Equal(nvidia, displayNvidia.ExecutablePath);
        Assert.Equal("OPEN", displayNvidia.StatusLabel);
        Assert.True(DesktopToolLauncher.IsSafeGetUri(displayNvidia.GetPath.Uri));

        var ocNvidia = Assert.Single(apps, app => app.Role == VendorAppRole.Overclock && app.Vendor == "NVIDIA");
        Assert.True(ocNvidia.IsInstalled);
        Assert.Equal(nvidia, ocNvidia.ExecutablePath);

        Assert.True(Assert.Single(apps, app => app.Id == VendorAppIds.AmdRyzenMaster).IsInstalled);
        Assert.False(Assert.Single(apps, app => app.Id == VendorAppIds.IntelXtu).IsInstalled);
        Assert.Equal("GET", Assert.Single(apps, app => app.Id == VendorAppIds.IntelArc).StatusLabel);
        Assert.Contains(apps, app => app.Role == VendorAppRole.Display);
        Assert.Contains(apps, app => app.Role == VendorAppRole.Overclock);
    }

    [Fact]
    public void VendorCatalog_NoDefaults_DoesNotInventInstalls()
    {
        var catalog = new VendorAppCatalog(new VendorAppDiscoverySettings { UseDefaultWindowsLocations = false });
        Assert.All(catalog.Discover(), app =>
        {
            Assert.False(app.IsInstalled);
            Assert.Equal("GET", app.StatusLabel);
            Assert.True(DesktopToolLauncher.IsSafeGetUri(app.GetPath.Uri));
        });
    }

    [Fact]
    public async Task VendorLauncher_Missing_AsksToGet_AndDoesNotWriteClocks()
    {
        var launcher = new VendorAppLauncher((_, _) => true, _ => true);
        var app = new VendorApp(
            VendorAppIds.IntelXtu,
            "Intel Extreme Tuning (XTU)",
            "Intel",
            VendorAppRole.Overclock,
            false,
            null,
            VendorAppCatalog.IntelXtuGet);

        var result = await launcher.LaunchAsync(app);
        Assert.False(result.Succeeded);
        Assert.Contains("Get", result.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("does not write GPU or CPU clocks", result.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task VendorLauncher_InstalledStub_StartsExecutable()
    {
        using var temp = new TempTree();
        var exe = temp.File("RadeonSoftware.exe");
        string? started = null;
        var launcher = new VendorAppLauncher((path, _) =>
        {
            started = path;
            return true;
        }, _ => true);

        var result = await launcher.LaunchAsync(new VendorApp(
            VendorAppIds.AmdAdrenalin,
            "AMD Adrenalin",
            "AMD",
            VendorAppRole.Display,
            true,
            exe,
            VendorAppCatalog.AmdAdrenalinGet));

        Assert.True(result.Succeeded);
        Assert.Equal(exe, started);
        Assert.Contains("did not change clocks", result.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task StartupAudit_ProtectsAnticheatAndPinsAllowlist()
    {
        using var temp = new TempTree();
        var eac = new StartupCandidate(
            StartupPolicy.MakeId(StartupSource.RunKeyUser, "EasyAntiCheat", @"C:\EAC\EasyAntiCheat.exe"),
            StartupSource.RunKeyUser,
            "EasyAntiCheat",
            @"C:\EAC\EasyAntiCheat.exe",
            "HKCU Run");
        var overwolf = new StartupCandidate(
            StartupPolicy.MakeId(StartupSource.RunKeyUser, "Overwolf", @"C:\Overwolf\Overwolf.exe"),
            StartupSource.RunKeyUser,
            "Overwolf",
            @"C:\Overwolf\Overwolf.exe",
            "HKCU Run");
        var vortex = new StartupCandidate(
            StartupPolicy.MakeId(StartupSource.StartupFolder, "Vortex", @"C:\Vortex\Vortex.exe"),
            StartupSource.StartupFolder,
            "Vortex",
            @"C:\Vortex\Vortex.exe",
            temp.Path);

        var store = new JsonStartupAllowlistStore(temp.Path);
        var mutator = new RecordingMutator();
        var audit = new StartupAuditService(new FakeInventory(eac, overwolf, vortex), store, mutator);

        var first = await audit.AuditAsync();
        Assert.Equal(StartupDisposition.Protected, first.Entries.Single(e => e.Name == "EasyAntiCheat").Disposition);
        Assert.Equal(StartupDisposition.Protected, first.Entries.Single(e => e.Name == "Vortex").Disposition);
        var review = first.Entries.Single(e => e.Name == "Overwolf");
        Assert.Equal(StartupDisposition.Review, review.Disposition);
        Assert.True(review.CanApplyDisable);

        await audit.PinAsync(overwolf.Id, pinned: true);
        var pinned = await audit.AuditAsync();
        Assert.Equal(StartupDisposition.Pinned, pinned.Entries.Single(e => e.Name == "Overwolf").Disposition);
        Assert.False(pinned.Entries.Single(e => e.Name == "Overwolf").CanApplyDisable);

        await audit.PinAsync(overwolf.Id, pinned: false);
        var applied = await audit.ApplyRecommendedAsync();
        Assert.Contains("Disabled", applied, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(overwolf.Id, mutator.DisabledIds);
        Assert.DoesNotContain(eac.Id, mutator.DisabledIds);
        Assert.DoesNotContain(vortex.Id, mutator.DisabledIds);
        Assert.True(File.Exists(store.FilePath));
    }

    [Fact]
    public async Task StartupMutator_RefusesProtectedEvenIfAsked()
    {
        var mutator = new WindowsStartupMutator();
        var entry = new StartupEntry(
            "x",
            StartupSource.RunKeyUser,
            "EasyAntiCheat",
            @"C:\EAC\EasyAntiCheat.exe",
            "HKCU",
            StartupDisposition.Review,
            "test",
            false);

        var message = await mutator.DisableAsync(entry);
        Assert.Contains("Skipped protected", message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void FileBrowser_FakePlaces_ListsHomeDesktopDownloadsAndDrives()
    {
        using var temp = new TempTree();
        var home = temp.Dir("home");
        var desktop = temp.Dir("desktop");
        var downloads = temp.Dir("downloads");
        File.WriteAllText(Path.Combine(home, "readme.txt"), "hi");
        Directory.CreateDirectory(Path.Combine(home, "Projects"));

        var browser = new LocalFileBrowser(new FileBrowserSettings
        {
            Places =
            [
                new FilePlace("home", "Home", home),
                new FilePlace("desktop", "Desktop", desktop),
                new FilePlace("downloads", "Downloads", downloads)
            ],
            IncludeDrives = false
        });

        var page = browser.OpenPlaces();
        Assert.Equal(home, page.CurrentPath);
        Assert.Contains(page.Places, place => place.Id == "home");
        Assert.Contains(page.Places, place => place.Id == "desktop");
        Assert.Contains(page.Places, place => place.Id == "downloads");
        Assert.Contains(page.Entries, entry => entry.Name == "Projects" && entry.IsDirectory);
        Assert.Contains(page.Entries, entry => entry.Name == "readme.txt" && !entry.IsDirectory);

        var nested = browser.OpenPath(Path.Combine(home, "Projects"));
        Assert.Equal(home, nested.ParentPath);
    }

    [Fact]
    public async Task SetupCleanup_RemovesKnownLeftoversAfterOnlineCheck()
    {
        using var temp = new TempTree();
        var root = temp.Dir("leftovers");
        Directory.CreateDirectory(Path.Combine(root, "UnboundOS-Setup"));
        Directory.CreateDirectory(Path.Combine(root, "OfflineNicDrivers"));
        File.WriteAllText(Path.Combine(root, "UnboundOS-setup.log"), "log");
        File.WriteAllText(Path.Combine(root, "online-ok.flag"), "ok");

        var cleanup = new SetupCleanupService(new SetupCleanupSettings { Root = root });
        var beforeOnline = await new SetupCleanupService(new SetupCleanupSettings
        {
            Root = temp.Dir("offline-root")
        }).CleanLeftoversAsync();
        Directory.CreateDirectory(Path.Combine(temp.Path, "offline-root", "OfflineNicDrivers"));
        var stillPacked = new SetupCleanupService(new SetupCleanupSettings
        {
            Root = Path.Combine(temp.Path, "offline-root")
        });
        var withoutFlag = await stillPacked.CleanLeftoversAsync();
        Assert.True(Directory.Exists(Path.Combine(temp.Path, "offline-root", "OfflineNicDrivers")));
        Assert.Contains("Windows image owns", withoutFlag.Message, StringComparison.Ordinal);

        var result = await cleanup.CleanLeftoversAsync();
        Assert.True(result.Succeeded);
        Assert.True(result.RemovedCount >= 3);
        Assert.False(Directory.Exists(Path.Combine(root, "UnboundOS-Setup")));
        Assert.False(Directory.Exists(Path.Combine(root, "OfflineNicDrivers")));
        Assert.Contains("Windows image owns", result.Message, StringComparison.Ordinal);
        _ = beforeOnline;
    }

    [Fact]
    public void AddUnboundOs_RegistersOsSliceServices()
    {
        var services = new ServiceCollection();
        services.AddUnboundOs();
        using var provider = services.BuildServiceProvider();

        Assert.IsType<StartupAuditService>(provider.GetRequiredService<IStartupAuditService>());
        Assert.IsType<VendorAppCatalog>(provider.GetRequiredService<IVendorAppCatalog>());
        Assert.IsType<LocalFileBrowser>(provider.GetRequiredService<IFileBrowser>());
        Assert.IsType<UnboundOS.Infrastructure.Hardware.OsHardwareInventory>(
            provider.GetRequiredService<IHardwareInventory>());
        Assert.IsType<SetupCleanupService>(provider.GetRequiredService<ISetupCleanup>());
    }

    private sealed class FakeInventory(params StartupCandidate[] items) : IStartupInventory
    {
        public IReadOnlyList<StartupCandidate> List() => items;
    }

    private sealed class RecordingMutator : IStartupMutator
    {
        public List<string> DisabledIds { get; } = [];

        public Task<string> DisableAsync(StartupEntry entry, CancellationToken ct = default)
        {
            DisabledIds.Add(entry.Id);
            return Task.FromResult($"Disabled {entry.Name}.");
        }
    }

    private sealed class TempTree : IDisposable
    {
        public string Path { get; } = System.IO.Path.Combine(
            System.IO.Path.GetTempPath(),
            "unboundos-os-" + Guid.NewGuid().ToString("N"));

        public TempTree() => Directory.CreateDirectory(Path);

        public string File(string name)
        {
            var full = System.IO.Path.Combine(Path, name);
            System.IO.File.WriteAllText(full, "stub");
            return full;
        }

        public string Dir(string name)
        {
            var full = System.IO.Path.Combine(Path, name);
            Directory.CreateDirectory(full);
            return full;
        }

        public void Dispose()
        {
            try { Directory.Delete(Path, true); } catch { /* best-effort */ }
        }
    }
}
