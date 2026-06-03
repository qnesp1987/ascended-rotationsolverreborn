namespace RotationSolver.Basic.Actions.PvPTargetSelection;

/// <summary>
/// Computes effective HP: the damage required to kill a target given currently-active
/// damage-reduction statuses. Multiplicative stacking. Invuln short-circuits to infinity.
/// Pure: no I/O. Reads properties from the passed-in <see cref="IBattleChara"/> only.
/// </summary>
public static class EffectiveHpCalculator
{
	/// <summary>
	/// Computes target eHP with all modeled active mitigation, including Guard.
	/// </summary>
	public static double Compute(IBattleChara target, IMitigationDatabase database)
	{
		ArgumentNullException.ThrowIfNull(target);
		ArgumentNullException.ThrowIfNull(database);

		return Compute(target, database, ignoredStatus: null);
	}

	/// <summary>
	/// Computes target eHP for Guard-piercing actions while preserving every other modeled mitigation.
	/// </summary>
	public static double ComputeIgnoringGuard(IBattleChara target, IMitigationDatabase database)
	{
		ArgumentNullException.ThrowIfNull(target);
		ArgumentNullException.ThrowIfNull(database);

		return Compute(target, database, static statusId => statusId == StatusID.Guard);
	}

	private static double Compute(
		IBattleChara target,
		IMitigationDatabase database,
		Func<StatusID, bool>? ignoredStatus)
	{
		var statusList = target.StatusList;
		if (statusList == null)
		{
			return target.CurrentHp;
		}

		var damageMultiplier = 1.0;
		foreach (var status in statusList)
		{
			var statusId = (StatusID)status.StatusId;
			if (ignoredStatus?.Invoke(statusId) == true || !database.TryGet(statusId, out var entry))
			{
				continue;
			}
			if (entry.Kind == MitigationKind.Invuln)
			{
				return double.PositiveInfinity;
			}
			damageMultiplier *= 1.0 - entry.DamageReductionPercent;
		}

		// damageMultiplier is the fraction of damage that lands. eHP = CurrentHp / damageMultiplier.
		// Guard against pathological 100% DR producing infinity; treat as effective invuln.
		if (damageMultiplier <= 0.0)
		{
			return double.PositiveInfinity;
		}
		return target.CurrentHp / damageMultiplier;
	}
}
