using RotationSolver.Basic.Actions.PvPTargetSelection;

namespace RotationSolver.RebornRotations.PVPRotations.Ranged;

internal readonly record struct BardPvPShutdownInput(
	bool TargetHasResilience,
	bool TargetIsCasting,
	bool TargetThreatensFragileAlly,
	bool TargetIsBurstWorthy,
	bool TargetHasLowMp,
	float TargetHealthRatio,
	float TargetDistance,
	bool SafeBackstepExists,
	bool ObjectiveControlNeeded);

internal readonly record struct BardPvPKillSecureSnapshot(
	ulong TargetId,
	float HealthRatio,
	double EffectiveHealthRatio,
	double ExpectedDamageRatio,
	double ActiveDamageReduction,
	bool HasInvulnerability,
	bool HasAllyFocus = false,
	bool IsObjectiveRelevant = false);

internal static class BardPvPDecisionPolicy
{
	private const float KillPressureHealthRatio = 0.55f;
	private const float PaeanLowHealthRatio = 0.55f;
	private const float PaeanFocusedHealthRatio = 0.65f;
	private const float RepellingRangeYalms = 10f;
	private const double DirectSecureBaseScore = 8.0;
	private const double DirectSecureHealthWeight = 4.0;
	private const double DirectSecureAllyFocusScore = 1.5;
	private const double DirectSecureObjectiveScore = 1.0;

	internal static bool ShouldUseSilentNocturne(BardPvPShutdownInput input)
	{
		if (input.TargetHasResilience)
		{
			return false;
		}

		return input.TargetIsCasting
			|| input.TargetThreatensFragileAlly
			|| input.TargetIsBurstWorthy
			|| input.TargetHasLowMp
			|| input.TargetHealthRatio <= KillPressureHealthRatio;
	}

	internal static bool ShouldUseRepellingShot(BardPvPShutdownInput input)
	{
		if (input.TargetHasResilience || input.TargetDistance > RepellingRangeYalms)
		{
			return false;
		}

		if (!input.SafeBackstepExists)
		{
			return false;
		}

		return input.TargetThreatensFragileAlly
			|| input.ObjectiveControlNeeded
			|| input.TargetIsBurstWorthy
			|| input.TargetHealthRatio <= KillPressureHealthRatio;
	}

	internal static bool ShouldUseBurstOrForcedSpend(
		bool targetIsBurstWorthy,
		bool targetBlocksDamage,
		bool forcedSpendWindow,
		bool targetCanBeKilled = false)
	{
		if (targetBlocksDamage)
		{
			return false;
		}

		if (targetCanBeKilled)
		{
			return true;
		}

		if (targetIsBurstWorthy)
		{
			return true;
		}

		return forcedSpendWindow;
	}

	internal static bool ShouldUseApexArrow(bool hasBlastArrowReady)
	{
		return !hasBlastArrowReady;
	}

	internal static List<ulong> RankDirectSecureTargets(IReadOnlyList<BardPvPKillSecureSnapshot> targets)
	{
		List<(ulong TargetId, double Score)> rankedTargets = [];
		foreach (var target in targets)
		{
			if (!CanDirectSecure(target))
			{
				continue;
			}

			rankedTargets.Add((target.TargetId, ScoreDirectSecureTarget(target)));
		}

		rankedTargets.Sort((left, right) => right.Score.CompareTo(left.Score));

		List<ulong> targetIds = [];
		foreach (var target in rankedTargets)
		{
			targetIds.Add(target.TargetId);
		}

		return targetIds;
	}

	internal static bool ShouldUseProtectivePaean(float healthRatio, int focusCount)
	{
		if (focusCount > 0)
		{
			return healthRatio <= PaeanFocusedHealthRatio;
		}

		return healthRatio <= PaeanLowHealthRatio;
	}

	private static bool CanDirectSecure(BardPvPKillSecureSnapshot target)
	{
		if (target.TargetId == 0 || target.ExpectedDamageRatio <= 0.0)
		{
			return false;
		}

		var gateDecision = PvPDamageGate.Evaluate(new PvPDamageGateInput(
			Intent: PvPBurstIntent.Secure,
			EffectiveHpRatio: target.EffectiveHealthRatio,
			ExpectedDamageRatio: target.ExpectedDamageRatio,
			ActiveDamageReduction: target.ActiveDamageReduction,
			HasInvulnerability: target.HasInvulnerability,
			HasPrioritySignal: true));

		return gateDecision == PvPBurstRecommendation.Secure;
	}

	private static double ScoreDirectSecureTarget(BardPvPKillSecureSnapshot target)
	{
		var score = DirectSecureBaseScore;
		score += (1.0 - Math.Clamp(target.HealthRatio, 0f, 1f)) * DirectSecureHealthWeight;

		if (target.HasAllyFocus)
		{
			score += DirectSecureAllyFocusScore;
		}

		if (target.IsObjectiveRelevant)
		{
			score += DirectSecureObjectiveScore;
		}

		return score;
	}
}
