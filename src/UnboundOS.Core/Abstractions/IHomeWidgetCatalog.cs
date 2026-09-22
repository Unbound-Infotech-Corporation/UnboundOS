using UnboundOS.Core.Home;

namespace UnboundOS.Core.Abstractions;

/// <summary>
/// Extra Home widgets from an addon. Register before
/// <c>AddUnboundOs()</c> via <c>TryAddEnumerable</c>. First-party
/// clock/temps always ship; this is the later custom-widget hook.
/// </summary>
public interface IHomeWidgetSource
{
    IReadOnlyList<HomeWidgetDefinition> Widgets { get; }
}

/// <summary>Built-in Home widgets plus any <see cref="IHomeWidgetSource"/> extras.</summary>
public interface IHomeWidgetCatalog
{
    IReadOnlyList<HomeWidgetDefinition> Widgets { get; }
}
