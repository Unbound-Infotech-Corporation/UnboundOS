namespace UnboundOS.Core.Models;

public sealed record GuideAction(
    string Id,
    string Title,
    string Hint,
    string Kind,
    string? Uri);

public sealed record OnScreenKey(
    string Id,
    string Label,
    string Value,
    int Row,
    int Column,
    bool IsAction);

public sealed record ControllerPointerDelta(
    int Dx,
    int Dy,
    bool PrimaryClick,
    bool SecondaryClick);
