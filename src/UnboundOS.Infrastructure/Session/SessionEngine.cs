using UnboundOS.Core.Abstractions;
using UnboundOS.Core.Models;

namespace UnboundOS.Infrastructure.Session;

public sealed class SessionEngine : ISessionEngine
{
    private readonly IProcessGuardian _processes;
    private readonly INetworkDirector _network;
    private readonly IPowerPlanService _power;
    private readonly IGamingSkinnyPolicy _skinny;
    private readonly object _gate = new();

    public SessionEngine(
        IProcessGuardian processes,
        INetworkDirector network,
        IPowerPlanService power,
        IGamingSkinnyPolicy skinny)
    {
        _processes = processes;
        _network = network;
        _power = power;
        _skinny = skinny;
    }

    public SessionState State { get; private set; } = SessionState.Idle;
    public SessionProfile? ActiveProfile { get; private set; }
    public SessionSnapshot? LastSnapshot { get; private set; }

    public event EventHandler<SessionState>? StateChanged;

    public async Task<SessionMutationResult> EnterAsync(SessionProfile profile, CancellationToken ct = default)
    {
        lock (_gate)
        {
            if (State is SessionState.Active or SessionState.Entering)
            {
                return new SessionMutationResult
                {
                    Succeeded = false,
                    Message = "A session is already active."
                };
            }

            SetState(SessionState.Entering);
        }

        var actions = new List<string>();
        IReadOnlyDictionary<string, int>? capturedMetrics = null;
        string? originalPower = null;
        var trafficApplied = false;
        var powerApplied = false;

        try
        {
            var metrics = await _network.CaptureMetricsAsync(ct).ConfigureAwait(false);
            capturedMetrics = metrics;
            actions.Add($"Captured metrics for {metrics.Count} adapter(s).");

            var adapters = await _network.ListAdaptersAsync(ct).ConfigureAwait(false);
            var plan = _network.RecommendPlan(adapters, profile);
            actions.Add(plan.Summary);

            var traffic = await _network.ApplyTrafficSeparationAsync(
                    plan.GameAdapterId,
                    plan.StreamAdapterId,
                    allowElevation: true,
                    ct)
                .ConfigureAwait(false);
            trafficApplied = traffic.Succeeded;
            actions.AddRange(traffic.Actions);
            actions.Add(traffic.Message);

            if (profile.EnableHighPerformancePowerHint)
            {
                originalPower = await _power.GetActiveSchemeGuidAsync(ct).ConfigureAwait(false);
                var power = await _power.ActivateHighPerformanceAsync(ct).ConfigureAwait(false);
                powerApplied = power.Succeeded;
                actions.Add(power.Message);
            }

            var terminated = await _processes.TerminateAsync(
                    profile.TerminateProcessNames,
                    profile.ProtectProcessNames,
                    ct)
                .ConfigureAwait(false);

            actions.Add(terminated.Count == 0
                ? "No denylist processes were running."
                : $"Stopped {terminated.Count} background process(es).");

            GamingSkinnySnapshot? skinnySnap = null;
            try
            {
                skinnySnap = await _skinny.ApplyAsync(profile, ct).ConfigureAwait(false);
                actions.AddRange(skinnySnap.Actions);
            }
            catch (Exception skinnyEx)
            {
                actions.Add($"Skinny posture skipped: {skinnyEx.Message}");
            }

            var snapshot = new SessionSnapshot
            {
                ProfileId = profile.Id,
                TerminatedProcesses = terminated,
                OriginalAdapterMetrics = metrics,
                OriginalPowerSchemeGuid = originalPower,
                Skinny = skinnySnap,
                Notes = plan.Summary
            };

            lock (_gate)
            {
                ActiveProfile = profile;
                LastSnapshot = snapshot;
                SetState(SessionState.Active);
            }

            var partial = !traffic.Succeeded || (profile.EnableHighPerformancePowerHint && !powerApplied);
            return new SessionMutationResult
            {
                Succeeded = true,
                Message = partial
                    ? $"Session live with partial system changes — {profile.Name}"
                    : $"Unbound session live — {profile.Name}",
                Actions = actions,
                Snapshot = snapshot
            };
        }
        catch (Exception ex)
        {
            if (trafficApplied && capturedMetrics is not null)
            {
                try
                {
                    var restore = await _network.RestoreMetricsAsync(capturedMetrics, allowElevation: true, ct)
                        .ConfigureAwait(false);
                    actions.Add(restore.Message);
                    actions.AddRange(restore.Actions);
                }
                catch (Exception restoreEx)
                {
                    actions.Add($"Could not restore NIC metrics after failed enter: {restoreEx.Message}");
                }
            }

            if (powerApplied && originalPower is not null)
            {
                try
                {
                    var restorePower = await _power.RestoreSchemeAsync(originalPower, ct).ConfigureAwait(false);
                    actions.Add(restorePower.Message);
                }
                catch (Exception restoreEx)
                {
                    actions.Add($"Could not restore power plan after failed enter: {restoreEx.Message}");
                }
            }

            lock (_gate)
            {
                SetState(SessionState.Faulted);
            }

            return new SessionMutationResult
            {
                Succeeded = false,
                Message = $"Failed to enter session: {ex.Message}",
                Actions = actions
            };
        }
    }

    public async Task<SessionMutationResult> ExitAsync(CancellationToken ct = default)
    {
        SessionSnapshot? snapshot;
        lock (_gate)
        {
            if (State is not SessionState.Active and not SessionState.Faulted)
            {
                return new SessionMutationResult
                {
                    Succeeded = false,
                    Message = "No active session to exit."
                };
            }

            snapshot = LastSnapshot;
            SetState(SessionState.Exiting);
        }

        var actions = new List<string>();

        try
        {
            if (snapshot is not null)
            {
                var restore = await _network.RestoreMetricsAsync(
                        snapshot.OriginalAdapterMetrics,
                        allowElevation: true,
                        ct)
                    .ConfigureAwait(false);
                actions.Add(restore.Message);
                actions.AddRange(restore.Actions);

                var restorePower = await _power.RestoreSchemeAsync(snapshot.OriginalPowerSchemeGuid, ct)
                    .ConfigureAwait(false);
                actions.Add(restorePower.Message);

                var restoreSkinny = await _skinny.RestoreAsync(snapshot.Skinny, ct).ConfigureAwait(false);
                actions.Add(restoreSkinny.Message);

                actions.Add(
                    $"Session had cleared {snapshot.TerminatedProcesses.Count} process(es); relaunch apps as needed.");
            }

            lock (_gate)
            {
                ActiveProfile = null;
                SetState(SessionState.Idle);
            }

            return new SessionMutationResult
            {
                Succeeded = true,
                Message = "Returned to desktop posture.",
                Actions = actions,
                Snapshot = snapshot
            };
        }
        catch (Exception ex)
        {
            lock (_gate)
            {
                SetState(SessionState.Faulted);
            }

            return new SessionMutationResult
            {
                Succeeded = false,
                Message = $"Exit incomplete: {ex.Message}",
                Actions = actions
            };
        }
    }

    private void SetState(SessionState state)
    {
        State = state;
        StateChanged?.Invoke(this, state);
    }
}
