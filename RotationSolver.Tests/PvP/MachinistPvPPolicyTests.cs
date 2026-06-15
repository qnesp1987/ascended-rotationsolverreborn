using RotationSolver.Basic.Actions.PvPTargetSelection;
using RotationSolver.RebornRotations.PVPRotations.Ranged;

namespace RotationSolver.Tests;

internal static partial class PvPTestSuite
{
	private const double MarksmanDefaultRecuperateRatio = 0.268;
	private const double MarksmanLowRecuperateRatio = 0.08;

	static void MachinistTargetPolicyPrefersKillableLowResourceTarget()
	{
		var highResourceTarget = MachinistTarget(1, healthRatio: 0.40f, currentMp: 10_000);
		var lowResourceTarget = MachinistTarget(2, healthRatio: 0.40f, currentMp: 2_000);

		var selected = MachinistPvPTargetPolicy.SelectBest(
			[highResourceTarget, lowResourceTarget],
			MachinistPvPActionIntent.MarksmanSpite);

		AssertEqual(2UL, selected?.TargetId, "MCH should prefer the target that cannot answer with repeated Recuperates");
	}

	static void MachinistTargetPolicyPrefersDirectSecureTarget()
	{
		var directSecureTarget = MachinistTarget(
			1,
			healthRatio: 0.15f,
			currentMp: 10_000,
			effectiveHealthRatio: 0.15,
			expectedDamageRatio: 0.20);
		var lowResourceTarget = MachinistTarget(2, healthRatio: 0.40f, currentMp: 2_000);

		var selected = MachinistPvPTargetPolicy.SelectBest(
			[directSecureTarget, lowResourceTarget],
			MachinistPvPActionIntent.AnalysisDrill);

		AssertEqual(1UL, selected?.TargetId, "MCH should prefer a direct secure target over a low MP pressure target");
	}

	static void MachinistTargetPolicyPrefersTeamFocusedNonLbTarget()
	{
		var oneFocusTarget = MachinistTarget(
			1,
			healthRatio: 0.40f,
			currentMp: 10_000,
			allyFocusCount: 1);
		var teamFocusTarget = MachinistTarget(
			2,
			healthRatio: 0.40f,
			currentMp: 10_000,
			allyFocusCount: 2);
		var noFocusTarget = MachinistTarget(
			3,
			healthRatio: 0.40f,
			currentMp: 10_000,
			allyFocusCount: 0);

		var oneFocusScore = MachinistPvPTargetPolicy.Score(oneFocusTarget, MachinistPvPActionIntent.BlazingShot);
		var teamFocusScore = MachinistPvPTargetPolicy.Score(teamFocusTarget, MachinistPvPActionIntent.BlazingShot);
		var noFocusScore = MachinistPvPTargetPolicy.Score(noFocusTarget, MachinistPvPActionIntent.BlazingShot);
		var selected = MachinistPvPTargetPolicy.SelectBest(
			[oneFocusTarget, teamFocusTarget, noFocusTarget],
			MachinistPvPActionIntent.BlazingShot);

		AssertTrue(oneFocusScore > noFocusScore, "MCH non-LB focus scoring should value a single ally focus over no focus");
		AssertTrue(teamFocusScore > oneFocusScore, "MCH non-LB focus scoring should value team focus over single ally focus");
		AssertEqual(2UL, selected?.TargetId, "MCH should prefer the enemy already focused by multiple allies for non-LB pressure");
	}

	static void MachinistTargetPolicyKeepsDirectSecureAboveTeamFocus()
	{
		var directSecureTarget = MachinistTarget(
			1,
			healthRatio: 0.15f,
			currentMp: 10_000,
			effectiveHealthRatio: 0.15,
			expectedDamageRatio: 0.20);
		var teamFocusTarget = MachinistTarget(
			2,
			healthRatio: 0.40f,
			currentMp: 10_000,
			allyFocusCount: 3);

		var selected = MachinistPvPTargetPolicy.SelectBest(
			[directSecureTarget, teamFocusTarget],
			MachinistPvPActionIntent.AnalysisDrill);

		AssertEqual(1UL, selected?.TargetId, "MCH should keep direct secure pressure ahead of focus-only team pressure");
	}

	static void MachinistTargetPolicyDerivesAllyFocusFromCount()
	{
		var noFocusTarget = MachinistTarget(
			1,
			healthRatio: 0.40f,
			currentMp: 10_000);
		var teamFocusTarget = MachinistTarget(
			2,
			healthRatio: 0.40f,
			currentMp: 10_000,
			allyFocusCount: 2);

		var noFocusScore = MachinistPvPTargetPolicy.Score(noFocusTarget, MachinistPvPActionIntent.BlazingShot);
		var teamFocusScore = MachinistPvPTargetPolicy.Score(teamFocusTarget, MachinistPvPActionIntent.BlazingShot);

		AssertTrue(teamFocusScore > noFocusScore, "MCH target focus should be derived from ally focus count");
	}

	static void MachinistTargetPolicyDoesNotBoostMarksmanTeamFocus()
	{
		var noFocusTarget = MachinistTarget(
			1,
			healthRatio: 0.40f,
			currentMp: 10_000,
			allyFocusCount: 0);
		var oneFocusTarget = MachinistTarget(
			2,
			healthRatio: 0.40f,
			currentMp: 10_000,
			allyFocusCount: 1);
		var teamFocusTarget = MachinistTarget(
			3,
			healthRatio: 0.40f,
			currentMp: 10_000,
			allyFocusCount: 2);

		var noFocusScore = MachinistPvPTargetPolicy.Score(noFocusTarget, MachinistPvPActionIntent.MarksmanSpite);
		var oneFocusScore = MachinistPvPTargetPolicy.Score(oneFocusTarget, MachinistPvPActionIntent.MarksmanSpite);
		var teamFocusScore = MachinistPvPTargetPolicy.Score(teamFocusTarget, MachinistPvPActionIntent.MarksmanSpite);

		AssertTrue(oneFocusScore > noFocusScore, "Marksman's Spite should keep existing single ally focus value");
		AssertEqual(oneFocusScore, teamFocusScore, "Marksman's Spite should not gain extra value from multiple ally focus");
	}

	static void MachinistTargetPolicyDoesNotBoostEagleEyeTeamFocus()
	{
		var noFocusTarget = MachinistTarget(
			1,
			healthRatio: 0.40f,
			currentMp: 10_000,
			allyFocusCount: 0);
		var oneFocusTarget = MachinistTarget(
			2,
			healthRatio: 0.40f,
			currentMp: 10_000,
			allyFocusCount: 1);
		var teamFocusTarget = MachinistTarget(
			3,
			healthRatio: 0.40f,
			currentMp: 10_000,
			allyFocusCount: 2);

		var noFocusScore = MachinistPvPTargetPolicy.Score(noFocusTarget, MachinistPvPActionIntent.EagleEyeShot);
		var oneFocusScore = MachinistPvPTargetPolicy.Score(oneFocusTarget, MachinistPvPActionIntent.EagleEyeShot);
		var teamFocusScore = MachinistPvPTargetPolicy.Score(teamFocusTarget, MachinistPvPActionIntent.EagleEyeShot);

		AssertTrue(oneFocusScore > noFocusScore, "Eagle Eye Shot should keep existing single ally focus value");
		AssertEqual(oneFocusScore, teamFocusScore, "Eagle Eye Shot should not gain extra value from multiple ally focus");
	}

	static void MachinistTargetPolicyAllowsGuardedDrillPunish()
	{
		var guardedLowTarget = MachinistTarget(
			1,
			healthRatio: 0.25f,
			currentMp: 2_000,
			hasGuard: true);
		var exposedHealthyTarget = MachinistTarget(2, healthRatio: 0.70f, currentMp: 2_000);

		var selected = MachinistPvPTargetPolicy.SelectBest(
			[guardedLowTarget, exposedHealthyTarget],
			MachinistPvPActionIntent.AnalysisDrill);

		AssertEqual(1UL, selected?.TargetId, "Analysis Drill should be allowed to punish low HP Guard");
	}

	static void MachinistAnalysisDrillRejectsFullResourceTarget()
	{
		var input = new MachinistPvPDecisionInput(
			Target: MachinistTarget(1, healthRatio: 0.90f, currentMp: 10_000),
			SafeCloseRange: true,
			FollowUpAvailable: false,
			AlliesCanBurst: false,
			ObjectiveControlNeeded: false,
			TargetCommitted: false);

		AssertFalse(MachinistPvPDecisionPolicy.ShouldUseAnalysisDrill(input), "Analysis Drill should not pad into full-resource targets");
	}

	static void MachinistAnalysisDrillAcceptsDirectSecureKill()
	{
		var input = new MachinistPvPDecisionInput(
			Target: MachinistTarget(
				1,
				healthRatio: 0.18f,
				currentMp: 10_000,
				effectiveHealthRatio: 0.18,
				expectedDamageRatio: 0.25),
			SafeCloseRange: true,
			FollowUpAvailable: false,
			AlliesCanBurst: false,
			ObjectiveControlNeeded: false,
			TargetCommitted: false);

		AssertTrue(MachinistPvPDecisionPolicy.ShouldUseAnalysisDrill(input), "Analysis Drill should secure a lethal target even when MP is high");
	}

	static void MachinistAnalysisAirAnchorRejectsResilientTarget()
	{
		var input = new MachinistPvPDecisionInput(
			Target: MachinistTarget(1, healthRatio: 0.30f, currentMp: 2_000, hasResilience: true),
			SafeCloseRange: true,
			FollowUpAvailable: true,
			AlliesCanBurst: true,
			ObjectiveControlNeeded: true,
			TargetCommitted: true);

		AssertFalse(MachinistPvPDecisionPolicy.ShouldUseAnalysisAirAnchor(input), "Analysis Air Anchor should reject Resilience when stun value matters");
	}

	static void MachinistAnalysisAirAnchorAcceptsDirectSecureThroughResilience()
	{
		var input = new MachinistPvPDecisionInput(
			Target: MachinistTarget(
				1,
				healthRatio: 0.18f,
				currentMp: 10_000,
				hasResilience: true,
				effectiveHealthRatio: 0.18,
				expectedDamageRatio: 0.20),
			SafeCloseRange: true,
			FollowUpAvailable: false,
			AlliesCanBurst: false,
			ObjectiveControlNeeded: false,
			TargetCommitted: false);

		AssertTrue(MachinistPvPDecisionPolicy.ShouldUseAnalysisAirAnchor(input), "Analysis Air Anchor damage should secure through Resilience");
	}

	static void MachinistAnalysisAirAnchorRejectsIsolatedSetup()
	{
		var input = new MachinistPvPDecisionInput(
			Target: MachinistTarget(1, healthRatio: 0.90f, currentMp: 10_000),
			SafeCloseRange: true,
			FollowUpAvailable: true,
			AlliesCanBurst: false,
			ObjectiveControlNeeded: false,
			TargetCommitted: true);

		AssertFalse(MachinistPvPDecisionPolicy.ShouldUseAnalysisAirAnchor(input), "Analysis Air Anchor should not spend stun on an isolated durable target");
	}

	static void MachinistAnalysisChainSawRequiresFollowUp()
	{
		var withoutFollowUp = new MachinistPvPDecisionInput(
			Target: MachinistTarget(1, healthRatio: 0.70f, currentMp: 10_000),
			SafeCloseRange: true,
			FollowUpAvailable: false,
			AlliesCanBurst: false,
			ObjectiveControlNeeded: false,
			TargetCommitted: true);
		var withFollowUp = withoutFollowUp with
		{
			FollowUpAvailable = true,
			AlliesCanBurst = true,
		};

		AssertFalse(MachinistPvPDecisionPolicy.ShouldUseAnalysisChainSaw(withoutFollowUp), "Analysis Chain Saw should not mark targets without follow-up");
		AssertTrue(MachinistPvPDecisionPolicy.ShouldUseAnalysisChainSaw(withFollowUp), "Analysis Chain Saw should set up burst when allies can hit");
	}

	static void MachinistScattergunRejectsUnsafeCloseRange()
	{
		var input = new MachinistPvPDecisionInput(
			Target: MachinistTarget(1, healthRatio: 0.25f, currentMp: 2_000, isInCloseRange: true),
			SafeCloseRange: false,
			FollowUpAvailable: true,
			AlliesCanBurst: true,
			ObjectiveControlNeeded: true,
			TargetCommitted: true);

		AssertFalse(MachinistPvPDecisionPolicy.ShouldUseScattergun(input), "Scattergun should reject unsafe 12y commits");
	}

	static void MachinistWildfireRequiresCommittedTargetAndFollowUp()
	{
		var looseTarget = new MachinistPvPDecisionInput(
			Target: MachinistTarget(1, healthRatio: 0.45f, currentMp: 2_000),
			SafeCloseRange: true,
			FollowUpAvailable: true,
			AlliesCanBurst: true,
			ObjectiveControlNeeded: false,
			TargetCommitted: false);
		var committedTarget = looseTarget with { TargetCommitted = true };

		AssertFalse(MachinistPvPDecisionPolicy.ShouldUseWildfire(looseTarget), "Wildfire should reject targets that can leave before detonation");
		AssertTrue(MachinistPvPDecisionPolicy.ShouldUseWildfire(committedTarget), "Wildfire should accept committed targets with follow-up");
	}

	static void MachinistBishopAcceptsObjectiveTeamfight()
	{
		var input = new MachinistPvPDecisionInput(
			Target: MachinistTarget(1, healthRatio: 0.80f, currentMp: 10_000, isObjectiveRelevant: true),
			SafeCloseRange: true,
			FollowUpAvailable: false,
			AlliesCanBurst: true,
			ObjectiveControlNeeded: true,
			TargetCommitted: true);

		AssertTrue(MachinistPvPDecisionPolicy.ShouldUseBishop(input), "Bishop should be used for objective teamfights");
	}

	static void MachinistBishopRejectsOutOfRangeTargets()
	{
		var inRange = MachinistTarget(1, healthRatio: 0.80f, currentMp: 10_000, isInNormalRange: true);
		var outOfRange = MachinistTarget(2, healthRatio: 0.10f, currentMp: 0, isInNormalRange: false);
		var rankedTargets = MachinistPvPTargetPolicy.Rank(
			[inRange, outOfRange],
			MachinistPvPActionIntent.Bishop);

		AssertEqual(1, rankedTargets.Count, "Bishop targeting should keep only reachable targets");
		AssertEqual(1UL, rankedTargets[0].TargetId, "Bishop targeting should choose the reachable target");
	}

	static void MachinistTargetPolicyBreaksScoreTiesByHealthThenId()
	{
		// Health key: an exact score tie with different health. The 0.25-health target's
		// extra health pressure (HealthPressure(0.25) = 3.0) equals the 0.50-health target's
		// health pressure plus Exposed (2.0 + ExposedScore 1.0 = 3.0), so the totals tie.
		// The lower-health target carries the HIGHER id, proving health beats the id anchor.
		// MachinistTarget defaults isExposed: true, so the 0.25 target must override it to false.
		var higherHealthExposed = MachinistTarget(1, healthRatio: 0.50f, currentMp: 10_000);
		var lowerHealthNotExposed = MachinistTarget(2, healthRatio: 0.25f, currentMp: 10_000, isExposed: false);

		var byHealth = MachinistPvPTargetPolicy.Rank(
			[higherHealthExposed, lowerHealthNotExposed],
			MachinistPvPActionIntent.BlazingShot);

		AssertEqual(2UL, byHealth[0].TargetId, "MCH tie should prefer the lower-health target even when it has the higher id");
		AssertEqual(1UL, byHealth[1].TargetId, "MCH tie should rank the higher-health target second");

		// Id anchor: equal in every score-affecting field including health, so health ties and
		// the unique TargetId decides. Lower id ranks first. Pass them high-id-first to prove
		// the comparator reorders rather than echoing input order.
		var idTwo = MachinistTarget(2, healthRatio: 0.40f, currentMp: 10_000);
		var idOne = MachinistTarget(1, healthRatio: 0.40f, currentMp: 10_000);

		var byId = MachinistPvPTargetPolicy.Rank(
			[idTwo, idOne],
			MachinistPvPActionIntent.BlazingShot);

		AssertEqual(1UL, byId[0].TargetId, "MCH tie with equal health should rank the lower id first");
		AssertEqual(2UL, byId[1].TargetId, "MCH tie with equal health should rank the higher id second");
	}

	static void MachinistMarksmanSpiteRejectsGuard()
	{
		var input = new MachinistPvPDecisionInput(
			Target: MachinistTarget(1, healthRatio: 0.10f, currentMp: 0, hasGuard: true),
			SafeCloseRange: true,
			FollowUpAvailable: true,
			AlliesCanBurst: true,
			ObjectiveControlNeeded: true,
			TargetCommitted: true);

		AssertFalse(MachinistPvPDecisionPolicy.ShouldUseMarksmanSpite(input), "Marksman's Spite should not be modeled as Guard piercing");
	}

	static void MachinistMarksmanSpiteHoldsOnDyingAllyFocusedTarget()
	{
		var input = new MachinistPvPDecisionInput(
			Target: MachinistTarget(
				1,
				healthRatio: 0.14f,
				currentMp: 2_000,
				allyFocusCount: 1,
				effectiveHealthRatio: 0.14),
			ExpectedDamageRatio: 0.67,
			SafeCloseRange: true,
			FollowUpAvailable: true,
			AlliesCanBurst: true,
			ObjectiveControlNeeded: false,
			TargetCommitted: true);

		AssertFalse(MachinistPvPDecisionPolicy.ShouldUseMarksmanSpite(input), "Marksman's Spite should not spend LB on targets allies are already cleaning up");
	}

	static void MachinistMarksmanSpiteAcceptsSecureDamage()
	{
		var input = new MachinistPvPDecisionInput(
			Target: MachinistTarget(
				1,
				healthRatio: 0.55f,
				currentMp: 2_000,
				effectiveHealthRatio: 0.55),
			ExpectedDamageRatio: 0.67,
			SafeCloseRange: true,
			FollowUpAvailable: true,
			AlliesCanBurst: false,
			ObjectiveControlNeeded: false,
			TargetCommitted: true);

		AssertTrue(MachinistPvPDecisionPolicy.ShouldUseMarksmanSpite(input), "Marksman's Spite should convert targets inside secure damage range");
	}

	static void MachinistMarksmanSpiteRejectsGuardReadySoloExecuteTarget()
	{
		var input = new MachinistPvPDecisionInput(
			Target: MachinistTarget(
				1,
				healthRatio: 0.55f,
				currentMp: 2_000,
				effectiveHealthRatio: 0.55,
				expectedDamageRatio: 0.67,
				guardAvailability: PvPGuardAvailability.Ready),
			ExpectedDamageRatio: 0.67,
			SafeCloseRange: true,
			FollowUpAvailable: false,
			AlliesCanBurst: false,
			ObjectiveControlNeeded: false,
			TargetCommitted: true,
			HasGuardCooldownKnowledge: true);

		AssertFalse(MachinistPvPDecisionPolicy.ShouldUseMarksmanSpite(input), "Marksman's Spite should not spend on a solo execute target who can Guard on reaction");
	}

	static void MachinistMarksmanSpiteRejectsUnknownGuardSoloExecuteTarget()
	{
		var input = new MachinistPvPDecisionInput(
			Target: MachinistTarget(
				1,
				healthRatio: 0.55f,
				currentMp: 2_000,
				effectiveHealthRatio: 0.55,
				expectedDamageRatio: 0.67,
				guardAvailability: PvPGuardAvailability.Unknown),
			ExpectedDamageRatio: 0.67,
			ExpectedRecuperateRatio: MarksmanDefaultRecuperateRatio,
			SafeCloseRange: true,
			FollowUpAvailable: false,
			AlliesCanBurst: false,
			ObjectiveControlNeeded: false,
			TargetCommitted: true,
			HasGuardCooldownKnowledge: true);

		AssertFalse(MachinistPvPDecisionPolicy.ShouldUseMarksmanSpite(input), "Marksman's Spite should not spend on a solo execute target with unknown Guard availability");
	}

	static void MachinistMarksmanSpiteRejectsLowMpNonlethalTarget()
	{
		var input = new MachinistPvPDecisionInput(
			Target: MachinistTarget(
				1,
				healthRatio: 0.68f,
				currentMp: 2_000,
				effectiveHealthRatio: 0.68,
				expectedDamageRatio: 0.67),
			ExpectedDamageRatio: 0.67,
			SafeCloseRange: true,
			FollowUpAvailable: true,
			AlliesCanBurst: false,
			ObjectiveControlNeeded: false,
			TargetCommitted: true);

		AssertFalse(MachinistPvPDecisionPolicy.ShouldUseMarksmanSpite(input), "Marksman's Spite should not fire when low MP is the only nonlethal signal");
	}

	static void MachinistMarksmanSpiteAcceptsAllyBackedNonlethalTarget()
	{
		var input = new MachinistPvPDecisionInput(
			Target: MachinistTarget(
				1,
				healthRatio: 0.68f,
				currentMp: 2_000,
				allyFocusCount: 1,
				effectiveHealthRatio: 0.68,
				expectedDamageRatio: 0.67,
				guardAvailability: PvPGuardAvailability.CoolingDown),
			ExpectedDamageRatio: 0.67,
			SafeCloseRange: true,
			FollowUpAvailable: true,
			AlliesCanBurst: true,
			ObjectiveControlNeeded: false,
			TargetCommitted: true,
			HasGuardCooldownKnowledge: true);

		AssertTrue(MachinistPvPDecisionPolicy.ShouldUseMarksmanSpite(input), "Marksman's Spite should fire when allies can convert the leftover health");
	}

	static void MachinistMarksmanSpiteAcceptsFocusedAlliedBurstNonlethalTarget()
	{
		var input = new MachinistPvPDecisionInput(
			Target: MachinistTarget(
				1,
				healthRatio: 0.68f,
				currentMp: 2_000,
				allyFocusCount: 1,
				effectiveHealthRatio: 0.68,
				expectedDamageRatio: 0.67,
				guardAvailability: PvPGuardAvailability.CoolingDown),
			ExpectedDamageRatio: 0.67,
			SafeCloseRange: true,
			FollowUpAvailable: false,
			AlliesCanBurst: true,
			ObjectiveControlNeeded: false,
			TargetCommitted: true,
			HasGuardCooldownKnowledge: true);

		AssertTrue(MachinistPvPDecisionPolicy.ShouldUseMarksmanSpite(input), "Marksman's Spite should fire when allied burst can convert the leftover health");
	}

	static void MachinistMarksmanSpiteAcceptsObjectiveBackedNonlethalTarget()
	{
		var input = new MachinistPvPDecisionInput(
			Target: MachinistTarget(
				1,
				healthRatio: 0.68f,
				currentMp: 2_000,
				allyFocusCount: 1,
				effectiveHealthRatio: 0.68,
				expectedDamageRatio: 0.67,
				guardAvailability: PvPGuardAvailability.CoolingDown),
			ExpectedDamageRatio: 0.67,
			SafeCloseRange: true,
			FollowUpAvailable: false,
			AlliesCanBurst: true,
			ObjectiveControlNeeded: true,
			TargetCommitted: true,
			HasGuardCooldownKnowledge: true);

		AssertTrue(MachinistPvPDecisionPolicy.ShouldUseMarksmanSpite(input), "Marksman's Spite should fire when focused objective pressure can convert the leftover health");
	}

	static void MachinistMarksmanSpiteRejectsObjectivePressureWithoutFocus()
	{
		var input = new MachinistPvPDecisionInput(
			Target: MachinistTarget(
				1,
				healthRatio: 0.72f,
				currentMp: 2_000,
				effectiveHealthRatio: 0.72,
				expectedDamageRatio: 0.67),
			ExpectedDamageRatio: 0.67,
			SafeCloseRange: true,
			FollowUpAvailable: false,
			AlliesCanBurst: false,
			ObjectiveControlNeeded: true,
			TargetCommitted: true);

		AssertFalse(MachinistPvPDecisionPolicy.ShouldUseMarksmanSpite(input), "Marksman's Spite should not use LB as objective pressure when no one can convert the leftover health");
	}

	static void MachinistMarksmanSpiteRejectsUnfocusedAllyProximity()
	{
		var input = new MachinistPvPDecisionInput(
			Target: MachinistTarget(
				1,
				healthRatio: 0.68f,
				currentMp: 2_000,
				effectiveHealthRatio: 0.68,
				expectedDamageRatio: 0.67),
			ExpectedDamageRatio: 0.67,
			SafeCloseRange: true,
			FollowUpAvailable: false,
			AlliesCanBurst: true,
			ObjectiveControlNeeded: false,
			TargetCommitted: true);

		AssertFalse(MachinistPvPDecisionPolicy.ShouldUseMarksmanSpite(input), "Marksman's Spite should not treat nearby allies as focused conversion pressure");
	}

	static void MachinistMarksmanSpiteRejectsUnsupportedNarrowLethalTarget()
	{
		var input = new MachinistPvPDecisionInput(
			Target: MachinistTarget(
				1,
				healthRatio: 0.665f,
				currentMp: 2_000,
				effectiveHealthRatio: 0.665),
			ExpectedDamageRatio: 0.67,
			SafeCloseRange: true,
			FollowUpAvailable: false,
			AlliesCanBurst: false,
			ObjectiveControlNeeded: false,
			TargetCommitted: true);

		AssertFalse(MachinistPvPDecisionPolicy.ShouldUseMarksmanSpite(input), "Marksman's Spite should hold narrow solo lethal reads without conversion support");
	}

	static void MachinistMarksmanSpiteRejectsObjectiveConversionAboveLeftoverCap()
	{
		var input = new MachinistPvPDecisionInput(
			Target: MachinistTarget(
				1,
				healthRatio: 0.76f,
				currentMp: 2_000,
				effectiveHealthRatio: 0.76,
				expectedDamageRatio: 0.67),
			ExpectedDamageRatio: 0.67,
			SafeCloseRange: true,
			FollowUpAvailable: false,
			AlliesCanBurst: false,
			ObjectiveControlNeeded: true,
			TargetCommitted: true);

		AssertFalse(MachinistPvPDecisionPolicy.ShouldUseMarksmanSpite(input), "Marksman's Spite should not fire on objective pressure above the leftover cap");
	}

	static void MachinistMarksmanSpiteRejectsFocusedAllyConversionAboveLeftoverCap()
	{
		var input = new MachinistPvPDecisionInput(
			Target: MachinistTarget(
				1,
				healthRatio: 0.76f,
				currentMp: 2_000,
				allyFocusCount: 1,
				effectiveHealthRatio: 0.76,
				expectedDamageRatio: 0.67),
			ExpectedDamageRatio: 0.67,
			SafeCloseRange: true,
			FollowUpAvailable: true,
			AlliesCanBurst: true,
			ObjectiveControlNeeded: false,
			TargetCommitted: true);

		AssertFalse(MachinistPvPDecisionPolicy.ShouldUseMarksmanSpite(input), "Marksman's Spite should not fire on focused ally pressure above the leftover cap");
	}

	static void MachinistMarksmanSpiteRejectsFocusedPressureAboveTightCap()
	{
		var input = new MachinistPvPDecisionInput(
			Target: MachinistTarget(
				1,
				healthRatio: 0.72f,
				currentMp: 2_000,
				allyFocusCount: 1,
				effectiveHealthRatio: 0.72,
				expectedDamageRatio: 0.67),
			ExpectedDamageRatio: 0.67,
			SafeCloseRange: true,
			FollowUpAvailable: true,
			AlliesCanBurst: true,
			ObjectiveControlNeeded: false,
			TargetCommitted: true);

		AssertFalse(MachinistPvPDecisionPolicy.ShouldUseMarksmanSpite(input), "Marksman's Spite should not spend LB when focused pressure leaves too much health to clean up");
	}

	static void MachinistMarksmanSpiteAcceptsVulnerableTarget()
	{
		var input = new MachinistPvPDecisionInput(
			Target: MachinistTarget(
				1,
				healthRatio: 0.68f,
				currentMp: 10_000,
				allyFocusCount: 1,
				isVulnerable: true,
				effectiveHealthRatio: 0.68,
				expectedDamageRatio: 0.67,
				guardAvailability: PvPGuardAvailability.CoolingDown),
			ExpectedDamageRatio: 0.67,
			SafeCloseRange: true,
			FollowUpAvailable: false,
			AlliesCanBurst: true,
			ObjectiveControlNeeded: false,
			TargetCommitted: true,
			HasGuardCooldownKnowledge: true);

		AssertTrue(MachinistPvPDecisionPolicy.ShouldUseMarksmanSpite(input), "Marksman's Spite should accept vulnerable targets when focused pressure can convert the leftover health");
	}

	static void MachinistMarksmanSpiteRejectsVulnerablePressureTarget()
	{
		var input = new MachinistPvPDecisionInput(
			Target: MachinistTarget(1, healthRatio: 0.80f, currentMp: 10_000, isVulnerable: true),
			ExpectedDamageRatio: 0.67,
			SafeCloseRange: true,
			FollowUpAvailable: true,
			AlliesCanBurst: false,
			ObjectiveControlNeeded: false,
			TargetCommitted: true);

		AssertFalse(MachinistPvPDecisionPolicy.ShouldUseMarksmanSpite(input), "Marksman's Spite should not spend LB only to leave a vulnerable target low");
	}

	static void MachinistMarksmanSpiteRejectsUnsupportedVulnerableTarget()
	{
		var input = new MachinistPvPDecisionInput(
			Target: MachinistTarget(1, healthRatio: 0.80f, currentMp: 10_000, isVulnerable: true),
			ExpectedDamageRatio: 0.67,
			SafeCloseRange: true,
			FollowUpAvailable: false,
			AlliesCanBurst: false,
			ObjectiveControlNeeded: false,
			TargetCommitted: true);

		AssertFalse(MachinistPvPDecisionPolicy.ShouldUseMarksmanSpite(input), "Marksman's Spite should not fire on vulnerability without conversion support");
	}

	static void MachinistMarksmanSpiteRejectsActiveInvulnerability()
	{
		var input = new MachinistPvPDecisionInput(
			Target: MachinistTarget(1, healthRatio: 0.10f, currentMp: 0, hasInvulnerability: true),
			ExpectedDamageRatio: 1.00,
			SafeCloseRange: true,
			FollowUpAvailable: true,
			AlliesCanBurst: true,
			ObjectiveControlNeeded: true,
			TargetCommitted: true);

		AssertFalse(MachinistPvPDecisionPolicy.ShouldUseMarksmanSpite(input), "Marksman's Spite should not fire into active invulnerability");
	}

	static void MachinistMarksmanSpiteAcceptsMitigatedSecureKill()
	{
		var input = new MachinistPvPDecisionInput(
			Target: MachinistTarget(
				1,
				healthRatio: 0.45f,
				currentMp: 10_000,
				effectiveHealthRatio: 0.62,
				activeDamageReduction: 0.25),
			ExpectedDamageRatio: 0.67,
			SafeCloseRange: true,
			FollowUpAvailable: false,
			AlliesCanBurst: false,
			ObjectiveControlNeeded: false,
			TargetCommitted: false);

		AssertTrue(MachinistPvPDecisionPolicy.ShouldUseMarksmanSpite(input), "Marksman's Spite should fire through mitigation when expected damage still kills");
	}

	static void MachinistMarksmanSpiteRejectsConversionWithoutGuardCooldownKnowledge()
	{
		var input = new MachinistPvPDecisionInput(
			Target: MachinistTarget(
				1,
				healthRatio: 0.68f,
				currentMp: 2_000,
				allyFocusCount: 1,
				effectiveHealthRatio: 0.68,
				expectedDamageRatio: 0.67,
				guardAvailability: PvPGuardAvailability.Ready),
			ExpectedDamageRatio: 0.67,
			SafeCloseRange: true,
			FollowUpAvailable: true,
			AlliesCanBurst: true,
			ObjectiveControlNeeded: false,
			TargetCommitted: true);

		AssertFalse(MachinistPvPDecisionPolicy.ShouldUseMarksmanSpite(input), "Marksman's Spite should require Guard cooldown knowledge before spending on a focused conversion target");
	}

	static void MachinistMarksmanSpiteRejectsGuardReadyConversionTarget()
	{
		var input = new MachinistPvPDecisionInput(
			Target: MachinistTarget(
				1,
				healthRatio: 0.68f,
				currentMp: 2_000,
				allyFocusCount: 1,
				effectiveHealthRatio: 0.68,
				expectedDamageRatio: 0.67,
				guardAvailability: PvPGuardAvailability.Ready),
			ExpectedDamageRatio: 0.67,
			SafeCloseRange: true,
			FollowUpAvailable: true,
			AlliesCanBurst: true,
			ObjectiveControlNeeded: false,
			TargetCommitted: true,
			HasGuardCooldownKnowledge: true);

		AssertFalse(MachinistPvPDecisionPolicy.ShouldUseMarksmanSpite(input), "Marksman's Spite should not spend on a narrow conversion target who can Guard on reaction");
	}

	static void MachinistMarksmanSpiteRejectsUnknownGuardConversionTarget()
	{
		var input = new MachinistPvPDecisionInput(
			Target: MachinistTarget(
				1,
				healthRatio: 0.68f,
				currentMp: 2_000,
				allyFocusCount: 1,
				effectiveHealthRatio: 0.68,
				expectedDamageRatio: 0.67,
				guardAvailability: PvPGuardAvailability.Unknown),
			ExpectedDamageRatio: 0.67,
			SafeCloseRange: true,
			FollowUpAvailable: true,
			AlliesCanBurst: true,
			ObjectiveControlNeeded: false,
			TargetCommitted: true,
			HasGuardCooldownKnowledge: true);

		AssertFalse(MachinistPvPDecisionPolicy.ShouldUseMarksmanSpite(input), "Marksman's Spite should treat unknown Guard availability as too risky for nonlethal conversion");
	}

	static void MachinistMarksmanSpiteAcceptsGuardCooldownConversionTarget()
	{
		var input = new MachinistPvPDecisionInput(
			Target: MachinistTarget(
				1,
				healthRatio: 0.68f,
				currentMp: 2_000,
				allyFocusCount: 1,
				effectiveHealthRatio: 0.68,
				expectedDamageRatio: 0.67,
				guardAvailability: PvPGuardAvailability.CoolingDown),
			ExpectedDamageRatio: 0.67,
			SafeCloseRange: true,
			FollowUpAvailable: true,
			AlliesCanBurst: true,
			ObjectiveControlNeeded: false,
			TargetCommitted: true,
			HasGuardCooldownKnowledge: true);

		AssertTrue(MachinistPvPDecisionPolicy.ShouldUseMarksmanSpite(input), "Marksman's Spite should allow existing conversion gates when Guard is confirmed unavailable");
	}

	static void MachinistMarksmanSpiteRejectsFocusedFinisherInStrictMode()
	{
		var input = new MachinistPvPDecisionInput(
			Target: MachinistTarget(
				1,
				healthRatio: 0.68f,
				currentMp: 2_000,
				allyFocusCount: 1,
				effectiveHealthRatio: 0.68,
				expectedDamageRatio: 0.67,
				guardAvailability: PvPGuardAvailability.CoolingDown),
			ExpectedDamageRatio: 0.67,
			SafeCloseRange: true,
			FollowUpAvailable: true,
			AlliesCanBurst: true,
			ObjectiveControlNeeded: false,
			TargetCommitted: true,
			HasGuardCooldownKnowledge: true,
			StrictMarksmanExecuteOnly: true);

		AssertFalse(MachinistPvPDecisionPolicy.ShouldUseMarksmanSpite(input), "Strict Marksman's Spite mode should reject focused team finishers");
	}

	static void MachinistMarksmanSpiteAcceptsStrictExecuteOnGuardCooldown()
	{
		var input = new MachinistPvPDecisionInput(
			Target: MachinistTarget(
				1,
				healthRatio: 0.40f,
				currentMp: 2_000,
				effectiveHealthRatio: 0.40,
				expectedDamageRatio: 0.67,
				guardAvailability: PvPGuardAvailability.CoolingDown),
			ExpectedDamageRatio: 0.67,
			SafeCloseRange: true,
			FollowUpAvailable: false,
			AlliesCanBurst: false,
			ObjectiveControlNeeded: false,
			TargetCommitted: true,
			HasGuardCooldownKnowledge: true,
			StrictMarksmanExecuteOnly: true);

		AssertTrue(MachinistPvPDecisionPolicy.ShouldUseMarksmanSpite(input), "Strict Marksman's Spite mode should still accept a clear execute when Guard is cooling down");
	}

	static void MachinistMarksmanSpiteRejectsStrictCcUnknownGuard()
	{
		var input = new MachinistPvPDecisionInput(
			Target: MachinistTarget(
				1,
				healthRatio: 0.30f,
				currentMp: PvPScoringFactors.LowMp,
				effectiveHealthRatio: 0.30,
				expectedDamageRatio: 0.67,
				guardAvailability: PvPGuardAvailability.Unknown),
			ExpectedDamageRatio: 0.67,
			ExpectedRecuperateRatio: MarksmanDefaultRecuperateRatio,
			SafeCloseRange: true,
			FollowUpAvailable: false,
			AlliesCanBurst: false,
			ObjectiveControlNeeded: false,
			TargetCommitted: true,
			HasGuardCooldownKnowledge: true,
			StrictMarksmanExecuteOnly: true);

		AssertFalse(MachinistPvPDecisionPolicy.ShouldUseMarksmanSpite(input), "Strict Marksman's Spite should require confirmed Guard cooldown when cooldown knowledge is reliable");
	}

	static void MachinistMarksmanSpiteRejectsStrictUnknownGuardRecuperateSurvivor()
	{
		var input = new MachinistPvPDecisionInput(
			Target: MachinistTarget(
				1,
				healthRatio: 0.55f,
				currentMp: PvPScoringFactors.LowMp,
				effectiveHealthRatio: 0.55,
				expectedDamageRatio: 0.67,
				guardAvailability: PvPGuardAvailability.Unknown),
			ExpectedDamageRatio: 0.67,
			ExpectedRecuperateRatio: MarksmanDefaultRecuperateRatio,
			SafeCloseRange: true,
			FollowUpAvailable: false,
			AlliesCanBurst: false,
			ObjectiveControlNeeded: false,
			TargetCommitted: true,
			HasGuardCooldownKnowledge: false,
			StrictMarksmanExecuteOnly: true);

		AssertFalse(MachinistPvPDecisionPolicy.ShouldUseMarksmanSpite(input), "Strict Marksman's Spite should not fire when one Recuperate lets an unknown Guard target survive");
	}

	static void MachinistMarksmanSpiteUsesCallerRecuperateRatio()
	{
		var input = new MachinistPvPDecisionInput(
			Target: MachinistTarget(
				1,
				healthRatio: 0.55f,
				currentMp: PvPScoringFactors.LowMp,
				effectiveHealthRatio: 0.55,
				expectedDamageRatio: 0.67,
				guardAvailability: PvPGuardAvailability.Unknown),
			ExpectedDamageRatio: 0.67,
			ExpectedRecuperateRatio: MarksmanLowRecuperateRatio,
			SafeCloseRange: true,
			FollowUpAvailable: false,
			AlliesCanBurst: false,
			ObjectiveControlNeeded: false,
			TargetCommitted: true,
			HasGuardCooldownKnowledge: false,
			StrictMarksmanExecuteOnly: true);

		AssertTrue(MachinistPvPDecisionPolicy.ShouldUseMarksmanSpite(input), "Strict Marksman's Spite should trust the caller-supplied Recuperate health ratio");
	}

	static void MachinistMarksmanSpiteAcceptsStrictUnknownGuardVeryLowHealth()
	{
		var input = new MachinistPvPDecisionInput(
			Target: MachinistTarget(
				1,
				healthRatio: 0.35f,
				currentMp: 10_000,
				effectiveHealthRatio: 0.35,
				expectedDamageRatio: 0.67,
				guardAvailability: PvPGuardAvailability.Unknown),
			ExpectedDamageRatio: 0.67,
			ExpectedRecuperateRatio: MarksmanDefaultRecuperateRatio,
			SafeCloseRange: true,
			FollowUpAvailable: false,
			AlliesCanBurst: false,
			ObjectiveControlNeeded: false,
			TargetCommitted: true,
			HasGuardCooldownKnowledge: false,
			StrictMarksmanExecuteOnly: true);

		AssertTrue(MachinistPvPDecisionPolicy.ShouldUseMarksmanSpite(input), "Strict Marksman's Spite should accept unknown Guard only when very low HP still dies through one Recuperate");
	}

	static void MachinistMarksmanSpiteAcceptsStrictUnknownGuardVeryLowMp()
	{
		var input = new MachinistPvPDecisionInput(
			Target: MachinistTarget(
				1,
				healthRatio: 0.37f,
				currentMp: PvPScoringFactors.LowMp,
				effectiveHealthRatio: 0.37,
				expectedDamageRatio: 0.67,
				guardAvailability: PvPGuardAvailability.Unknown),
			ExpectedDamageRatio: 0.67,
			ExpectedRecuperateRatio: MarksmanDefaultRecuperateRatio,
			SafeCloseRange: true,
			FollowUpAvailable: false,
			AlliesCanBurst: false,
			ObjectiveControlNeeded: false,
			TargetCommitted: true,
			HasGuardCooldownKnowledge: false,
			StrictMarksmanExecuteOnly: true);

		AssertTrue(MachinistPvPDecisionPolicy.ShouldUseMarksmanSpite(input), "Strict Marksman's Spite should accept unknown Guard when low MP still dies through one Recuperate");
	}

	static void MachinistMarksmanSpiteRejectsStrictUnknownGuardWithoutLowSignal()
	{
		var input = new MachinistPvPDecisionInput(
			Target: MachinistTarget(
				1,
				healthRatio: 0.37f,
				currentMp: 10_000,
				effectiveHealthRatio: 0.37,
				expectedDamageRatio: 0.67,
				guardAvailability: PvPGuardAvailability.Unknown),
			ExpectedDamageRatio: 0.67,
			ExpectedRecuperateRatio: MarksmanDefaultRecuperateRatio,
			SafeCloseRange: true,
			FollowUpAvailable: false,
			AlliesCanBurst: false,
			ObjectiveControlNeeded: false,
			TargetCommitted: true,
			HasGuardCooldownKnowledge: false,
			StrictMarksmanExecuteOnly: true);

		AssertFalse(MachinistPvPDecisionPolicy.ShouldUseMarksmanSpite(input), "Strict Marksman's Spite should require very low HP or low MP before accepting unknown Guard");
	}

	static void MachinistMarksmanSpiteRejectsStrictUnknownGuardMitigatedRecuperateSurvivor()
	{
		var input = new MachinistPvPDecisionInput(
			Target: MachinistTarget(
				1,
				healthRatio: 0.30f,
				currentMp: PvPScoringFactors.LowMp,
				effectiveHealthRatio: 0.40,
				activeDamageReduction: 0.25,
				expectedDamageRatio: 0.67,
				guardAvailability: PvPGuardAvailability.Unknown),
			ExpectedDamageRatio: 0.67,
			ExpectedRecuperateRatio: MarksmanDefaultRecuperateRatio,
			SafeCloseRange: true,
			FollowUpAvailable: false,
			AlliesCanBurst: false,
			ObjectiveControlNeeded: false,
			TargetCommitted: true,
			HasGuardCooldownKnowledge: false,
			StrictMarksmanExecuteOnly: true);

		AssertFalse(MachinistPvPDecisionPolicy.ShouldUseMarksmanSpite(input), "Strict Marksman's Spite should reject unknown Guard when mitigation makes one Recuperate survivable");
	}

	static void MachinistMarksmanSpiteRejectsStrictUnknownGuardMitigatedLowHealthSurvivor()
	{
		var input = new MachinistPvPDecisionInput(
			Target: MachinistTarget(
				1,
				healthRatio: 0.25f,
				currentMp: PvPScoringFactors.LowMp,
				effectiveHealthRatio: 0.3333333333333333,
				activeDamageReduction: 0.25,
				expectedDamageRatio: 0.67,
				guardAvailability: PvPGuardAvailability.Unknown),
			ExpectedDamageRatio: 0.67,
			ExpectedRecuperateRatio: MarksmanDefaultRecuperateRatio,
			SafeCloseRange: true,
			FollowUpAvailable: false,
			AlliesCanBurst: false,
			ObjectiveControlNeeded: false,
			TargetCommitted: true,
			HasGuardCooldownKnowledge: false,
			StrictMarksmanExecuteOnly: true);

		AssertFalse(MachinistPvPDecisionPolicy.ShouldUseMarksmanSpite(input), "Strict Marksman's Spite should scale Recuperate through mitigation before accepting low HP unknown Guard");
	}

	static void MachinistMarksmanSpitePreservesNonStrictUnknownGuardExecute()
	{
		var input = new MachinistPvPDecisionInput(
			Target: MachinistTarget(
				1,
				healthRatio: 0.40f,
				currentMp: 10_000,
				effectiveHealthRatio: 0.40,
				expectedDamageRatio: 0.67,
				guardAvailability: PvPGuardAvailability.Unknown),
			ExpectedDamageRatio: 0.67,
			SafeCloseRange: true,
			FollowUpAvailable: false,
			AlliesCanBurst: false,
			ObjectiveControlNeeded: false,
			TargetCommitted: true,
			HasGuardCooldownKnowledge: false,
			StrictMarksmanExecuteOnly: false);

		AssertTrue(MachinistPvPDecisionPolicy.ShouldUseMarksmanSpite(input), "Non strict Marksman's Spite should preserve existing unknown Guard true execute behavior");
	}

	static void MachinistMarksmanSpiteRejectsUnknownGuardLethalEmergency()
	{
		var input = new MachinistPvPDecisionInput(
			Target: MachinistTarget(
				1,
				healthRatio: 0.40f,
				currentMp: 0,
				effectiveHealthRatio: 0.40,
				guardAvailability: PvPGuardAvailability.Unknown),
			ExpectedDamageRatio: 0.67,
			SafeCloseRange: true,
			FollowUpAvailable: true,
			AlliesCanBurst: true,
			ObjectiveControlNeeded: true,
			TargetCommitted: true,
			HasGuardCooldownKnowledge: true);

		AssertFalse(MachinistPvPDecisionPolicy.ShouldUseMarksmanSpite(input), "Marksman's Spite objective pressure should not override unknown Guard availability");
	}

	static void MachinistMarksmanSpiteIdentityRejectsAdjustedDrill()
	{
		const uint drillPvPActionId = 29405;
		const uint marksmanSpitePvPActionId = 29415;

		AssertFalse(
			MachinistPvPDecisionPolicy.IsDirectMarksmansSpiteAction(drillPvPActionId, marksmanSpitePvPActionId),
			"Marksman's Spite lookup must not accept Drill only because Drill adjusted into the LB action");
		AssertTrue(
			MachinistPvPDecisionPolicy.IsDirectMarksmansSpiteAction(marksmanSpitePvPActionId, marksmanSpitePvPActionId),
			"Marksman's Spite lookup should accept the direct PvP LB action");
	}

	static void MachinistMarksmanSpiteLiveGuardVetoBlocksInheritedPierce()
	{
		var activeGuard = new MachinistPvPLiveGuardInput(
			TargetHasGuard: true,
			GuardWillExpireBeforeAction: false);
		var expiringGuard = new MachinistPvPLiveGuardInput(
			TargetHasGuard: true,
			GuardWillExpireBeforeAction: true);

		AssertTrue(
			MachinistPvPDecisionPolicy.ShouldVetoMarksmanSpiteForLiveGuard(activeGuard),
			"Marksman's Spite should be vetoed by live Guard even if the selected action object inherited Guard piercing settings");
		AssertFalse(
			MachinistPvPDecisionPolicy.ShouldVetoMarksmanSpiteForLiveGuard(expiringGuard),
			"Marksman's Spite should not be vetoed when Guard expires before the LB resolves");
	}

	static void MachinistAnalysisChainSawAcceptsLowResourceKillWindow()
	{
		var input = new MachinistPvPDecisionInput(
			Target: MachinistTarget(1, healthRatio: 0.50f, currentMp: 2_000),
			SafeCloseRange: true,
			FollowUpAvailable: false,
			AlliesCanBurst: false,
			ObjectiveControlNeeded: false,
			TargetCommitted: true);

		AssertTrue(MachinistPvPDecisionPolicy.ShouldUseAnalysisChainSaw(input), "Analysis Chain Saw should mark low MP targets before they stabilize");
	}

	static void MachinistFullMetalRejectsUncommittedFollowUp()
	{
		var input = new MachinistPvPDecisionInput(
			Target: MachinistTarget(1, healthRatio: 0.90f, currentMp: 10_000),
			SafeCloseRange: true,
			FollowUpAvailable: true,
			AlliesCanBurst: false,
			ObjectiveControlNeeded: false,
			TargetCommitted: false);

		AssertFalse(MachinistPvPDecisionPolicy.ShouldUseFullMetalField(input), "Full Metal Field should not spend burst on an uncommitted durable target");
	}

	static void MachinistFullMetalAcceptsDirectSecureWithoutFollowUp()
	{
		var input = new MachinistPvPDecisionInput(
			Target: MachinistTarget(
				1,
				healthRatio: 0.20f,
				currentMp: 10_000,
				effectiveHealthRatio: 0.20,
				expectedDamageRatio: 0.25),
			SafeCloseRange: true,
			FollowUpAvailable: false,
			AlliesCanBurst: false,
			ObjectiveControlNeeded: false,
			TargetCommitted: false);

		AssertTrue(MachinistPvPDecisionPolicy.ShouldUseFullMetalField(input), "Full Metal Field should secure lethal targets without setup signals");
	}

	static void MachinistFullMetalRejectsGuardedDirectSecure()
	{
		var input = new MachinistPvPDecisionInput(
			Target: MachinistTarget(
				1,
				healthRatio: 0.20f,
				currentMp: 10_000,
				hasGuard: true,
				effectiveHealthRatio: 0.20,
				expectedDamageRatio: 0.25),
			SafeCloseRange: true,
			FollowUpAvailable: true,
			AlliesCanBurst: true,
			ObjectiveControlNeeded: true,
			TargetCommitted: true);

		AssertFalse(MachinistPvPDecisionPolicy.ShouldUseFullMetalField(input), "Full Metal Field should not treat Guard as killable");
	}

	static void MachinistFullMetalRejectsOutOfRangeDirectSecure()
	{
		var input = new MachinistPvPDecisionInput(
			Target: MachinistTarget(
				1,
				healthRatio: 0.20f,
				currentMp: 10_000,
				effectiveHealthRatio: 0.20,
				expectedDamageRatio: 0.25,
				isInNormalRange: false),
			SafeCloseRange: true,
			FollowUpAvailable: true,
			AlliesCanBurst: true,
			ObjectiveControlNeeded: true,
			TargetCommitted: true);

		AssertFalse(MachinistPvPDecisionPolicy.ShouldUseFullMetalField(input), "Full Metal Field should not secure targets outside action range");
	}

	static void MachinistBlazingShotAcceptsDirectSecureWithoutFollowUp()
	{
		var input = new MachinistPvPDecisionInput(
			Target: MachinistTarget(
				1,
				healthRatio: 0.12f,
				currentMp: 10_000,
				effectiveHealthRatio: 0.12,
				expectedDamageRatio: 0.15),
			SafeCloseRange: true,
			FollowUpAvailable: false,
			AlliesCanBurst: false,
			ObjectiveControlNeeded: false,
			TargetCommitted: false);

		AssertTrue(MachinistPvPDecisionPolicy.ShouldUseBlazingShot(input), "Blazing Shot should secure lethal targets without setup signals");
	}
}
