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
using UnboundOS.Infrastructure.Stream;
using UnboundOS.Infrastructure.Telemetry;

namespace UnboundOS.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddUnboundOs(this IServiceCollection services)
    {
        services.AddSingleton<IProcessGuardian, ProcessGuardian>();
        services.AddSingleton<INetworkDirector, NetworkDirector>();
        services.AddSingleton<IPowerPlanService, WindowsPowerPlanService>();
        services.AddSingleton<IStreamCanvas, StreamCanvasService>();
        services.AddSingleton<IProfileStore, JsonProfileStore>();
        services.AddSingleton<ITelemetryService, WindowsTelemetryService>();
        services.AddSingleton<ISessionEngine, SessionEngine>();
        services.AddSingleton<SteamWorkshopCatalogService>();
        services.AddSingleton<IModCatalogService, UnifiedModCatalogService>();
        services.AddSingleton<IModProfileStore, JsonModProfileStore>();
        services.AddSingleton<IModBackupService, FileModBackupService>();
        services.AddSingleton<IGameModAdapter, SteamWorkshopReadOnlyAdapter>();
        services.AddSingleton<IModProfileManager, ModProfileManager>();
        services.AddSingleton<IExternalModHandoff, SteamExternalModHandoff>();
        // Overlay is optional and off. Register IDesktopOverlayHost before AddUnboundOs() to replace this.
        services.TryAddSingleton<IDesktopOverlayHost, DisabledDesktopOverlayHost>();
        return services;
    }
}
