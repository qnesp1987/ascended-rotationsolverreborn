using RotationSolver.Basic.Actions.PvPTargetSelection;

namespace RotationSolver.RebornRotations.PVPRotations.Ranged;

internal readonly record struct BardPvPKillSecureFacts(
	double EffectiveHpRatio,
	double ExpectedDamageRatio,
	double RecuperateRatio,
	bool TargetCanRecuperate,
	bool HasGuard);

internal readonly record struct BardPvPShutdownInput(
	bool TargetHasResilience,
	bool TargetIsCasting,
	bool TargetThreatensFragileAlly,
	bool TargetIsBurstWorthy,
	float TargetHealthRatio,
	float TargetDistance,
	bool SafeBackstepExists,
	bool ObjectiveControlNeeded,
	BardPvPKillSecureFacts KillSecure = default);

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

	// Above this health ratio a target is "safe" and not worth a non-securing silence. Subsumes the
	// former kill-pressure use for Silent Nocturne; Repelling Shot keeps its own KillPressureHealthRatio.
	private const float EngagedHealthRatio = 0.70f;

	// Buffer (fraction of max HP) requiring the burst to clearly clear base HP before a silence counts
	// as kill-securing. Tunable.
	private const double SilenceSecureSafetyMargin = 0.05;
	private const float PaeanLowHealthRatio = 0.55f;
	private const float PaeanFocusedHealthRatio = 0.65f;
	private const float RepellingRangeYalms = 10f;
	private const double DirectSecureBaseScore = 8.0;
	private const double DirectSecureHealthWeight = 4.0;
	private const double DirectSecureAllyFocusScore = 1.5;
	private const double DirectSecureObjectiveScore = 1.0;

	internal static bool ShouldUseSilentNocturne(BardPvPShutdownInput input)
	{
		// Guard grants Silence immunity; Resilience nullifies re-applied CC after a Purify.
		// Silencing either is wasted.
		if (input.TargetHasResilience || input.KillSecure.HasGuard)
		{
			return false;
		}

		return WouldSilenceSecureKill(input)
			|| input.TargetThreatensFragileAlly
			|| input.TargetIsBurstWorthy
			|| input.ObjectiveControlNeeded
			|| input.TargetIsCasting
			|| input.TargetHealthRatio <= EngagedHealthRatio;
	}

	/// <summary>
	/// Silence secures a kill only in the gap where the incoming burst kills the target's base health
	/// but a Recuperate would otherwise save them. Outside that gap the silence adds no kill value.
	/// </summary>
	private static bool WouldSilenceSecureKill(BardPvPShutdownInput input)
	{
		var facts = input.KillSecure;
		if (!facts.TargetCanRecuperate || facts.ExpectedDamageRatio <= 0.0 || facts.EffectiveHpRatio <= 0.0)
		{
			return false;
		}

		// Burst must clearly kill base health (with margin) for a secure to be real.
		if (facts.EffectiveHpRatio + SilenceSecureSafetyMargin > facts.ExpectedDamageRatio)
		{
			return false;
		}

		// ...but only matters if a Recuperate would have pushed survival past the burst.
		var effectiveRecuperateRatio = EffectiveRecuperateRatio(input.TargetHealthRatio, facts);
		return facts.ExpectedDamageRatio < facts.EffectiveHpRatio + effectiveRecuperateRatio;
	}

	private static double EffectiveRecuperateRatio(float healthRatio, BardPvPKillSecureFacts facts)
	{
		if (healthRatio <= 0f || facts.EffectiveHpRatio <= 0.0)
		{
			return double.PositiveInfinity;
		}

		var damageMultiplier = healthRatio / facts.EffectiveHpRatio;
		if (damageMultiplier <= 0.0 || !double.IsFinite(damageMultiplier))
		{
			return double.PositiveInfinity;
		}

		return facts.RecuperateRatio / damageMultiplier;
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
