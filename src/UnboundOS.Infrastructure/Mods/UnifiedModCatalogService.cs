using UnboundOS.Core.Abstractions;
using UnboundOS.Core.Models;

namespace UnboundOS.Infrastructure.Mods;

public sealed class UnifiedModCatalogService(SteamWorkshopCatalogService steam) : IModCatalogService
{
    public async Task<IReadOnlyList<ModGame>> DiscoverAsync(CancellationToken cancellationToken = default)
    {
        var discovered = await steam.DiscoverAsync(cancellationToken).ConfigureAwait(false);
        return discovered.Count > 0 ? discovered : CreateDemoCatalog();
    }

    private static IReadOnlyList<ModGame> CreateDemoCatalog()
    {
        var capabilities = new ModCapabilities(
            true,
            false,
            false,
            true,
            false,
            false,
            ModManagementLevel.DiscoveryOnly,
            "Demo catalog: install a Steam Workshop game to replace this preview. No files will be changed.");

        return
        [
            new ModGame(
                "unbound-demo",
                "No Workshop Games Found — Preview",
                null,
                ModProvider.BuiltIn,
                capabilities,
                [
                    new InstalledMod(
                        "demo-foundation",
                        "unbound-demo",
                        "Foundation Framework",
                        ModProvider.BuiltIn,
                        string.Empty,
                        true,
                        0,
                        [],
                        [],
                        ModUpdateState.Current,
                        "1.4.0"),
                    new InstalledMod(
                        "demo-visuals",
                        "unbound-demo",
                        "Cinematic Visuals",
                        ModProvider.BuiltIn,
                        string.Empty,
                        true,
                        1,
                        ["demo-foundation"],
                        [],
                        ModUpdateState.UpdateAvailable,
                        "2.1.3"),
                    new InstalledMod(
                        "demo-gameplay",
                        "unbound-demo",
                        "Unbound Gameplay",
                        ModProvider.BuiltIn,
                        string.Empty,
                        false,
                        2,
                        ["demo-foundation"],
                        ["demo-legacy-rules"],
                        ModUpdateState.Current,
                        "0.9.0")
                ],
                "demo-readonly")
        ];
    }
}
