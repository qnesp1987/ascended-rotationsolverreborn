using RotationSolver.Basic.Actions.PvPTargetSelection;

namespace RotationSolver.RebornRotations.PVPRotations.Ranged;

internal enum BardPvPActionIntent
{
	PowerfulShot,
	HarmonicArrow,
	PitchPerfect,
	ApexArrow,
	BlastArrow,
	EncoreOfLight,
	EagleEyeShot,
}

internal readonly record struct BardPvPTargetSnapshot(
	ulong TargetId,
	float HealthRatio,
	uint CurrentMp,
	bool HasGuard,
	bool HasResilience,
	bool IsObjectiveRelevant,
	int AllyFocusCount,
	bool IsVulnerable,
	bool IsControlled,
	bool HasInvulnerability,
	double ExpectedDamageRatio,
	double EffectiveHealthRatio,
	double GuardPiercingEffectiveHealthRatio,
	double ActiveDamageReduction,
	bool IsExposed,
	bool IsInNormalRange,
	int LineTargetCount,
	int SplashTargetCount,
	PvPGuardAvailability GuardAvailability = PvPGuardAvailability.Unknown)
{
	internal bool HasAllyFocus => AllyFocusCount > 0;
}

internal readonly record struct BardPvPTargetSpatialState(
	bool IsInNormalRange,
	int LineTargetCount,
	int SplashTargetCount);

internal static class BardPvPTargetSnapshotRefresher
{
	internal static BardPvPTargetSnapshot RefreshSpatialState(
		BardPvPTargetSnapshot snapshot,
		BardPvPTargetSpatialState spatialState)
	{
		return snapshot with
		{
			IsExposed = !snapshot.HasGuard && spatialState.IsInNormalRange,
			IsInNormalRange = spatialState.IsInNormalRange,
			LineTargetCount = spatialState.LineTargetCount,
			SplashTargetCount = spatialState.SplashTargetCount,
		};
	}
}

internal static class BardPvPTargetPolicy
{
	private const double HealthPressureWeight = 4.0;
	private const double MpPressureWeight = 3.0;
	private const double ObjectiveScore = 1.5;
	private const int TeamFocusThreshold = 2;
	private const double AllyFocusScore = 1.25;
	private const double TeamFocusScore = 2.25;
	private const double VulnerableScore = 1.5;
	private const double ControlledScore = 1.25;
	private const double ExposedScore = 1.0;
	private const double DirectSecureScore = 8.0;
	private const double GuardPenalty = 4.0;
	private const double BlastArrowResiliencePenalty = 2.5;
	private const double AreaTargetScore = 0.75;
	private const double EncoreMpDenyScore = 2.0;
	private const uint FullMp = 10_000;

	internal static BardPvPTargetSnapshot? SelectBest(
		IReadOnlyList<BardPvPTargetSnapshot> targets,
		BardPvPActionIntent intent)
	{
		return PvPTargetRanking.SelectBest(targets, target => Score(target, intent), CompareScoredTargets);
	}

	internal static List<BardPvPTargetSnapshot> Rank(
		IReadOnlyList<BardPvPTargetSnapshot> targets,
		BardPvPActionIntent intent)
	{
		return PvPTargetRanking.Rank(targets, target => Score(target, intent), CompareScoredTargets);
	}

	internal static double Score(BardPvPTargetSnapshot target, BardPvPActionIntent intent)
	{
		if (!target.IsInNormalRange || target.HasInvulnerability)
		{
			return double.NegativeInfinity;
		}

		var score = HealthPressure(target.HealthRatio);
		score += MpPressure(target.CurrentMp);
		score += AreaValue(target, intent);
		score += EncoreMpDenyValue(target, intent);

		if (CanDirectSecure(target, intent))
		{
			score += DirectSecureScore;
		}

		if (target.IsObjectiveRelevant)
		{
			score += ObjectiveScore;
		}

		score += AllyFocusValue(target, intent);

		if (target.IsVulnerable)
		{
			score += VulnerableScore;
		}

		if (target.IsControlled)
		{
			score += ControlledScore;
		}

		if (HasExposureValue(target, intent))
		{
			score += ExposedScore;
		}

		score -= GuardCost(target, intent);
		score -= ResilienceCost(target, intent);

		return score;
	}

	private static int CompareScoredTargets(
		(BardPvPTargetSnapshot Target, double Score) left,
		(BardPvPTargetSnapshot Target, double Score) right)
	{
		var scoreComparison = right.Score.CompareTo(left.Score);
		if (scoreComparison != 0)
		{
			return scoreComparison;
		}

		var healthComparison = left.Target.HealthRatio.CompareTo(right.Target.HealthRatio);
		return healthComparison != 0
			? healthComparison
			: left.Target.TargetId.CompareTo(right.Target.TargetId);
	}

	private static bool CanDirectSecure(BardPvPTargetSnapshot target, BardPvPActionIntent intent)
	{
		if (target.ExpectedDamageRatio <= 0.0 || target.HasInvulnerability)
		{
			return false;
		}

		var ignoresGuard = IgnoresGuard(intent);
		if (target.HasGuard && !ignoresGuard)
		{
			return false;
		}

		var effectiveHealthRatio = target.HasGuard && ignoresGuard
			? target.GuardPiercingEffectiveHealthRatio
			: target.EffectiveHealthRatio;

		var gateDecision = PvPDamageGate.Evaluate(new PvPDamageGateInput(
			Intent: PvPBurstIntent.Secure,
			EffectiveHpRatio: effectiveHealthRatio,
			ExpectedDamageRatio: target.ExpectedDamageRatio,
			ActiveDamageReduction: target.ActiveDamageReduction,
			HasInvulnerability: target.HasInvulnerability,
			HasPrioritySignal: true));

		return gateDecision == PvPBurstRecommendation.Secure;
	}

	private static bool IgnoresGuard(BardPvPActionIntent intent)
	{
		return intent == BardPvPActionIntent.EagleEyeShot;
	}

	private static double AllyFocusValue(BardPvPTargetSnapshot target, BardPvPActionIntent intent)
	{
		if (!target.HasAllyFocus)
		{
			return 0.0;
		}

		if (ReceivesNonLbFocusBonus(intent) && target.AllyFocusCount >= TeamFocusThreshold)
		{
			return TeamFocusScore;
		}

		return AllyFocusScore;
	}

	private static bool ReceivesNonLbFocusBonus(BardPvPActionIntent intent)
	{
		return intent is BardPvPActionIntent.PowerfulShot
			or BardPvPActionIntent.HarmonicArrow
			or BardPvPActionIntent.PitchPerfect
			or BardPvPActionIntent.ApexArrow
			or BardPvPActionIntent.BlastArrow
			or BardPvPActionIntent.EncoreOfLight;
	}

	private static bool HasExposureValue(BardPvPTargetSnapshot target, BardPvPActionIntent intent)
	{
		return target.IsExposed || (IgnoresGuard(intent) && target.HasGuard && target.IsInNormalRange);
	}

	private static double HealthPressure(float healthRatio)
	{
		return PvPScoringFactors.ComputeHealthPressure(healthRatio, HealthPressureWeight);
	}

	private static double MpPressure(uint currentMp)
	{
		return PvPScoringFactors.ComputeMpPressure(currentMp) * MpPressureWeight;
	}

	private static double AreaValue(BardPvPTargetSnapshot target, BardPvPActionIntent intent)
	{
		var targetCount = intent switch
		{
			BardPvPActionIntent.ApexArrow or BardPvPActionIntent.BlastArrow => target.LineTargetCount,
			BardPvPActionIntent.PitchPerfect or BardPvPActionIntent.EncoreOfLight => target.SplashTargetCount,
			_ => 0,
		};

		return Math.Max(0, targetCount - 1) * AreaTargetScore;
	}

	private static double EncoreMpDenyValue(BardPvPTargetSnapshot target, BardPvPActionIntent intent)
	{
		if (intent != BardPvPActionIntent.EncoreOfLight)
		{
			return 0.0;
		}

		var spentMpRatio = 1.0 - Math.Clamp((double)target.CurrentMp / FullMp, 0.0, 1.0);
		return spentMpRatio * EncoreMpDenyScore;
	}

	private static double GuardCost(BardPvPTargetSnapshot target, BardPvPActionIntent intent)
	{
		if (!target.HasGuard || IgnoresGuard(intent))
		{
			return 0.0;
		}

		return GuardPenalty;
	}

	private static double ResilienceCost(BardPvPTargetSnapshot target, BardPvPActionIntent intent)
	{
		return intent == BardPvPActionIntent.BlastArrow
			? PvPScoringFactors.ComputeResiliencePenalty(target.HasResilience) * BlastArrowResiliencePenalty
			: 0.0;
	}
}
