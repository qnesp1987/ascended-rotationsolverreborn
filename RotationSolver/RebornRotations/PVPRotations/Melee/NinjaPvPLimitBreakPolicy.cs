namespace RotationSolver.RebornRotations.PVPRotations.Melee;

internal readonly record struct NinjaPvPLimitBreakTargetSnapshot(
	ulong TargetId,
	float HealthRatio,
	float DistanceToPlayer,
	bool HasRespectedInvulnerability);

internal static class NinjaPvPLimitBreakPolicy
{
	internal const float ExecuteHealthRatioThreshold = 0.35f;
	internal const float LimitBreakRangeYalms = 10f;

	internal static NinjaPvPLimitBreakTargetSnapshot? SelectBest(
		IReadOnlyList<NinjaPvPLimitBreakTargetSnapshot> targets)
	{
		var ranked = Rank(targets);
		return ranked.Count == 0 ? null : ranked[0];
	}

	internal static List<NinjaPvPLimitBreakTargetSnapshot> Rank(
		IReadOnlyList<NinjaPvPLimitBreakTargetSnapshot> targets)
	{
		List<NinjaPvPLimitBreakTargetSnapshot> executable = [];
		foreach (var target in targets)
		{
			if (ShouldExecute(target))
			{
				executable.Add(target);
			}
		}

		executable.Sort((left, right) => left.DistanceToPlayer.CompareTo(right.DistanceToPlayer));
		return executable;
	}

	internal static bool ShouldExecute(NinjaPvPLimitBreakTargetSnapshot target)
	{
		if (target.HasRespectedInvulnerability)
		{
			return false;
		}

		if (target.DistanceToPlayer > LimitBreakRangeYalms)
		{
			return false;
		}

		return target.HealthRatio < ExecuteHealthRatioThreshold;
	}
}
