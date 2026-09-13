using UnboundOS.Core.Models;

namespace UnboundOS.Core.Abstractions;

public interface IGameLibraryCatalog
{
    Task<IReadOnlyList<LibraryGame>> DiscoverAsync(CancellationToken cancellationToken = default);
}

public interface ILibraryLaunchService
{
    Task<SessionMutationResult> LaunchAsync(
        LibraryGame game,
        SessionProfile profile,
        CancellationToken cancellationToken = default);

    Task CancelWatchAsync();
}

public interface IEntitlementService
{
    EntitlementSnapshot Current { get; }
    event EventHandler? Changed;
    Task LoadAsync(CancellationToken cancellationToken = default);
    Task<ActivationOutcome> ActivateAsync(string licenseKey, CancellationToken cancellationToken = default);
}

public interface ISessionTuningService
{
    Task<(bool Succeeded, string Message, TuningSnapshot Snapshot, IReadOnlyList<string> Actions)> ApplyAsync(
        SessionProfile profile,
        SessionEnterContext context,
        string? gameAdapterId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<string>> RestoreAsync(
        TuningSnapshot snapshot,
        CancellationToken cancellationToken = default);
}
