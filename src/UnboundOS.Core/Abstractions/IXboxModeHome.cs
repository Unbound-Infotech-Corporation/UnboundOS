using UnboundOS.Core.Models;

namespace UnboundOS.Core.Abstractions;

public interface IXboxModeHome
{
    bool IsWanted { get; }

    XboxModeHomeSnapshot Probe();

    Task<(bool Succeeded, string Message)> SetWantedAsync(bool wanted, CancellationToken cancellationToken = default);

    Task<(bool Succeeded, string Message)> ConfirmSelectedAsync(CancellationToken cancellationToken = default);

    Task<(bool Succeeded, string Message)> OpenXboxModeSettingsAsync(CancellationToken cancellationToken = default);
}

public interface ILastSessionResume
{
    Task<LastSessionRecord?> LoadAsync(CancellationToken cancellationToken = default);

    Task SaveAsync(LastSessionRecord record, CancellationToken cancellationToken = default);

    Task ClearAsync(CancellationToken cancellationToken = default);
}

public interface IGuideMenu
{
    IReadOnlyList<GuideAction> Actions { get; }

    Task<(bool Succeeded, string Message)> InvokeAsync(string id, CancellationToken cancellationToken = default);
}

public interface IGameProfileStore
{
    Task<GameProfile> LoadAsync(string gameId, CancellationToken cancellationToken = default);

    Task SaveAsync(GameProfile profile, CancellationToken cancellationToken = default);
}

public interface ICustomLibraryStore
{
    Task<IReadOnlyList<CustomLibraryEntry>> ListAsync(CancellationToken cancellationToken = default);

    Task AddAsync(CustomLibraryEntry entry, CancellationToken cancellationToken = default);
}

public interface IDiagnosticsExport
{
    Task<(bool Succeeded, string Message, string Path)> ExportAsync(CancellationToken cancellationToken = default);
}
