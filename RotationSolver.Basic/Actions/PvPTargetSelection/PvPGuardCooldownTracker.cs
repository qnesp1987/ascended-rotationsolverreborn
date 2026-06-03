namespace RotationSolver.Basic.Actions.PvPTargetSelection;

/// <summary>
/// Availability estimate for a hostile player's universal PvP Guard action.
/// </summary>
public enum PvPGuardAvailability
{
	/// <summary>
	/// Guard has not been observed recently enough to infer its cooldown.
	/// </summary>
	Unknown,

	/// <summary>
	/// Guard is currently active on the target.
	/// </summary>
	Active,

	/// <summary>
	/// Guard was observed and should remain unavailable through the requested window.
	/// </summary>
	CoolingDown,

	/// <summary>
	/// Guard is ready, or it returns before the requested action window is complete.
	/// </summary>
	Ready,
}

/// <summary>
/// Value-only observation used to update inferred enemy Guard cooldown state.
/// </summary>
public readonly record struct PvPGuardCooldownObservation(
	ulong TargetId,
	TimeSpan ObservedAt,
	bool HasGuard,
	TimeSpan GuardRemaining);

/// <summary>
/// Tracks observed PvP Guard activations so rotations can share one conservative reaction model.
/// </summary>
public sealed class PvPGuardCooldownTracker
{
	/// <summary>
	/// Patch 7.5 PvP Guard active duration.
	/// </summary>
	public static readonly TimeSpan GuardDuration = TimeSpan.FromSeconds(4);

	/// <summary>
	/// Patch 7.5 PvP Guard recast.
	/// </summary>
	public static readonly TimeSpan GuardRecast = TimeSpan.FromSeconds(30);

	/// <summary>
	/// Default visibility grace before inferred Guard cooldown state becomes unreliable.
	/// </summary>
	public static readonly TimeSpan DefaultMaxUnseenDuration = TimeSpan.FromSeconds(5);

	private readonly Dictionary<ulong, GuardCooldownState> _targets = [];

	/// <summary>
	/// Records the latest visible Guard state for a target.
	/// </summary>
	public void Observe(PvPGuardCooldownObservation observation)
	{
		if (observation.TargetId == 0)
		{
			return;
		}

		if (observation.HasGuard)
		{
			var remaining = Clamp(observation.GuardRemaining, TimeSpan.Zero, GuardDuration);
			var guardStartedAt = observation.ObservedAt - (GuardDuration - remaining);
			var activeUntil = observation.ObservedAt + remaining;
			_targets[observation.TargetId] = new GuardCooldownState(
				ReadyAt: guardStartedAt + GuardRecast,
				ActiveUntil: activeUntil,
				LastSeenAt: observation.ObservedAt);
			return;
		}

		if (_targets.TryGetValue(observation.TargetId, out var state))
		{
			_targets[observation.TargetId] = state with
			{
				ActiveUntil = TimeSpan.MinValue,
				LastSeenAt = observation.ObservedAt,
			};
		}
	}

	/// <summary>
	/// Returns inferred Guard availability for the target at the requested time.
	/// </summary>
	public PvPGuardAvailability GetAvailability(
		ulong targetId,
		TimeSpan now,
		TimeSpan requiredUnavailableWindow)
	{
		if (targetId == 0 || !_targets.TryGetValue(targetId, out var state))
		{
			return PvPGuardAvailability.Unknown;
		}

		if (now < state.ActiveUntil)
		{
			return PvPGuardAvailability.Active;
		}

		return state.ReadyAt > now + requiredUnavailableWindow
			? PvPGuardAvailability.CoolingDown
			: PvPGuardAvailability.Ready;
	}

	/// <summary>
	/// Removes targets whose state is no longer reliable because they were unseen.
	/// </summary>
	public void ForgetUnseen(
		TimeSpan now,
		IReadOnlySet<ulong> observedTargetIds,
		TimeSpan maxUnseenDuration)
	{
		List<ulong> staleTargetIds = [];
		foreach (var (targetId, state) in _targets)
		{
			if (observedTargetIds.Contains(targetId))
			{
				continue;
			}

			if (now - state.LastSeenAt >= maxUnseenDuration)
			{
				staleTargetIds.Add(targetId);
			}
		}

		foreach (var targetId in staleTargetIds)
		{
			_targets.Remove(targetId);
		}
	}

	/// <summary>
	/// Removes one target's inferred cooldown state after death or identity reset.
	/// </summary>
	public void Forget(ulong targetId) => _targets.Remove(targetId);

	/// <summary>
	/// Clears all inferred cooldown state when match context changes.
	/// </summary>
	public void Reset() => _targets.Clear();

	private static TimeSpan Clamp(TimeSpan value, TimeSpan minimum, TimeSpan maximum)
	{
		if (value < minimum)
		{
			return minimum;
		}

		return value > maximum ? maximum : value;
	}

	private readonly record struct GuardCooldownState(
		TimeSpan ReadyAt,
		TimeSpan ActiveUntil,
		TimeSpan LastSeenAt);
}
