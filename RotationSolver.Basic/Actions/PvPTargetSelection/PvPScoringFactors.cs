namespace RotationSolver.Basic.Actions.PvPTargetSelection;

/// <summary>
/// Computes pure PvPSmart scalar factors so live target adapters and tests share the
/// same Ranked CC pressure thresholds.
/// </summary>
public static class PvPScoringFactors
{
	/// <summary>
	/// Targets at or below this MP have limited Recuperate and Purify response budget.
	/// </summary>
	public const uint LowMp = 2_000;

	/// <summary>
	/// Targets at or below this MP are worth pressure, but less than near-empty targets.
	/// </summary>
	public const uint MediumMp = 4_000;

	/// <summary>
	/// Score MP pressure because Bard control and Encore are strongest when the target
	/// cannot freely answer with MP spenders.
	/// </summary>
	public static double ComputeMpPressure(uint currentMp)
	{
		if (currentMp <= LowMp)
		{
			return 1.0;
		}

		if (currentMp <= MediumMp)
		{
			return 0.5;
		}

		return 0.0;
	}

	/// <summary>
	/// Score missing health linearly because finishing low targets is every ranged
	/// kit's highest-value pressure; the per-job weight keeps tuning job-local.
	/// </summary>
	public static double ComputeHealthPressure(float healthRatio, double weight)
	{
		return (1.0 - Math.Clamp(healthRatio, 0f, 1f)) * weight;
	}

	/// <summary>
	/// Score objective pressure only from already verified target ids so target selection
	/// never depends on guessed Crystalline Conflict identifiers.
	/// </summary>
	public static double ComputeObjectivePressure(ulong targetId, IReadOnlySet<ulong> objectiveRelevantTargetIds)
	{
		return objectiveRelevantTargetIds.Contains(targetId) ? 1.0 : 0.0;
	}

	/// <summary>
	/// Score Resilience as a penalty because Bard silence, bind, and knockback control
	/// lose value while the target is crowd-control protected.
	/// </summary>
	public static double ComputeResiliencePenalty(bool hasResilience)
	{
		return hasResilience ? 1.0 : 0.0;
	}
}
