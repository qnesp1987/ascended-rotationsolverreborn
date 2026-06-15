using RotationSolver.Basic.Actions.PvPTargetSelection;

namespace RotationSolver.RebornRotations.PVPRotations.Ranged;

internal enum MachinistPvPActionIntent
{
	AnalysisDrill,
	AnalysisAirAnchor,
	AnalysisChainSaw,
	AnalysisBioblaster,
	Wildfire,
	Scattergun,
	Bishop,
	MarksmanSpite,
	FullMetalField,
	BlazingShot,
	EagleEyeShot,
}

internal readonly record struct MachinistPvPTargetSnapshot(
	ulong TargetId,
	float HealthRatio,
	uint CurrentMp,
	bool HasGuard,
	bool HasResilience,
	bool IsObjectiveRelevant,
	int AllyFocusCount,
	bool IsVulnerable,
	bool IsExposed,
	bool IsInNormalRange,
	bool IsInCloseRange,
	bool HasInvulnerability = false,
	bool HasWildfire = false,
	double ExpectedDamageRatio = 0.0,
	double EffectiveHealthRatio = 1.0,
	double ActiveDamageReduction = 0.0,
	PvPGuardAvailability GuardAvailability = PvPGuardAvailability.Unknown)
{
	internal bool HasAllyFocus => AllyFocusCount > 0;
}

internal static class MachinistPvPTargetPolicy
{
	private const double HealthPressureWeight = 4.0;
	private const double MpPressureWeight = 3.0;
	private const double ObjectiveScore = 1.5;
	private const int TeamFocusThreshold = 2;
	private const double AllyFocusScore = 1.25;
	private const double TeamFocusScore = 2.25;
	private const double VulnerableScore = 1.5;
	private const double ExposedScore = 1.0;
	private const double NormalRangeScore = 0.5;
	private const double CloseRangeScore = 0.5;
	private const double DirectSecureScore = 8.0;
	private const double GuardPenalty = 4.0;
	private const double ResiliencePenalty = 2.5;
	private const double DrillGuardPunishScore = 4.0;
	private const float GuardDrillPunishHealthRatio = 0.35f;

	internal static MachinistPvPTargetSnapshot? SelectBest(
		IReadOnlyList<MachinistPvPTargetSnapshot> targets,
		MachinistPvPActionIntent intent)
	{
		return PvPTargetRanking.SelectBest(targets, target => Score(target, intent), CompareScoredTargets);
	}

	internal static List<MachinistPvPTargetSnapshot> Rank(
		IReadOnlyList<MachinistPvPTargetSnapshot> targets,
		MachinistPvPActionIntent intent)
	{
		return PvPTargetRanking.Rank(targets, target => Score(target, intent), CompareScoredTargets);
	}

	private static int CompareScoredTargets(
		(MachinistPvPTargetSnapshot Target, double Score) left,
		(MachinistPvPTargetSnapshot Target, double Score) right)
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

	internal static double Score(MachinistPvPTargetSnapshot target, MachinistPvPActionIntent intent)
	{
		if (!target.IsInNormalRange)
		{
			return double.NegativeInfinity;
		}

		var score = HealthPressure(target.HealthRatio);
		score += MpPressure(target.CurrentMp);

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

		if (target.IsExposed)
		{
			score += ExposedScore;
		}

		if (target.IsInNormalRange)
		{
			score += NormalRangeScore;
		}

		if (target.IsInCloseRange)
		{
			score += CloseRangeScore;
		}

		score -= GuardCost(target, intent);
		score -= ResilienceCost(target, intent);

		return score;
	}

	private static bool CanDirectSecure(MachinistPvPTargetSnapshot target, MachinistPvPActionIntent intent)
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
			? target.HealthRatio
			: target.EffectiveHealthRatio;
		return effectiveHealthRatio <= target.ExpectedDamageRatio;
	}

	private static bool IgnoresGuard(MachinistPvPActionIntent intent)
	{
		return intent is MachinistPvPActionIntent.AnalysisDrill or MachinistPvPActionIntent.EagleEyeShot;
	}

	private static double AllyFocusValue(MachinistPvPTargetSnapshot target, MachinistPvPActionIntent intent)
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

	private static bool ReceivesNonLbFocusBonus(MachinistPvPActionIntent intent)
	{
		return intent is MachinistPvPActionIntent.AnalysisDrill
			or MachinistPvPActionIntent.AnalysisAirAnchor
			or MachinistPvPActionIntent.AnalysisChainSaw
			or MachinistPvPActionIntent.AnalysisBioblaster
			or MachinistPvPActionIntent.Wildfire
			or MachinistPvPActionIntent.Scattergun
			or MachinistPvPActionIntent.Bishop
			or MachinistPvPActionIntent.FullMetalField
			or MachinistPvPActionIntent.BlazingShot;
	}

	private static double HealthPressure(float healthRatio)
	{
		return PvPScoringFactors.ComputeHealthPressure(healthRatio, HealthPressureWeight);
	}

	private static double MpPressure(uint currentMp)
	{
		return PvPScoringFactors.ComputeMpPressure(currentMp) * MpPressureWeight;
	}

	private static double GuardCost(MachinistPvPTargetSnapshot target, MachinistPvPActionIntent intent)
	{
		if (!target.HasGuard)
		{
			return 0.0;
		}

		if (intent == MachinistPvPActionIntent.EagleEyeShot)
		{
			return 0.0;
		}

		if (intent == MachinistPvPActionIntent.AnalysisDrill
			&& target.HealthRatio <= GuardDrillPunishHealthRatio)
		{
			return -DrillGuardPunishScore;
		}

		return GuardPenalty;
	}

	private static double ResilienceCost(MachinistPvPTargetSnapshot target, MachinistPvPActionIntent intent)
	{
		if (!target.HasResilience)
		{
			return 0.0;
		}

		return intent is MachinistPvPActionIntent.AnalysisAirAnchor or MachinistPvPActionIntent.Scattergun
			? ResiliencePenalty
			: 0.0;
	}
}
