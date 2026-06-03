namespace RotationSolver.Basic.Actions.PvPTargetSelection;

/// <summary>
/// Shared live target facts extracted once before Bard and Machinist map job-specific snapshot fields.
/// </summary>
/// <param name="TargetId">Live object id used to map facts back to the selected target.</param>
/// <param name="HealthRatio">Caller-supplied health ratio after plugin live HP policy is applied.</param>
/// <param name="CurrentMp">Current MP exposed for PvP resource pressure.</param>
/// <param name="HasGuard">Whether caller status policy reports active Guard for this target.</param>
/// <param name="HasResilience">Whether caller status policy reports active Resilience for this target.</param>
/// <param name="IsObjectiveRelevant">Whether caller-supplied objective ids include this target.</param>
/// <param name="AllyFocusCount">How many caller-supplied ally snapshots target this target.</param>
/// <param name="HasNonGuardInvulnerability">Whether a non-Guard mitigation entry blocks normal damage.</param>
/// <param name="EffectiveHealthRatio">Effective health ratio with all modeled mitigation.</param>
/// <param name="GuardPiercingEffectiveHealthRatio">Effective health ratio for Guard-piercing actions.</param>
/// <param name="ActiveDamageReduction">Additive active damage reduction signal used by target policies.</param>
/// <param name="IsExposed">Whether the target is unguarded and inside the caller's action range.</param>
/// <param name="IsInNormalRange">Whether the target is inside the caller's action range.</param>
/// <param name="GuardAvailability">Caller-supplied Guard cooldown inference for this target.</param>
public readonly record struct PvPLiveTargetFacts(
	ulong TargetId,
	float HealthRatio,
	uint CurrentMp,
	bool HasGuard,
	bool HasResilience,
	bool IsObjectiveRelevant,
	int AllyFocusCount,
	bool HasNonGuardInvulnerability,
	double EffectiveHealthRatio,
	double GuardPiercingEffectiveHealthRatio,
	double ActiveDamageReduction,
	bool IsExposed,
	bool IsInNormalRange,
	PvPGuardAvailability GuardAvailability)
{
	/// <summary>
	/// Whether any caller-supplied ally snapshot targets this target.
	/// </summary>
	public bool HasAllyFocus => AllyFocusCount > 0;
}
