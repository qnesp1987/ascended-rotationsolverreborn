using RotationSolver.Basic.Actions.PvPTargetSelection.Factors;

namespace RotationSolver.Basic.Actions.PvPTargetSelection;

/// <summary>
/// Translates live PvP combatants into shared target facts using caller-supplied frame dependencies.
/// </summary>
public static class PvPLiveTargetFactsBuilder
{
	private const ulong NoExcludedObjectId = 0;

	/// <summary>
	/// Creates shared target facts for one live combatant without reading global PvP frame state.
	/// </summary>
	public static PvPLiveTargetFacts Create(IBattleChara target, PvPLiveTargetFactsContext context)
	{
		ArgumentNullException.ThrowIfNull(target);
		ArgumentNullException.ThrowIfNull(context.MitigationDatabase);
		ArgumentNullException.ThrowIfNull(context.ObjectiveRelevantTargetIds);
		ArgumentNullException.ThrowIfNull(context.Allies);
		ArgumentNullException.ThrowIfNull(context.GuardCooldownTracker);
		ArgumentNullException.ThrowIfNull(context.DistanceToPlayerProvider);
		ArgumentNullException.ThrowIfNull(context.HealthRatioProvider);
		ArgumentNullException.ThrowIfNull(context.HasStatus);

		var targetId = target.GameObjectId;
		var mitigationDatabase = context.MitigationDatabase;
		var effectiveHealth = EffectiveHpCalculator.Compute(target, mitigationDatabase);
		var guardPiercingEffectiveHealth = EffectiveHpCalculator.ComputeIgnoringGuard(target, mitigationDatabase);
		var isInNormalRange = context.DistanceToPlayerProvider(target) <= context.ActionRange;
		var hasGuard = context.HasStatus(target, StatusID.Guard);
		var allyFocusCount = PvPCombatantQueries.CountAlliesTargeting(context.Allies, targetId);

		return new PvPLiveTargetFacts(
			TargetId: targetId,
			HealthRatio: context.HealthRatioProvider(target),
			CurrentMp: target.CurrentMp,
			HasGuard: hasGuard,
			HasResilience: context.HasStatus(target, StatusID.Resilience),
			IsObjectiveRelevant: context.ObjectiveRelevantTargetIds.Contains(targetId),
			AllyFocusCount: allyFocusCount,
			HasNonGuardInvulnerability: HasNonGuardInvulnerability(target, mitigationDatabase),
			EffectiveHealthRatio: ToEffectiveHealthRatio(target, effectiveHealth),
			GuardPiercingEffectiveHealthRatio: ToEffectiveHealthRatio(target, guardPiercingEffectiveHealth),
			ActiveDamageReduction: MitigationPenalty.Compute(target, mitigationDatabase),
			IsExposed: !hasGuard && isInNormalRange,
			IsInNormalRange: isInNormalRange,
			GuardAvailability: context.GuardCooldownTracker.GetAvailability(
				targetId,
				context.CurrentTime,
				context.GuardReactionWindow));
	}

	/// <summary>
	/// Captures value-only combatant facts needed by shared PvP query helpers.
	/// </summary>
	public static PvPCombatantSnapshot ToCombatantSnapshot(
		IBattleChara combatant,
		Func<IBattleChara, float> healthRatioProvider)
	{
		ArgumentNullException.ThrowIfNull(combatant);
		ArgumentNullException.ThrowIfNull(healthRatioProvider);

		return new PvPCombatantSnapshot(
			ObjectId: combatant.GameObjectId,
			HealthRatio: healthRatioProvider(combatant),
			CurrentHp: combatant.CurrentHp,
			TargetObjectId: combatant.TargetObjectId,
			Position: combatant.Position,
			HitboxRadius: combatant.HitboxRadius);
	}

	/// <summary>
	/// Captures value-only combatant facts for live collections while skipping null object entries.
	/// </summary>
	/// <param name="combatants">Live combatants supplied by the caller's frame boundary.</param>
	/// <param name="healthRatioProvider">Caller health policy used for each captured combatant.</param>
	/// <param name="excludedObjectId">Optional object id to skip, used when the local player should not count as ally focus.</param>
	public static List<PvPCombatantSnapshot> ToCombatantSnapshots(
		IEnumerable<IBattleChara?> combatants,
		Func<IBattleChara, float> healthRatioProvider,
		ulong excludedObjectId = NoExcludedObjectId)
	{
		ArgumentNullException.ThrowIfNull(combatants);
		ArgumentNullException.ThrowIfNull(healthRatioProvider);

		List<PvPCombatantSnapshot> snapshots = [];
		foreach (var combatant in combatants)
		{
			if (combatant == null)
			{
				continue;
			}

			if (excludedObjectId != NoExcludedObjectId
				&& combatant.GameObjectId == excludedObjectId)
			{
				continue;
			}

			snapshots.Add(ToCombatantSnapshot(combatant, healthRatioProvider));
		}

		return snapshots;
	}

	/// <summary>
	/// Finds the live combatant for a previously captured target id while tolerating null entries.
	/// </summary>
	public static IBattleChara? FindLiveTargetById(IEnumerable<IBattleChara?> combatants, ulong targetId)
	{
		ArgumentNullException.ThrowIfNull(combatants);

		foreach (var combatant in combatants)
		{
			if (combatant?.GameObjectId == targetId)
			{
				return combatant;
			}
		}

		return null;
	}

	private static bool HasNonGuardInvulnerability(IBattleChara target, IMitigationDatabase database)
	{
		var statusList = target.StatusList;
		if (statusList == null)
		{
			return false;
		}

		foreach (var status in statusList)
		{
			var statusId = (StatusID)status.StatusId;
			if (statusId == StatusID.Guard)
			{
				continue;
			}

			if (database.TryGet(statusId, out var entry) && entry.Kind == MitigationKind.Invuln)
			{
				return true;
			}
		}

		return false;
	}

	private static double ToEffectiveHealthRatio(IBattleChara target, double effectiveHealth)
	{
		if (target.MaxHp == 0 || double.IsPositiveInfinity(effectiveHealth))
		{
			return double.PositiveInfinity;
		}

		return effectiveHealth / target.MaxHp;
	}
}
