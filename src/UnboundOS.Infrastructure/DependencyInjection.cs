using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using UnboundOS.Core.Abstractions;
using UnboundOS.Core.Overlay;
using UnboundOS.Infrastructure.Network;
using UnboundOS.Infrastructure.Mods;
using UnboundOS.Infrastructure.Overlay;
using UnboundOS.Infrastructure.Power;
using UnboundOS.Infrastructure.Process;
using UnboundOS.Infrastructure.Profiles;
using UnboundOS.Infrastructure.Session;
using UnboundOS.Infrastructure.Settings;
using UnboundOS.Infrastructure.Files;
using UnboundOS.Infrastructure.Hardware;
using UnboundOS.Infrastructure.Setup;
using UnboundOS.Infrastructure.Startup;
using UnboundOS.Infrastructure.Stream;
using UnboundOS.Infrastructure.Telemetry;
using UnboundOS.Infrastructure.Tools;
using UnboundOS.Infrastructure.Vendor;

namespace UnboundOS.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddUnboundOs(this IServiceCollection services)
    {
        services.AddSingleton<IProcessGuardian, ProcessGuardian>();
        services.AddSingleton<INetworkDirector, NetworkDirector>();
        services.AddSingleton<IPowerPlanService, WindowsPowerPlanService>();
        services.AddSingleton<IStreamCanvas, StreamCanvasService>();
        services.AddSingleton<DesktopToolDiscoverySettings>();
        services.AddSingleton<IDesktopToolCatalog, DesktopToolCatalog>();
        services.AddSingleton<IDesktopToolLauncher, DesktopToolLauncher>();
        services.AddSingleton<IProfileStore, JsonProfileStore>();
        services.TryAddSingleton<ISystemAnimationPreference, AlwaysOnSystemAnimationPreference>();
        services.AddSingleton<IShellSettingsStore, JsonShellSettingsStore>();
        services.AddSingleton<IUiMotionPolicy, UiMotionPolicy>();
        services.AddSingleton<IStartupInventory, WindowsStartupInventory>();
        services.AddSingleton<IStartupAllowlistStore, JsonStartupAllowlistStore>();
        services.AddSingleton<IStartupMutator, WindowsStartupMutator>();
        services.AddSingleton<IStartupAuditService, StartupAuditService>();
        services.AddSingleton<IVendorAppCatalog, VendorAppCatalog>();
        services.AddSingleton<IVendorAppLauncher, VendorAppLauncher>();
        services.AddSingleton<IFileBrowser, LocalFileBrowser>();
        services.AddSingleton<IHardwareInventory, OsHardwareInventory>();
        services.AddSingleton<ISetupCleanup, SetupCleanupService>();
        services.AddSingleton<ITelemetryService, WindowsTelemetryService>();
        services.AddSingleton<ISessionEngine, SessionEngine>();
        services.AddSingleton<SteamWorkshopCatalogService>();
        services.AddSingleton<VortexDiscoverySettings>();
        services.AddSingleton<VortexCatalogService>();
        services.AddSingleton<VortexLauncher>();
        services.AddSingleton<IModCatalogService, UnifiedModCatalogService>();
        services.AddSingleton<IModProfileStore, JsonModProfileStore>();
        services.AddSingleton<IModBackupService, FileModBackupService>();
        services.AddSingleton<IGameModAdapter, SteamWorkshopReadOnlyAdapter>();
        services.AddSingleton<IGameModAdapter, VortexReadOnlyAdapter>();
        services.AddSingleton<IModProfileManager, ModProfileManager>();
        services.AddSingleton<SteamExternalModHandoff>();
        services.AddSingleton<IExternalModHandoff, CompositeExternalModHandoff>();
        // Overlay is optional and off. Register IDesktopOverlayHost before AddUnboundOs() to replace this.
        services.TryAddSingleton<IDesktopOverlayHost, DisabledDesktopOverlayHost>();
        return services;
    }
}
