using UnboundOS.Core.Abstractions;
using UnboundOS.Core.Home;

namespace UnboundOS.Infrastructure.Home;

public sealed class HomeWidgetCatalog : IHomeWidgetCatalog
{
    public HomeWidgetCatalog(IEnumerable<IHomeWidgetSource>? extras = null)
    {
        var extra = extras?.SelectMany(source => source.Widgets) ?? [];
        Widgets = [.. HomeWidgets.BuiltIn, .. extra];
    }

    public IReadOnlyList<HomeWidgetDefinition> Widgets { get; }
}
