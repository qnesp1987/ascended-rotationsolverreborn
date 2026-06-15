using RotationSolver.Basic.Actions.PvPTargetSelection;
using RotationSolver.RebornRotations.PVPRotations.Ranged;

namespace RotationSolver.Tests;

internal static partial class PvPTestSuite
{
	static void SilentNocturneRejectsFillerUse()
	{
		var input = new BardPvPShutdownInput(
			TargetHasResilience: false,
			TargetIsCasting: false,
			TargetThreatensFragileAlly: false,
			TargetIsBurstWorthy: false,
			TargetHealthRatio: 1f,
			TargetDistance: 20f,
			SafeBackstepExists: true,
			ObjectiveControlNeeded: false);

		AssertFalse(BardPvPDecisionPolicy.ShouldUseSilentNocturne(input), "Nocturne should not be filler");
	}

	static void SilentNocturneAcceptsCastingShutdown()
	{
		var input = new BardPvPShutdownInput(
			TargetHasResilience: false,
			TargetIsCasting: true,
			TargetThreatensFragileAlly: false,
			TargetIsBurstWorthy: false,
			TargetHealthRatio: 1f,
			TargetDistance: 20f,
			SafeBackstepExists: true,
			ObjectiveControlNeeded: false);

		AssertTrue(BardPvPDecisionPolicy.ShouldUseSilentNocturne(input), "Nocturne should interrupt high-value casts");
	}

	static void SilentNocturneRejectsResilientTarget()
	{
		var input = new BardPvPShutdownInput(
			TargetHasResilience: true,
			TargetIsCasting: true,
			TargetThreatensFragileAlly: true,
			TargetIsBurstWorthy: true,
			TargetHealthRatio: 0.20f,
			TargetDistance: 8f,
			SafeBackstepExists: true,
			ObjectiveControlNeeded: true);

		AssertFalse(BardPvPDecisionPolicy.ShouldUseSilentNocturne(input), "Nocturne should reject Resilience");
	}

	static void SilentNocturneRejectsGuardedTarget()
	{
		var input = new BardPvPShutdownInput(
			TargetHasResilience: false,
			TargetIsCasting: true,
			TargetThreatensFragileAlly: true,
			TargetIsBurstWorthy: true,
			TargetHealthRatio: 0.20f,
			TargetDistance: 8f,
			SafeBackstepExists: true,
			ObjectiveControlNeeded: true,
			KillSecure: new BardPvPKillSecureFacts(
				EffectiveHpRatio: 0.20,
				ExpectedDamageRatio: 0.50,
				RecuperateRatio: 0.30,
				TargetCanRecuperate: true,
				HasGuard: true));

		AssertFalse(BardPvPDecisionPolicy.ShouldUseSilentNocturne(input),
			"Nocturne should not fire into Guard immunity");
	}

	static void SilentNocturneSecuresKillThroughRecuperate()
	{
		// Health 0.80 keeps the engaged-HP trigger OFF, so a true result proves the anti-heal path:
		// burst (0.90) clears base eHP (0.80, +0.05 margin) but a Recuperate (0.40) would reach 1.20 > 0.90.
		var input = new BardPvPShutdownInput(
			TargetHasResilience: false,
			TargetIsCasting: false,
			TargetThreatensFragileAlly: false,
			TargetIsBurstWorthy: false,
			TargetHealthRatio: 0.80f,
			TargetDistance: 20f,
			SafeBackstepExists: true,
			ObjectiveControlNeeded: false,
			KillSecure: new BardPvPKillSecureFacts(
				EffectiveHpRatio: 0.80,
				ExpectedDamageRatio: 0.90,
				RecuperateRatio: 0.40,
				TargetCanRecuperate: true,
				HasGuard: false));

		AssertTrue(BardPvPDecisionPolicy.ShouldUseSilentNocturne(input),
			"Nocturne should secure a kill a Recuperate would otherwise save");
	}

	static void SilentNocturneRejectsSecureWhenBurstKillsRegardless()
	{
		var input = new BardPvPShutdownInput(
			TargetHasResilience: false,
			TargetIsCasting: false,
			TargetThreatensFragileAlly: false,
			TargetIsBurstWorthy: false,
			TargetHealthRatio: 0.80f,
			TargetDistance: 20f,
			SafeBackstepExists: true,
			ObjectiveControlNeeded: false,
			KillSecure: new BardPvPKillSecureFacts(
				EffectiveHpRatio: 0.80,
				ExpectedDamageRatio: 1.20,
				RecuperateRatio: 0.30,
				TargetCanRecuperate: true,
				HasGuard: false));

		AssertFalse(BardPvPDecisionPolicy.ShouldUseSilentNocturne(input),
			"Nocturne should not spend on a kill that lands through a Recuperate anyway");
	}

	static void SilentNocturneRejectsSecureWhenBurstCannotKill()
	{
		var input = new BardPvPShutdownInput(
			TargetHasResilience: false,
			TargetIsCasting: false,
			TargetThreatensFragileAlly: false,
			TargetIsBurstWorthy: false,
			TargetHealthRatio: 0.80f,
			TargetDistance: 20f,
			SafeBackstepExists: true,
			ObjectiveControlNeeded: false,
			KillSecure: new BardPvPKillSecureFacts(
				EffectiveHpRatio: 0.80,
				ExpectedDamageRatio: 0.50,
				RecuperateRatio: 0.30,
				TargetCanRecuperate: true,
				HasGuard: false));

		AssertFalse(BardPvPDecisionPolicy.ShouldUseSilentNocturne(input),
			"Nocturne should not claim a secure the burst cannot reach");
	}

	static void SilentNocturneRejectsSecureWhenTargetCannotRecuperate()
	{
		var input = new BardPvPShutdownInput(
			TargetHasResilience: false,
			TargetIsCasting: false,
			TargetThreatensFragileAlly: false,
			TargetIsBurstWorthy: false,
			TargetHealthRatio: 0.80f,
			TargetDistance: 20f,
			SafeBackstepExists: true,
			ObjectiveControlNeeded: false,
			KillSecure: new BardPvPKillSecureFacts(
				EffectiveHpRatio: 0.40,
				ExpectedDamageRatio: 0.50,
				RecuperateRatio: 0.30,
				TargetCanRecuperate: false,
				HasGuard: false));

		AssertFalse(BardPvPDecisionPolicy.ShouldUseSilentNocturne(input),
			"Anti-heal secure requires the target to actually have a Recuperate to deny");
	}

	static void SilentNocturneRejectsSecureWhenEffectiveHpDegenerate()
	{
		// A 0-HP / zero effective-HP target mid-frame must not produce a spurious kill-secure.
		var input = new BardPvPShutdownInput(
			TargetHasResilience: false,
			TargetIsCasting: false,
			TargetThreatensFragileAlly: false,
			TargetIsBurstWorthy: false,
			TargetHealthRatio: 0.80f,
			TargetDistance: 20f,
			SafeBackstepExists: true,
			ObjectiveControlNeeded: false,
			KillSecure: new BardPvPKillSecureFacts(
				EffectiveHpRatio: 0.0,
				ExpectedDamageRatio: 0.50,
				RecuperateRatio: 0.30,
				TargetCanRecuperate: true,
				HasGuard: false));

		AssertFalse(BardPvPDecisionPolicy.ShouldUseSilentNocturne(input),
			"Degenerate zero effective HP must not produce a spurious kill-secure");
	}

	static void SilentNocturneRejectsSecureWhenTargetInvulnerable()
	{
		// Invulnerable target: effective HP is infinite, so no burst can secure. Health 0.80 keeps the
		// engaged-HP trigger off, isolating the anti-heal path's infinite-eHP rejection.
		var input = new BardPvPShutdownInput(
			TargetHasResilience: false,
			TargetIsCasting: false,
			TargetThreatensFragileAlly: false,
			TargetIsBurstWorthy: false,
			TargetHealthRatio: 0.80f,
			TargetDistance: 20f,
			SafeBackstepExists: true,
			ObjectiveControlNeeded: false,
			KillSecure: new BardPvPKillSecureFacts(
				EffectiveHpRatio: double.PositiveInfinity,
				ExpectedDamageRatio: 0.50,
				RecuperateRatio: 0.30,
				TargetCanRecuperate: true,
				HasGuard: false));

		AssertFalse(BardPvPDecisionPolicy.ShouldUseSilentNocturne(input),
			"Anti-heal secure must not fire against an invulnerable target");
	}

	static void SilentNocturneFiresOnEngagedTarget()
	{
		var input = new BardPvPShutdownInput(
			TargetHasResilience: false,
			TargetIsCasting: false,
			TargetThreatensFragileAlly: false,
			TargetIsBurstWorthy: false,
			TargetHealthRatio: 0.65f,
			TargetDistance: 20f,
			SafeBackstepExists: true,
			ObjectiveControlNeeded: false,
			KillSecure: default);

		AssertTrue(BardPvPDecisionPolicy.ShouldUseSilentNocturne(input),
			"Nocturne should be used on an engaged target rather than hoarded");
	}

	static void RepellingRejectsUnsafeBackstep()
	{
		var input = new BardPvPShutdownInput(
			TargetHasResilience: false,
			TargetIsCasting: false,
			TargetThreatensFragileAlly: true,
			TargetIsBurstWorthy: false,
			TargetHealthRatio: 1f,
			TargetDistance: 8f,
			SafeBackstepExists: false,
			ObjectiveControlNeeded: false);

		AssertFalse(BardPvPDecisionPolicy.ShouldUseRepellingShot(input), "Repelling should reject unsafe backsteps");
	}

	static void RepellingRejectsResilientTarget()
	{
		var input = new BardPvPShutdownInput(
			TargetHasResilience: true,
			TargetIsCasting: false,
			TargetThreatensFragileAlly: true,
			TargetIsBurstWorthy: true,
			TargetHealthRatio: 0.20f,
			TargetDistance: 8f,
			SafeBackstepExists: true,
			ObjectiveControlNeeded: true);

		AssertFalse(BardPvPDecisionPolicy.ShouldUseRepellingShot(input), "Repelling should reject Resilience");
	}

	static void RepellingAcceptsSafePeel()
	{
		var input = new BardPvPShutdownInput(
			TargetHasResilience: false,
			TargetIsCasting: false,
			TargetThreatensFragileAlly: true,
			TargetIsBurstWorthy: false,
			TargetHealthRatio: 1f,
			TargetDistance: 8f,
			SafeBackstepExists: true,
			ObjectiveControlNeeded: false);

		AssertTrue(BardPvPDecisionPolicy.ShouldUseRepellingShot(input), "Repelling should peel safe short-range divers");
	}

	static void BardForcedBurstRejectsBlockedTarget()
	{
		AssertFalse(
			BardPvPDecisionPolicy.ShouldUseBurstOrForcedSpend(
				targetIsBurstWorthy: false,
				targetBlocksDamage: true,
				forcedSpendWindow: true),
			"Bard forced burst should not override a blocked damage target");
	}

	static void BardBurstGateCannotOverrideBlockedTarget()
	{
		AssertFalse(
			BardPvPDecisionPolicy.ShouldUseBurstOrForcedSpend(
				targetIsBurstWorthy: true,
				targetBlocksDamage: true,
				forcedSpendWindow: true),
			"Bard burst should not fire when active mitigation blocks the damage");
	}

	static void BardForcedBurstAllowsUnblockedTarget()
	{
		AssertTrue(
			BardPvPDecisionPolicy.ShouldUseBurstOrForcedSpend(
				targetIsBurstWorthy: false,
				targetBlocksDamage: false,
				forcedSpendWindow: true),
			"Bard forced burst may prevent expiry or overcap when damage is not blocked");
	}

	static void BardApexArrowRejectsActiveBlastArrowWindow()
	{
		AssertFalse(
			BardPvPDecisionPolicy.ShouldUseApexArrow(true),
			"Apex Arrow must not overwrite an active Blast Arrow window");
	}

	static void BardApexArrowAllowsMissingBlastArrowWindow()
	{
		AssertTrue(
			BardPvPDecisionPolicy.ShouldUseApexArrow(false),
			"Apex Arrow should remain available when Blast Arrow is not ready");
	}

	static void ProtectivePaeanRejectsHealthyUnfocusedAlly()
	{
		AssertFalse(
			BardPvPDecisionPolicy.ShouldUseProtectivePaean(0.90f, 0),
			"healthy unfocused ally should not receive fake-shield Paean");
	}

	static void ProtectivePaeanAllowsFocusedAlly()
	{
		AssertTrue(
			BardPvPDecisionPolicy.ShouldUseProtectivePaean(0.60f, 1),
			"focused ally near pressure threshold should receive fake-shield Paean");
	}

	static void BardForcedBurstAllowsDirectSecureTarget()
	{
		AssertTrue(
			BardPvPDecisionPolicy.ShouldUseBurstOrForcedSpend(
				targetIsBurstWorthy: false,
				targetBlocksDamage: false,
				forcedSpendWindow: false,
				targetCanBeKilled: true),
			"Bard burst actions should spend when the current action can secure the target");
	}

	static void BardForcedBurstRejectsBlockedDirectSecureTarget()
	{
		AssertFalse(
			BardPvPDecisionPolicy.ShouldUseBurstOrForcedSpend(
				targetIsBurstWorthy: true,
				targetBlocksDamage: true,
				forcedSpendWindow: true,
				targetCanBeKilled: true),
			"Bard burst actions should not spend into blocked damage even when HP looks lethal");
	}

	static void BardKillSecureRanksLethalHostile()
	{
		var targets = new[]
		{
			BardKillTarget(1, healthRatio: 0.20f, effectiveHealthRatio: 0.20, expectedDamageRatio: 0.10),
			BardKillTarget(2, healthRatio: 0.12f, effectiveHealthRatio: 0.09, expectedDamageRatio: 0.10),
		};

		var ranked = BardPvPDecisionPolicy.RankDirectSecureTargets(targets);

		AssertEqual(1, ranked.Count, "only lethal Bard targets should be ranked");
		AssertEqual(2UL, ranked[0], "Bard should force target selection onto the lethal hostile");
	}

	static void BardKillSecureRejectsInvulnerability()
	{
		var targets = new[]
		{
			BardKillTarget(1, healthRatio: 0.05f, effectiveHealthRatio: 0.05, expectedDamageRatio: 0.10, hasInvulnerability: true),
		};

		var ranked = BardPvPDecisionPolicy.RankDirectSecureTargets(targets);

		AssertEqual(0, ranked.Count, "Bard kill secure must not target active invulnerability");
	}

	static void BardKillSecurePrefersLowestLethalHealth()
	{
		var targets = new[]
		{
			BardKillTarget(1, healthRatio: 0.18f, effectiveHealthRatio: 0.09, expectedDamageRatio: 0.10),
			BardKillTarget(2, healthRatio: 0.08f, effectiveHealthRatio: 0.07, expectedDamageRatio: 0.10),
		};

		var ranked = BardPvPDecisionPolicy.RankDirectSecureTargets(targets);

		AssertEqual(2, ranked.Count, "all lethal Bard targets should remain available");
		AssertEqual(2UL, ranked[0], "Bard should target the lowest health lethal hostile first");
	}

	static void BardOffensiveTargetPolicyPrefersDirectSecureTarget()
	{
		var directSecureTarget = BardOffensiveTarget(
			1,
			healthRatio: 0.15f,
			currentMp: 10_000,
			effectiveHealthRatio: 0.15,
			expectedDamageRatio: 0.20);
		var lowMpTarget = BardOffensiveTarget(2, healthRatio: 0.40f, currentMp: 2_000);

		var selected = BardPvPTargetPolicy.SelectBest(
			[directSecureTarget, lowMpTarget],
			BardPvPActionIntent.HarmonicArrow);

		AssertEqual(1UL, selected?.TargetId, "Bard should prefer a target that the current action can secure");
	}

	static void BardOffensiveTargetPolicyPrefersLowMpTarget()
	{
		var highMpTarget = BardOffensiveTarget(1, healthRatio: 0.40f, currentMp: 10_000);
		var lowMpTarget = BardOffensiveTarget(2, healthRatio: 0.40f, currentMp: 2_000);

		var selected = BardPvPTargetPolicy.SelectBest(
			[highMpTarget, lowMpTarget],
			BardPvPActionIntent.PowerfulShot);

		AssertEqual(2UL, selected?.TargetId, "Bard should prefer pressure on enemies with limited Recuperate resources");
	}

	static void BardOffensiveTargetPolicyPrefersTeamFocusedNonLbTarget()
	{
		var oneFocusTarget = BardOffensiveTarget(
			1,
			healthRatio: 0.40f,
			currentMp: 10_000,
			allyFocusCount: 1);
		var teamFocusTarget = BardOffensiveTarget(
			2,
			healthRatio: 0.40f,
			currentMp: 10_000,
			allyFocusCount: 2);
		var noFocusTarget = BardOffensiveTarget(
			3,
			healthRatio: 0.40f,
			currentMp: 10_000,
			allyFocusCount: 0);

		var oneFocusScore = BardPvPTargetPolicy.Score(oneFocusTarget, BardPvPActionIntent.PowerfulShot);
		var teamFocusScore = BardPvPTargetPolicy.Score(teamFocusTarget, BardPvPActionIntent.PowerfulShot);
		var noFocusScore = BardPvPTargetPolicy.Score(noFocusTarget, BardPvPActionIntent.PowerfulShot);
		var selected = BardPvPTargetPolicy.SelectBest(
			[oneFocusTarget, teamFocusTarget, noFocusTarget],
			BardPvPActionIntent.PowerfulShot);

		AssertTrue(oneFocusScore > noFocusScore, "Bard non-LB focus scoring should value a single ally focus over no focus");
		AssertTrue(teamFocusScore > oneFocusScore, "Bard non-LB focus scoring should value team focus over single ally focus");
		AssertEqual(2UL, selected?.TargetId, "Bard should prefer the enemy already focused by multiple allies for non-LB pressure");
	}

	static void BardOffensiveTargetPolicyKeepsDirectSecureAboveTeamFocus()
	{
		var directSecureTarget = BardOffensiveTarget(
			1,
			healthRatio: 0.15f,
			currentMp: 10_000,
			effectiveHealthRatio: 0.15,
			expectedDamageRatio: 0.20);
		var teamFocusTarget = BardOffensiveTarget(
			2,
			healthRatio: 0.40f,
			currentMp: 10_000,
			allyFocusCount: 3);

		var selected = BardPvPTargetPolicy.SelectBest(
			[directSecureTarget, teamFocusTarget],
			BardPvPActionIntent.HarmonicArrow);

		AssertEqual(1UL, selected?.TargetId, "Bard should keep direct secure pressure ahead of focus-only team pressure");
	}

	static void BardOffensiveTargetPolicyDerivesAllyFocusFromCount()
	{
		var noFocusTarget = BardOffensiveTarget(
			1,
			healthRatio: 0.40f,
			currentMp: 10_000);
		var teamFocusTarget = BardOffensiveTarget(
			2,
			healthRatio: 0.40f,
			currentMp: 10_000,
			allyFocusCount: 2);

		var noFocusScore = BardPvPTargetPolicy.Score(noFocusTarget, BardPvPActionIntent.PowerfulShot);
		var teamFocusScore = BardPvPTargetPolicy.Score(teamFocusTarget, BardPvPActionIntent.PowerfulShot);

		AssertTrue(teamFocusScore > noFocusScore, "Bard target focus should be derived from ally focus count");
	}

	static void BardOffensiveTargetPolicyDoesNotBoostEagleEyeTeamFocus()
	{
		var noFocusTarget = BardOffensiveTarget(
			1,
			healthRatio: 0.40f,
			currentMp: 10_000,
			allyFocusCount: 0);
		var oneFocusTarget = BardOffensiveTarget(
			2,
			healthRatio: 0.40f,
			currentMp: 10_000,
			allyFocusCount: 1);
		var teamFocusTarget = BardOffensiveTarget(
			3,
			healthRatio: 0.40f,
			currentMp: 10_000,
			allyFocusCount: 2);

		var noFocusScore = BardPvPTargetPolicy.Score(noFocusTarget, BardPvPActionIntent.EagleEyeShot);
		var oneFocusScore = BardPvPTargetPolicy.Score(oneFocusTarget, BardPvPActionIntent.EagleEyeShot);
		var teamFocusScore = BardPvPTargetPolicy.Score(teamFocusTarget, BardPvPActionIntent.EagleEyeShot);

		AssertTrue(oneFocusScore > noFocusScore, "Eagle Eye Shot should keep existing single ally focus value");
		AssertEqual(oneFocusScore, teamFocusScore, "Eagle Eye Shot should not gain extra value from multiple ally focus");
	}

	static void BardOffensiveTargetPolicyUsesPitchPerfectSplashValue()
	{
		var isolatedTarget = BardOffensiveTarget(1, healthRatio: 0.40f, currentMp: 10_000, splashTargetCount: 1);
		var splashTarget = BardOffensiveTarget(2, healthRatio: 0.40f, currentMp: 10_000, splashTargetCount: 3);

		var selected = BardPvPTargetPolicy.SelectBest(
			[isolatedTarget, splashTarget],
			BardPvPActionIntent.PitchPerfect);

		AssertEqual(2UL, selected?.TargetId, "Pitch Perfect should prefer targets that add splash value");
	}

	static void BardOffensiveTargetPolicyRejectsOutOfRangeTarget()
	{
		var inRangeTarget = BardOffensiveTarget(1, healthRatio: 0.80f, currentMp: 10_000, isInNormalRange: true);
		var outOfRangeTarget = BardOffensiveTarget(2, healthRatio: 0.10f, currentMp: 0, isInNormalRange: false);

		var rankedTargets = BardPvPTargetPolicy.Rank(
			[inRangeTarget, outOfRangeTarget],
			BardPvPActionIntent.HarmonicArrow);

		AssertEqual(1, rankedTargets.Count, "Bard offensive targeting should keep only reachable targets");
		AssertEqual(1UL, rankedTargets[0].TargetId, "Bard offensive targeting should choose the reachable target");
	}

	static void BardOffensiveTargetPolicyKeepsEagleEyeGuardTarget()
	{
		var guardedTarget = BardOffensiveTarget(
			1,
			healthRatio: 0.40f,
			currentMp: 10_000,
			hasGuard: true,
			expectedDamageRatio: 0.0);
		var exposedTarget = guardedTarget with { TargetId = 2, HasGuard = false };

		var guardedScore = BardPvPTargetPolicy.Score(guardedTarget, BardPvPActionIntent.EagleEyeShot);
		var exposedScore = BardPvPTargetPolicy.Score(exposedTarget, BardPvPActionIntent.EagleEyeShot);

		AssertEqual(exposedScore, guardedScore, "Eagle Eye Shot should not penalize Guard because the role action ignores Guard");
	}

	static void BardOffensiveTargetPolicyTreatsGuardedEagleEyeTargetAsExposed()
	{
		const ulong guardedTargetId = 1;
		const ulong exposedTargetId = 2;
		const float targetHealthRatio = 0.40f;
		const uint fullMp = 10_000;
		const double noExpectedDamage = 0.0;
		var guardedTarget = BardOffensiveTarget(
			guardedTargetId,
			healthRatio: targetHealthRatio,
			currentMp: fullMp,
			hasGuard: true,
			isExposed: false,
			isInNormalRange: true,
			expectedDamageRatio: noExpectedDamage);
		var exposedTarget = guardedTarget with { TargetId = exposedTargetId, HasGuard = false, IsExposed = true };

		var guardedScore = BardPvPTargetPolicy.Score(guardedTarget, BardPvPActionIntent.EagleEyeShot);
		var exposedScore = BardPvPTargetPolicy.Score(exposedTarget, BardPvPActionIntent.EagleEyeShot);

		AssertEqual(exposedScore, guardedScore, "Eagle Eye Shot should treat guarded in-range targets as exposed because it ignores Guard");
	}

	static void BardOffensiveTargetPolicyPreservesEagleEyeMitigation()
	{
		var mitigatedTarget = BardOffensiveTarget(
			1,
			healthRatio: 0.10f,
			currentMp: 10_000,
			hasGuard: true,
			effectiveHealthRatio: 0.40,
			guardPiercingEffectiveHealthRatio: 0.40,
			expectedDamageRatio: 0.20);
		var noDamageTarget = mitigatedTarget with { ExpectedDamageRatio = 0.0 };

		var score = BardPvPTargetPolicy.Score(mitigatedTarget, BardPvPActionIntent.EagleEyeShot);
		var noDamageScore = BardPvPTargetPolicy.Score(noDamageTarget, BardPvPActionIntent.EagleEyeShot);

		AssertEqual(noDamageScore, score, "Eagle Eye Shot should not treat nonlethal mitigated targets as direct secure");
	}

	static void BardOffensiveTargetPolicyPenalizesBlastResilience()
	{
		var resilientTarget = BardOffensiveTarget(
			1,
			healthRatio: 0.40f,
			currentMp: 10_000,
			hasResilience: true,
			lineTargetCount: 1);
		var exposedTarget = resilientTarget with { TargetId = 2, HasResilience = false };

		var resilientScore = BardPvPTargetPolicy.Score(resilientTarget, BardPvPActionIntent.BlastArrow);
		var exposedScore = BardPvPTargetPolicy.Score(exposedTarget, BardPvPActionIntent.BlastArrow);

		AssertTrue(resilientScore < exposedScore, "Blast Arrow should penalize Resilience when displacement value matters");
	}

	static void BardHarmonicArrowRejectsGuardedNonlethalTarget()
	{
		var input = BardOffensiveInput(
			BardOffensiveTarget(
				1,
				healthRatio: 0.42f,
				currentMp: 10_000,
				hasGuard: true,
				effectiveHealthRatio: 0.42,
				expectedDamageRatio: 0.30),
			alliesCanBurst: true,
			objectiveControlNeeded: true,
			harmonicWouldOvercap: true);

		AssertFalse(BardPvPOffensiveDecisionPolicy.ShouldUseHarmonicArrow(input), "Harmonic Arrow should not spend into Guard when the target survives");
	}

	static void BardHarmonicArrowAcceptsUnblockedChargeOvercap()
	{
		var input = BardOffensiveInput(
			BardOffensiveTarget(1, healthRatio: 0.90f, currentMp: 10_000),
			harmonicWouldOvercap: true);
		var guardedInput = input with { Target = input.Target with { HasGuard = true } };

		AssertTrue(BardPvPOffensiveDecisionPolicy.ShouldUseHarmonicArrow(input), "Harmonic Arrow should spend before wasting a charge on an unblocked target");
		AssertFalse(BardPvPOffensiveDecisionPolicy.ShouldUseHarmonicArrow(guardedInput), "Harmonic Arrow overcap should still respect blocked damage");
	}

	static void BardHarmonicArrowAcceptsLowMpConversion()
	{
		var input = BardOffensiveInput(
			BardOffensiveTarget(1, healthRatio: 0.90f, currentMp: PvPScoringFactors.LowMp));

		AssertTrue(BardPvPOffensiveDecisionPolicy.ShouldUseHarmonicArrow(input), "Harmonic Arrow should convert high-health low MP pressure");
	}

	static void BardPitchPerfectAcceptsRepertoireAllyFocusFollowUp()
	{
		var input = BardOffensiveInput(
			BardOffensiveTarget(1, healthRatio: 0.75f, currentMp: 10_000, allyFocusCount: 1),
			followUpAvailable: true,
			hasRepertoire: true);

		AssertTrue(BardPvPOffensiveDecisionPolicy.ShouldUsePitchPerfect(input), "Pitch Perfect should convert Repertoire into an ally focused follow up");
	}

	static void BardPitchPerfectAcceptsRepertoireLowMpTarget()
	{
		var input = BardOffensiveInput(
			BardOffensiveTarget(1, healthRatio: 0.90f, currentMp: PvPScoringFactors.LowMp),
			hasRepertoire: true);

		AssertTrue(BardPvPOffensiveDecisionPolicy.ShouldUsePitchPerfect(input), "Pitch Perfect should convert Repertoire into high-health low MP pressure");
	}

	static void BardPitchPerfectAcceptsRepertoireObjectiveTarget()
	{
		var input = BardOffensiveInput(
			BardOffensiveTarget(1, healthRatio: 0.90f, currentMp: 10_000, isObjectiveRelevant: true),
			hasRepertoire: true);

		AssertTrue(BardPvPOffensiveDecisionPolicy.ShouldUsePitchPerfect(input), "Pitch Perfect should convert Repertoire into objective pressure");
	}

	static void BardPitchPerfectAcceptsRepertoireAllyBurst()
	{
		var input = BardOffensiveInput(
			BardOffensiveTarget(1, healthRatio: 0.90f, currentMp: 10_000),
			alliesCanBurst: true,
			hasRepertoire: true);

		AssertTrue(BardPvPOffensiveDecisionPolicy.ShouldUsePitchPerfect(input), "Pitch Perfect should convert Repertoire during allied burst");
	}

	static void BardPitchPerfectRejectsRepertoireFiller()
	{
		var input = BardOffensiveInput(
			BardOffensiveTarget(1, healthRatio: 0.90f, currentMp: 10_000),
			hasRepertoire: true);

		AssertFalse(BardPvPOffensiveDecisionPolicy.ShouldUsePitchPerfect(input), "Pitch Perfect should hold Repertoire when the target has no pressure value");
	}

	static void BardApexArrowAcceptsObjectiveLineValue()
	{
		var input = BardOffensiveInput(
			BardOffensiveTarget(
				1,
				healthRatio: 0.85f,
				currentMp: 10_000,
				isObjectiveRelevant: true,
				lineTargetCount: 2),
			objectiveControlNeeded: true);

		AssertTrue(BardPvPOffensiveDecisionPolicy.ShouldUseApexArrow(input), "Apex Arrow should spend when the line pressures an objective target");
	}

	static void BardApexArrowAcceptsGuardedObjectivePressure()
	{
		var input = BardOffensiveInput(
			BardOffensiveTarget(
				1,
				healthRatio: 0.85f,
				currentMp: 10_000,
				hasGuard: true,
				isObjectiveRelevant: true),
			objectiveControlNeeded: true);

		AssertTrue(BardPvPOffensiveDecisionPolicy.ShouldUseApexArrow(input), "Apex Arrow should spend into Guard when objective pressure is valuable");
	}

	static void BardApexArrowAcceptsGuardedForcedTiming()
	{
		var input = BardOffensiveInput(
			BardOffensiveTarget(1, healthRatio: 0.85f, currentMp: 10_000, hasGuard: true),
			forcedExpiryWindow: true);

		AssertTrue(BardPvPOffensiveDecisionPolicy.ShouldUseApexArrow(input), "Apex Arrow should spend into Guard when buff timing would be lost");
	}

	static void BardApexArrowAcceptsStandaloneObjectiveValue()
	{
		var input = BardOffensiveInput(
			BardOffensiveTarget(
				1,
				healthRatio: 0.85f,
				currentMp: 10_000,
				isObjectiveRelevant: true),
			objectiveControlNeeded: true);

		AssertTrue(BardPvPOffensiveDecisionPolicy.ShouldUseApexArrow(input), "Apex Arrow should spend for objective value without requiring line value");
	}

	static void BardApexArrowAcceptsStandaloneAllyBurstValue()
	{
		var input = BardOffensiveInput(
			BardOffensiveTarget(1, healthRatio: 0.85f, currentMp: 10_000),
			alliesCanBurst: true);

		AssertTrue(BardPvPOffensiveDecisionPolicy.ShouldUseApexArrow(input), "Apex Arrow should spend for ally burst without requiring line value");
	}

	static void BardApexArrowRejectsGuardedFiller()
	{
		var input = BardOffensiveInput(
			BardOffensiveTarget(1, healthRatio: 0.85f, currentMp: 10_000, hasGuard: true));

		AssertFalse(BardPvPOffensiveDecisionPolicy.ShouldUseApexArrow(input), "Apex Arrow should not spend filler into Guard");
	}

	static void BardBlastArrowAcceptsObjectiveDisplacement()
	{
		var input = BardOffensiveInput(
			BardOffensiveTarget(
				1,
				healthRatio: 0.85f,
				currentMp: 10_000,
				isObjectiveRelevant: true),
			objectiveControlNeeded: true,
			hasBlastArrowReady: true);

		AssertTrue(BardPvPOffensiveDecisionPolicy.ShouldUseBlastArrow(input), "Blast Arrow should spend for objective displacement");
	}

	static void BardBlastArrowRejectsResilienceDisplacement()
	{
		var input = BardOffensiveInput(
			BardOffensiveTarget(
				1,
				healthRatio: 0.85f,
				currentMp: 10_000,
				isObjectiveRelevant: true),
			objectiveControlNeeded: true,
			hasBlastArrowReady: true);
		var resilientInput = input with { Target = input.Target with { HasResilience = true } };

		AssertFalse(BardPvPOffensiveDecisionPolicy.ShouldUseBlastArrow(resilientInput), "Blast Arrow should reject Resilience when displacement is the primary value");
	}

	static void BardBlastArrowRejectsBlastReadyFiller()
	{
		var input = BardOffensiveInput(
			BardOffensiveTarget(1, healthRatio: 0.85f, currentMp: 10_000),
			hasBlastArrowReady: true);

		AssertFalse(BardPvPOffensiveDecisionPolicy.ShouldUseBlastArrow(input), "Blast Arrow should not spend Blast Ready without line, objective, peel, or committed follow up value");
	}

	static void BardEncoreOfLightAcceptsLowMpConversion()
	{
		var input = BardOffensiveInput(
			BardOffensiveTarget(
				1,
				healthRatio: 0.85f,
				currentMp: PvPScoringFactors.LowMp,
				guardAvailability: PvPGuardAvailability.CoolingDown),
			hasFinalFantasia: true,
			hasFrontlinersMarch: true,
			hasGuardCooldownKnowledge: true);

		AssertTrue(BardPvPOffensiveDecisionPolicy.ShouldUseEncoreOfLight(input), "Encore of Light should convert low MP pressure when Guard is unavailable");
	}

	static void BardEncoreOfLightAcceptsAllyBurstWindow()
	{
		var input = BardOffensiveInput(
			BardOffensiveTarget(
				1,
				healthRatio: 0.85f,
				currentMp: 10_000,
				guardAvailability: PvPGuardAvailability.Ready),
			alliesCanBurst: true,
			hasFrontlinersMarch: true,
			hasGuardCooldownKnowledge: true);

		AssertTrue(BardPvPOffensiveDecisionPolicy.ShouldUseEncoreOfLight(input), "Encore of Light should spend for ally burst even when Guard can react");
	}

	static void BardEncoreOfLightAcceptsFinalFantasiaPushWindow()
	{
		var input = BardOffensiveInput(
			BardOffensiveTarget(
				1,
				healthRatio: 0.85f,
				currentMp: 10_000,
				guardAvailability: PvPGuardAvailability.Ready),
			hasFinalFantasia: true,
			hasGuardCooldownKnowledge: true);

		AssertTrue(BardPvPOffensiveDecisionPolicy.ShouldUseEncoreOfLight(input), "Encore of Light should spend for Final Fantasia push windows");
	}

	static void BardEncoreOfLightRejectsBlockedFiller()
	{
		var input = BardOffensiveInput(
			BardOffensiveTarget(
				1,
				healthRatio: 0.85f,
				currentMp: PvPScoringFactors.LowMp,
				hasGuard: true,
				guardAvailability: PvPGuardAvailability.Active),
			hasFinalFantasia: true,
			hasFrontlinersMarch: true);

		AssertFalse(BardPvPOffensiveDecisionPolicy.ShouldUseEncoreOfLight(input), "Encore of Light should not spend into blocked damage");
	}

	static void BardEncoreOfLightRejectsGuardReactionConversion()
	{
		var input = BardOffensiveInput(
			BardOffensiveTarget(
				1,
				healthRatio: 0.85f,
				currentMp: PvPScoringFactors.LowMp,
				guardAvailability: PvPGuardAvailability.Ready),
			hasFrontlinersMarch: true,
			hasGuardCooldownKnowledge: true);

		AssertFalse(BardPvPOffensiveDecisionPolicy.ShouldUseEncoreOfLight(input), "Encore of Light should hold low MP conversion when the target can Guard and no priority signal exists");
	}

	static void BardEncoreOfLightRejectsUnknownGuardReactionConversion()
	{
		var input = BardOffensiveInput(
			BardOffensiveTarget(
				1,
				healthRatio: 0.85f,
				currentMp: PvPScoringFactors.LowMp,
				guardAvailability: PvPGuardAvailability.Unknown),
			hasFrontlinersMarch: true);

		AssertFalse(BardPvPOffensiveDecisionPolicy.ShouldUseEncoreOfLight(input), "Encore of Light should hold low MP conversion when Guard reaction knowledge is unavailable");
	}

	static void BardPowerfulShotAcceptsSafePressureFiller()
	{
		var input = BardOffensiveInput(
			BardOffensiveTarget(1, healthRatio: 0.55f, currentMp: PvPScoringFactors.MediumMp));

		AssertTrue(BardPvPOffensiveDecisionPolicy.ShouldUsePowerfulShot(input), "Powerful Shot should fill safe pressure into a low resource kill window");
	}

	static void BardPowerfulShotAcceptsNeutralSafeFiller()
	{
		var input = BardOffensiveInput(
			BardOffensiveTarget(1, healthRatio: 0.90f, currentMp: 10_000));

		AssertTrue(BardPvPOffensiveDecisionPolicy.ShouldUsePowerfulShot(input), "Powerful Shot should remain available as safe neutral filler");
	}

	static void BardPowerfulShotRejectsBlockedTarget()
	{
		var input = BardOffensiveInput(
			BardOffensiveTarget(
				1,
				healthRatio: 0.55f,
				currentMp: PvPScoringFactors.MediumMp,
				hasGuard: true));

		AssertFalse(BardPvPOffensiveDecisionPolicy.ShouldUsePowerfulShot(input), "Powerful Shot should not spend into blocked targets");
	}

	static void BardOffensiveDecisionPolicyRerunsLiveGuardState()
	{
		var target = BardOffensiveTarget(1, healthRatio: 0.55f, currentMp: PvPScoringFactors.MediumMp);
		var clearInput = BardOffensiveInput(target);
		var guardedInput = clearInput with { Target = target with { HasGuard = true } };

		AssertTrue(BardPvPOffensiveDecisionPolicy.ShouldUsePowerfulShot(clearInput), "Bard should accept safe pressure before a live Guard refresh");
		AssertFalse(BardPvPOffensiveDecisionPolicy.ShouldUsePowerfulShot(guardedInput), "Bard should reject the same target after live state refresh shows Guard");
	}

	static void BardTargetRefreshUpdatesLiveSpatialSignals()
	{
		const ulong targetId = 1;
		const float staleHealthRatio = 0.75f;
		const uint fullMp = 10_000;
		const int staleTargetCount = 1;
		const int expectedLineTargetCount = 3;
		const int expectedSplashTargetCount = 4;
		var staleSnapshot = BardOffensiveTarget(
			targetId,
			healthRatio: staleHealthRatio,
			currentMp: fullMp,
			isExposed: false,
			isInNormalRange: false,
			lineTargetCount: staleTargetCount,
			splashTargetCount: staleTargetCount);

		var spatialState = new BardPvPTargetSpatialState(
			IsInNormalRange: true,
			LineTargetCount: expectedLineTargetCount,
			SplashTargetCount: expectedSplashTargetCount);

		var refreshedSnapshot = BardPvPTargetSnapshotRefresher.RefreshSpatialState(staleSnapshot, spatialState);

		AssertTrue(refreshedSnapshot.IsInNormalRange, "refresh should replace stale range state");
		AssertTrue(refreshedSnapshot.IsExposed, "refresh should recompute exposure from live Guard and range state");
		AssertEqual(expectedLineTargetCount, refreshedSnapshot.LineTargetCount, "refresh should replace stale line target count");
		AssertEqual(expectedSplashTargetCount, refreshedSnapshot.SplashTargetCount, "refresh should replace stale splash target count");
	}
}
