using System.Numerics;
using RotationSolver.Basic.Actions.PvPTargetSelection;

namespace RotationSolver.Tests;

internal static partial class PvPTestSuite
{
	static void PvPSmartDefaultPresetIsRanked()
	{
		AssertEqual(ScoringPreset.Ranked, ScoringWeights.DefaultPreset, "PvPSmart should default to Ranked for Ranked CC Bard tuning");
	}

	static void PvPSmartDefaultWeightsMatchRanked()
	{
		var expected = ScoringWeights.ForPreset(ScoringPreset.Ranked);

		AssertEqual(expected, ScoringWeights.DefaultWeights, "PvPSmart default weights should match Ranked weights");
	}

	static void LegacyCustomPvPWeightsFillNewControlDefaults()
	{
		var legacyCustomWeights = LegacyTunedCustomWeights();
		var migrated = ScoringWeights.MigrateLegacyCustomWeights(legacyCustomWeights);
		var expected = new ScoringWeights(
			RoleWeight: 1.25,
			FinishWeight: 1.40,
			MitigationPenaltyWeight: 1.30,
			DistancePenaltyWeight: 0.20,
			StickyBonus: 0.08,
			CarrierWeight: 0.75,
			LBWeight: 1.20,
			IsolationWeight: 0.35,
			ThreatWeight: 0.55,
			MpPressureWeight: 0.40,
			ResiliencePenaltyWeight: 0.50,
			ObjectiveWeight: 0.50);

		AssertEqual(expected, migrated, "legacy custom migration should preserve old weights and seed new control weights");
	}

	static void LegacyCasualPvPWeightsAreDetected()
	{
		var legacyDefault = LegacyCasualWeights();

		AssertTrue(ScoringWeights.IsLegacyCasualDefault(legacyDefault), "legacy Casual default should be detected for config migration");
		AssertFalse(ScoringWeights.IsLegacyCasualDefault(ScoringWeights.ForPreset(ScoringPreset.Casual)), "current Casual default should not be treated as legacy");
	}

	static void LegacyDefaultPvPWeightsMigrateToRankedDefaults()
	{
		var migrated = ScoringWeights.MigrateLegacyDefaultWeights(LegacyCasualWeights());

		AssertEqual(ScoringWeights.DefaultWeights, migrated, "legacy default PvP weights should migrate to Ranked defaults");
	}

	static void LegacyPvPScoringConfigMigratesDefaultPresetAndWeights()
	{
		var migrated = ScoringWeights.MigrateLegacyConfig(ScoringPreset.Casual, LegacyCasualWeights());

		AssertEqual(ScoringPreset.Ranked, migrated.Preset, "legacy Casual preset should migrate to Ranked");
		AssertEqual(ScoringWeights.DefaultWeights, migrated.Weights, "legacy Casual backing weights should migrate to Ranked defaults");
	}

	static void PvPCombatantQueriesFindHostileById()
	{
		var hostiles = new[]
		{
			Combatant(10, health: 1f),
			Combatant(20, health: 1f),
		};

		var found = PvPCombatantQueries.FindById(hostiles, 20);

		AssertEqual(20UL, found?.ObjectId ?? 0, "query should return the matching hostile");
	}

	static void PvPCombatantQueriesCountAllyFocus()
	{
		var allies = new[]
		{
			Combatant(1, health: 1f, targetId: 99),
			Combatant(2, health: 1f, targetId: 99),
			Combatant(3, health: 0f, targetId: 99),
		};

		var count = PvPCombatantQueries.CountAlliesTargeting(allies, 99);

		AssertEqual(3, count, "focus counts should preserve current target id semantics");
	}

	static void PvPCombatantQueriesCountHostilesTargetingAlly()
	{
		var hostiles = new[]
		{
			Combatant(10, health: 1f, targetId: 5),
			Combatant(11, health: 1f, targetId: 5),
			Combatant(12, health: 0f, targetId: 5),
		};

		var count = PvPCombatantQueries.CountHostilesTargeting(hostiles, 5);

		AssertEqual(3, count, "hostile focus counts should preserve current target id semantics");
	}

	static void PvPCombatantQueriesCountNearbyHostiles()
	{
		var hostiles = new[]
		{
			Combatant(10, health: 1f, position: new Vector3(0f, 0f, 3f)),
			Combatant(11, health: 1f, position: new Vector3(0f, 0f, 8f)),
			Combatant(12, health: 0f, position: new Vector3(0f, 0f, 2f)),
		};

		var count = PvPCombatantQueries.CountHostilesNear(hostiles, Vector3.Zero, radius: 5f);

		AssertEqual(1, count, "only living hostiles inside radius should count");
	}

	static void PvPCombatantQueriesCountNearbyAllies()
	{
		var allies = new[]
		{
			Combatant(1, health: 1f, position: new Vector3(0f, 0f, 4f)),
			Combatant(2, health: 1f, position: new Vector3(0f, 0f, 7f)),
			Combatant(3, health: 0f, position: new Vector3(0f, 0f, 3f)),
		};

		var count = PvPCombatantQueries.CountAlliesNear(allies, Vector3.Zero, radius: 5f);

		AssertEqual(1, count, "only living allies inside radius should count");
	}

	static void PvPCombatantQueriesMeasureNearestHostileDistance()
	{
		var hostiles = new[]
		{
			Combatant(10, health: 1f, position: new Vector3(0f, 0f, 8f), hitboxRadius: 1f),
			Combatant(11, health: 1f, position: new Vector3(0f, 0f, 4f), hitboxRadius: 0.5f),
			Combatant(12, health: 0f, position: new Vector3(0f, 0f, 1f), hitboxRadius: 0f),
		};

		var distance = PvPCombatantQueries.DistanceToNearestHostile(hostiles, Vector3.Zero);

		AssertEqual(3.5f, distance, "nearest hostile distance should ignore dead combatants and subtract hostile hitbox radius");
	}

	static void PvPCombatantQueriesCountHostilesInLine()
	{
		var hostiles = new[]
		{
			Combatant(10, health: 1f, position: new Vector3(0f, 0f, 5f), hitboxRadius: 0.5f),
			Combatant(11, health: 1f, position: new Vector3(4f, 0f, 5f), hitboxRadius: 0.5f),
			Combatant(12, health: 0f, position: new Vector3(0f, 0f, 4f), hitboxRadius: 0.5f),
		};

		var count = PvPCombatantQueries.CountHostilesInLine(
			hostiles,
			origin: Vector3.Zero,
			targetPosition: new Vector3(0f, 2f, 10f),
			range: 25f,
			halfWidth: 1f);

		AssertEqual(1, count, "line query should use XZ projection and ignore dead combatants");
	}

	static void PvPCombatantQueriesLineCountRejectsZeroLengthDirection()
	{
		var hostiles = new[]
		{
			Combatant(10, health: 1f, position: new Vector3(0f, 0f, 1f), hitboxRadius: 0.5f),
		};

		var count = PvPCombatantQueries.CountHostilesInLine(
			hostiles,
			origin: Vector3.Zero,
			targetPosition: Vector3.Zero,
			range: 25f,
			halfWidth: 1f);

		AssertEqual(0, count, "line query should reject a zero-length direction");
	}

	static void PvPCombatantQueriesLineCountRejectsHostilesBeyondRange()
	{
		var hostiles = new[]
		{
			Combatant(10, health: 1f, position: new Vector3(0f, 0f, 26f), hitboxRadius: 1f),
		};

		var count = PvPCombatantQueries.CountHostilesInLine(
			hostiles,
			origin: Vector3.Zero,
			targetPosition: new Vector3(0f, 0f, 10f),
			range: 25f,
			halfWidth: 1f);

		AssertEqual(0, count, "line query should reject hostiles beyond the action range");
	}

	static void PvPCombatantQueriesLineCountIncludesHitboxBoundary()
	{
		var hostiles = new[]
		{
			Combatant(10, health: 1f, position: new Vector3(1.5f, 0f, 5f), hitboxRadius: 0.5f),
			Combatant(11, health: 1f, position: new Vector3(1.6f, 0f, 5f), hitboxRadius: 0.5f),
		};

		var count = PvPCombatantQueries.CountHostilesInLine(
			hostiles,
			origin: Vector3.Zero,
			targetPosition: new Vector3(0f, 0f, 10f),
			range: 25f,
			halfWidth: 1f);

		AssertEqual(1, count, "line query should include hostiles on the half width plus hitbox boundary");
	}

	static PvPCombatantSnapshot Combatant(
		ulong objectId,
		float health,
		ulong targetId = 0,
		Vector3 position = default,
		float hitboxRadius = 0f)
	{
		return new PvPCombatantSnapshot(
			ObjectId: objectId,
			HealthRatio: health,
			CurrentHp: health > 0f ? 1u : 0u,
			TargetObjectId: targetId,
			Position: position,
			HitboxRadius: hitboxRadius);
	}

	static ScoringWeights LegacyCasualWeights()
	{
		return new ScoringWeights(
			RoleWeight: 1.00,
			FinishWeight: 1.00,
			MitigationPenaltyWeight: 1.00,
			DistancePenaltyWeight: 0.10,
			StickyBonus: 0.05,
			CarrierWeight: 0.50,
			LBWeight: 1.00,
			IsolationWeight: 0.25,
			ThreatWeight: 0.40,
			MpPressureWeight: 0.0,
			ResiliencePenaltyWeight: 0.0,
			ObjectiveWeight: 0.0);
	}

	static ScoringWeights LegacyTunedCustomWeights()
	{
		return new ScoringWeights(
			RoleWeight: 1.25,
			FinishWeight: 1.40,
			MitigationPenaltyWeight: 1.30,
			DistancePenaltyWeight: 0.20,
			StickyBonus: 0.08,
			CarrierWeight: 0.75,
			LBWeight: 1.20,
			IsolationWeight: 0.35,
			ThreatWeight: 0.55,
			MpPressureWeight: 0.0,
			ResiliencePenaltyWeight: 0.0,
			ObjectiveWeight: 0.0);
	}

	static void MpPressureScoresLowAndMediumMp()
	{
		AssertEqual(1.0, PvPScoringFactors.ComputeMpPressure(2_000), "low MP should be highest pressure");
		AssertEqual(0.5, PvPScoringFactors.ComputeMpPressure(4_000), "medium MP should be partial pressure");
		AssertEqual(0.0, PvPScoringFactors.ComputeMpPressure(6_000), "high MP should not add pressure");
	}

	static void PvPScoringHealthPressureScalesMissingHealthByWeight()
	{
		AssertEqual(0.0, PvPScoringFactors.ComputeHealthPressure(1f, 4.0), "full health should produce no pressure");
		AssertEqual(4.0, PvPScoringFactors.ComputeHealthPressure(0f, 4.0), "empty health should produce the full weight");
		AssertEqual(3.0, PvPScoringFactors.ComputeHealthPressure(0.25f, 4.0), "missing health should scale linearly");
		AssertEqual(4.0, PvPScoringFactors.ComputeHealthPressure(-0.5f, 4.0), "below-zero ratios should clamp to empty");
		AssertEqual(0.0, PvPScoringFactors.ComputeHealthPressure(1.5f, 4.0), "above-one ratios should clamp to full");
	}

	static void ObjectivePressureScoresKnownObjectiveTarget()
	{
		var targetId = 42UL;
		var ids = new HashSet<ulong> { targetId };

		AssertEqual(1.0, PvPScoringFactors.ComputeObjectivePressure(targetId, ids), "objective target should score");
		AssertEqual(0.0, PvPScoringFactors.ComputeObjectivePressure(99UL, ids), "unlisted target should not score");
	}

	static void ResiliencePenaltyScoresBooleanSignal()
	{
		AssertEqual(1.0, PvPScoringFactors.ComputeResiliencePenalty(true), "resilience should score as a penalty");
		AssertEqual(0.0, PvPScoringFactors.ComputeResiliencePenalty(false), "no resilience should not penalize");
	}

	static void PvpDamageGateRejectsInvulnerability()
	{
		var decision = PvPDamageGate.Evaluate(new PvPDamageGateInput(
			Intent: PvPBurstIntent.Secure,
			EffectiveHpRatio: double.PositiveInfinity,
			ExpectedDamageRatio: 1.00,
			ActiveDamageReduction: 0.99,
			HasInvulnerability: true,
			HasPrioritySignal: true));

		AssertEqual(PvPBurstRecommendation.Hold, decision, "damage gate should never spend into active invulnerability");
	}

	static void PvpDamageGateAllowsMitigatedSecureKill()
	{
		var decision = PvPDamageGate.Evaluate(new PvPDamageGateInput(
			Intent: PvPBurstIntent.Secure,
			EffectiveHpRatio: 0.62,
			ExpectedDamageRatio: 0.67,
			ActiveDamageReduction: 0.25,
			HasInvulnerability: false,
			HasPrioritySignal: false));

		AssertEqual(PvPBurstRecommendation.Secure, decision, "damage gate should allow mitigation when expected damage still kills");
	}

	static void PvpFinalGuardGateBlocksStaleGuardedTarget()
	{
		var input = new PvPActionUseGuardInput(
			IsPvP: true,
			IsHostileAction: true,
			IgnoresGuard: false,
			TargetHasGuard: true,
			GuardWillExpireBeforeAction: false);

		AssertTrue(PvPActionUseGuard.ShouldBlock(input), "final action use should recheck Guard after target selection");
	}

	static void PvpFinalGuardGateAllowsGuardPiercingAction()
	{
		var input = new PvPActionUseGuardInput(
			IsPvP: true,
			IsHostileAction: true,
			IgnoresGuard: true,
			TargetHasGuard: true,
			GuardWillExpireBeforeAction: false);

		AssertFalse(PvPActionUseGuard.ShouldBlock(input), "final action use should not block actions that ignore Guard");
	}

	static void PvpFinalGuardGateAllowsExpiringGuard()
	{
		var input = new PvPActionUseGuardInput(
			IsPvP: true,
			IsHostileAction: true,
			IgnoresGuard: false,
			TargetHasGuard: true,
			GuardWillExpireBeforeAction: true);

		AssertFalse(PvPActionUseGuard.ShouldBlock(input), "final action use should allow targets whose Guard expires before resolution");
	}

	static void PvpFinalGuardGateAllowsNonhostileAction()
	{
		var input = new PvPActionUseGuardInput(
			IsPvP: true,
			IsHostileAction: false,
			IgnoresGuard: false,
			TargetHasGuard: true,
			GuardWillExpireBeforeAction: false);

		AssertFalse(PvPActionUseGuard.ShouldBlock(input), "final action use should not block self or friendly actions because the target has Guard");
	}

	static void PvpGuardCooldownTrackerBackdatesObservedGuard()
	{
		var tracker = new PvPGuardCooldownTracker();

		tracker.Observe(new PvPGuardCooldownObservation(
			TargetId: 10,
			ObservedAt: TimeSpan.FromSeconds(10),
			HasGuard: true,
			GuardRemaining: TimeSpan.FromSeconds(2.5)));

		AssertEqual(
			PvPGuardAvailability.CoolingDown,
			tracker.GetAvailability(10, TimeSpan.FromSeconds(38), TimeSpan.Zero),
			"observed Guard should backdate use time from remaining duration");
		AssertEqual(
			PvPGuardAvailability.Ready,
			tracker.GetAvailability(10, TimeSpan.FromSeconds(38.6), TimeSpan.Zero),
			"Guard should become ready 30 seconds after inferred activation");
	}

	static void PvpGuardCooldownTrackerKeepsCooldownAfterEarlyCancel()
	{
		var tracker = new PvPGuardCooldownTracker();

		tracker.Observe(new PvPGuardCooldownObservation(
			TargetId: 10,
			ObservedAt: TimeSpan.Zero,
			HasGuard: true,
			GuardRemaining: TimeSpan.FromSeconds(4)));
		tracker.Observe(new PvPGuardCooldownObservation(
			TargetId: 10,
			ObservedAt: TimeSpan.FromSeconds(1),
			HasGuard: false,
			GuardRemaining: TimeSpan.Zero));

		AssertEqual(
			PvPGuardAvailability.CoolingDown,
			tracker.GetAvailability(10, TimeSpan.FromSeconds(10), TimeSpan.Zero),
			"canceling Guard early should not make Guard available before the recast finishes");
		AssertEqual(
			PvPGuardAvailability.Ready,
			tracker.GetAvailability(10, TimeSpan.FromSeconds(30.1), TimeSpan.Zero),
			"Guard should be ready after its recast from activation");
	}

	static void PvpGuardCooldownTrackerRequiresSafeUnavailableWindow()
	{
		var tracker = new PvPGuardCooldownTracker();

		tracker.Observe(new PvPGuardCooldownObservation(
			TargetId: 10,
			ObservedAt: TimeSpan.Zero,
			HasGuard: true,
			GuardRemaining: TimeSpan.FromSeconds(4)));

		AssertEqual(
			PvPGuardAvailability.CoolingDown,
			tracker.GetAvailability(10, TimeSpan.FromSeconds(28.5), TimeSpan.FromSeconds(1)),
			"Guard should count as unavailable when it remains down through the required commit window");
		AssertEqual(
			PvPGuardAvailability.Ready,
			tracker.GetAvailability(10, TimeSpan.FromSeconds(29.2), TimeSpan.FromSeconds(1)),
			"Guard should count as ready when it returns during the required commit window");
	}

	static void PvpGuardCooldownTrackerForgetsStaleUnseenTargets()
	{
		var tracker = new PvPGuardCooldownTracker();

		tracker.Observe(new PvPGuardCooldownObservation(
			TargetId: 10,
			ObservedAt: TimeSpan.Zero,
			HasGuard: true,
			GuardRemaining: TimeSpan.FromSeconds(4)));
		tracker.ForgetUnseen(TimeSpan.FromSeconds(8), new HashSet<ulong> { 20 }, TimeSpan.FromSeconds(5));

		AssertEqual(
			PvPGuardAvailability.Unknown,
			tracker.GetAvailability(10, TimeSpan.FromSeconds(8), TimeSpan.Zero),
			"stale unseen targets should become unknown because they may have used Guard out of sight");
	}

	static void PvpGuardCooldownTrackerForgetsTarget()
	{
		var tracker = new PvPGuardCooldownTracker();

		tracker.Observe(new PvPGuardCooldownObservation(
			TargetId: 10,
			ObservedAt: TimeSpan.Zero,
			HasGuard: true,
			GuardRemaining: TimeSpan.FromSeconds(4)));
		tracker.Forget(10);

		AssertEqual(
			PvPGuardAvailability.Unknown,
			tracker.GetAvailability(10, TimeSpan.FromSeconds(1), TimeSpan.Zero),
			"death or match reset should clear a target's inferred Guard cooldown");
	}
}
