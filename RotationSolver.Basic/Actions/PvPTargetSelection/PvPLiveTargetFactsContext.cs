namespace RotationSolver.Basic.Actions.PvPTargetSelection;

/// <summary>
/// Caller-supplied frame dependencies required to translate a live PvP target into shared facts.
/// </summary>
/// <param name="MitigationDatabase">Mitigation lookup for the current plugin data version.</param>
/// <param name="ObjectiveRelevantTargetIds">Objective-relevant target ids from the caller's PvP mode context.</param>
/// <param name="Allies">Value snapshots for ally focus queries.</param>
/// <param name="CurrentTime">Current frame time supplied by the rotation caller.</param>
/// <param name="GuardCooldownTracker">Stateful Guard cooldown tracker owned outside the builder.</param>
/// <param name="GuardReactionWindow">Minimum Guard unavailable window required by the caller.</param>
/// <param name="ActionRange">Action range used for shared normal-range and exposure facts.</param>
/// <param name="DistanceToPlayerProvider">Caller distance policy, including current player position semantics.</param>
/// <param name="HealthRatioProvider">Caller health ratio policy, including any live refined HP semantics.</param>
/// <param name="HasStatus">Caller status policy, including any pending apply-status semantics.</param>
public readonly record struct PvPLiveTargetFactsContext(
	IMitigationDatabase MitigationDatabase,
	IReadOnlySet<ulong> ObjectiveRelevantTargetIds,
	IReadOnlyList<PvPCombatantSnapshot> Allies,
	TimeSpan CurrentTime,
	PvPGuardCooldownTracker GuardCooldownTracker,
	TimeSpan GuardReactionWindow,
	float ActionRange,
	Func<IBattleChara, float> DistanceToPlayerProvider,
	Func<IBattleChara, float> HealthRatioProvider,
	Func<IBattleChara, StatusID, bool> HasStatus);
