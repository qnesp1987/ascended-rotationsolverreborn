using System.Text.RegularExpressions;
using RotationSolver.RebornRotations.Ranged;

namespace RotationSolver.Tests;

internal static partial class PvETestSuite
{
	static void BardAscendedDotThresholdsHonorTargetTime()
	{
		AssertFalse(
			BardAscendedDecisionPolicy.ShouldApplyBothDots(14.99f, isBossTarget: false, replacesEnhancedFiller: false),
			"both DoTs should reject targets below the 15 second floor");
		AssertTrue(
			BardAscendedDecisionPolicy.ShouldApplyBothDots(15f, isBossTarget: false, replacesEnhancedFiller: false),
			"both DoTs should accept targets at the 15 second floor");
		AssertFalse(
			BardAscendedDecisionPolicy.ShouldApplyBothDots(17.99f, isBossTarget: false, replacesEnhancedFiller: true),
			"both DoTs should reject enhanced filler replacement below 18 seconds");
		AssertTrue(
			BardAscendedDecisionPolicy.ShouldApplyBothDots(18f, isBossTarget: false, replacesEnhancedFiller: true),
			"both DoTs should accept enhanced filler replacement at 18 seconds");

		AssertFalse(
			BardAscendedDecisionPolicy.ShouldRefreshIronJaws(8.99f, isBossTarget: false, replacesEnhancedFiller: false),
			"Iron Jaws should reject targets below the 9 second floor");
		AssertTrue(
			BardAscendedDecisionPolicy.ShouldRefreshIronJaws(9f, isBossTarget: false, replacesEnhancedFiller: false),
			"Iron Jaws should accept targets at the 9 second floor");
		AssertFalse(
			BardAscendedDecisionPolicy.ShouldRefreshIronJaws(11.99f, isBossTarget: false, replacesEnhancedFiller: true),
			"Iron Jaws should reject enhanced filler replacement below 12 seconds");
		AssertTrue(
			BardAscendedDecisionPolicy.ShouldRefreshIronJaws(12f, isBossTarget: false, replacesEnhancedFiller: true),
			"Iron Jaws should accept enhanced filler replacement at 12 seconds");

		AssertFalse(
			BardAscendedDecisionPolicy.ShouldApplyCausticOnly(11.99f, isBossTarget: false),
			"Caustic Bite alone should reject targets below 12 seconds");
		AssertTrue(
			BardAscendedDecisionPolicy.ShouldApplyCausticOnly(12f, isBossTarget: false),
			"Caustic Bite alone should accept targets at 12 seconds");
		AssertFalse(
			BardAscendedDecisionPolicy.ShouldApplyStormbiteOnly(14.99f, isBossTarget: false),
			"Stormbite alone should reject targets below 15 seconds");
		AssertTrue(
			BardAscendedDecisionPolicy.ShouldApplyStormbiteOnly(15f, isBossTarget: false),
			"Stormbite alone should accept targets at 15 seconds");
	}

	static void BardAscendedDotThresholdsUseBossFallbackOnlyWhenTtkIsUnknown()
	{
		AssertTrue(
			BardAscendedDecisionPolicy.ShouldApplyBothDots(float.NaN, isBossTarget: true, replacesEnhancedFiller: true),
			"boss fallback should allow both DoTs only when target time is unknown");
		AssertFalse(
			BardAscendedDecisionPolicy.ShouldApplyBothDots(14.99f, isBossTarget: true, replacesEnhancedFiller: false),
			"boss targets with known planned kill time should still honor the 15 second floor");
		AssertTrue(
			BardAscendedDecisionPolicy.ShouldRefreshIronJaws(float.NaN, isBossTarget: true, replacesEnhancedFiller: true),
			"boss fallback should allow Iron Jaws only when target time is unknown");
		AssertFalse(
			BardAscendedDecisionPolicy.ShouldRefreshIronJaws(8.99f, isBossTarget: true, replacesEnhancedFiller: false),
			"boss targets with known planned kill time should still honor the 9 second floor");
		AssertTrue(
			BardAscendedDecisionPolicy.ShouldApplyCausticOnly(float.NaN, isBossTarget: true),
			"boss fallback should allow Caustic Bite when target time is unknown");
		AssertFalse(
			BardAscendedDecisionPolicy.ShouldApplyStormbiteOnly(14.99f, isBossTarget: true),
			"boss targets with known planned kill time should still honor the Stormbite floor");
	}

	static void BardAscendedDotRuntimeUsesResolvedTargetTtk()
	{
		var source = StripSourceComments(File.ReadAllText(RepositoryPath("RotationSolver", "RebornRotations", "Ranged", "BRD_Ascended.cs")));
		var ironJawsCandidate = ExtractMethodBody(source, "bool HasTargetAwareIronJawsCandidate");
		var dotCandidate = ExtractMethodBody(source, "bool HasTargetAwareDoTUseCandidate");
		var stormbiteCandidate = ExtractMethodBody(source, "bool HasTargetAwareStormbiteUseCandidate");
		var causticCandidate = ExtractMethodBody(source, "bool HasTargetAwareCausticBiteUseCandidate");
		var tryUseIronJaws = ExtractMethodBody(source, "bool TryUseIronJaws");
		var tryUseDoTs = ExtractMethodBody(source, "bool TryUseDoTs");
		var previewTarget = ExtractMethodBody(source, "bool TryPreviewActionTarget");
		var dotTargetTimeToKill = ExtractMethodBody(source, "float GetDotTargetTimeToKill");
		var dotBossTarget = ExtractMethodBody(source, "bool IsDotBossTarget");
		var hasTargetStatusData = ExtractMethodBody(source, "bool HasTargetStatusData");
		var shouldUseIronJaws = ExtractMethodBody(source, "bool ShouldUseIronJawsOnTarget");
		var shouldUseStormbite = ExtractMethodBody(source, "bool ShouldUseStormbiteOnTarget");
		var shouldUseCausticBite = ExtractMethodBody(source, "bool ShouldUseCausticBiteOnTarget");
		var targetAwareHelpers = string.Join(
			Environment.NewLine,
			ironJawsCandidate,
			dotCandidate,
			stormbiteCandidate,
			causticCandidate,
			tryUseIronJaws,
			tryUseDoTs);
		var forbiddenHardTargetHelpers =
			@"\b(CurrentTarget|EffectiveTargetTimeToKill|TargetIsBoss|CanDoTMobs|TargetHasAllDots|AnyDotEnding|DoTsEnding)\b";

		AssertSourceMatches(
			source,
			@"\bprivate\s+bool\s+WouldUseIronJaws\s*=>\s*HasTargetAwareIronJawsCandidate\s*\(\s*\)\s*;",
			"WouldUseIronJaws should delegate to the target aware Iron Jaws candidate path");
		AssertSourceMatches(
			source,
			@"\bprivate\s+bool\s+WouldUseDoTs\s*=>\s*HasTargetAwareDoTUseCandidate\s*\(\s*\)\s*;",
			"WouldUseDoTs should delegate to the target aware DoT use candidate path");

		AssertSourceMatches(
			ironJawsCandidate,
			@"TryPreviewActionTarget\s*\(\s*IronJawsPvE\s*,\s*out\s+var\s+target\s*,\s*skipStatusProvideCheck\s*:\s*true\s*\).*?ShouldUseIronJawsOnTarget\s*\(\s*target\s*\)",
			"Iron Jaws candidate should preview the resolved target before threshold evaluation");
		AssertSourceMatches(
			stormbiteCandidate,
			@"TryPreviewActionTarget\s*\(\s*Stormbite\s*,\s*out\s+var\s+stormbiteTarget\s*,\s*skipStatusProvideCheck\s*:\s*true\s*\).*?ShouldUseStormbiteOnTarget\s*\(\s*stormbiteTarget\s*\)",
			"Stormbite candidate should preview the resolved target before threshold evaluation");
		AssertSourceMatches(
			causticCandidate,
			@"TryPreviewActionTarget\s*\(\s*CausticBite\s*,\s*out\s+var\s+causticTarget\s*,\s*skipStatusProvideCheck\s*:\s*true\s*\).*?ShouldUseCausticBiteOnTarget\s*\(\s*causticTarget\s*\)",
			"Caustic Bite candidate should preview the resolved target before threshold evaluation");
		AssertSourceMatches(
			dotCandidate,
			@"HasTargetAwareStormbiteUseCandidate\s*\(\s*\).*?HasTargetAwareCausticBiteUseCandidate\s*\(\s*\)",
			"DoT use candidate should compose the target aware Stormbite and Caustic Bite paths");

		AssertSourceMatches(
			tryUseIronJaws,
			@"HasTargetAwareIronJawsCandidate\s*\(\s*\).*?IronJawsPvE\.CanUse\s*\(\s*out\s+act\s*,\s*skipStatusProvideCheck\s*:\s*true\s*\)",
			"TryUseIronJaws should commit only after the target aware candidate passes");
		AssertSourceMatches(
			tryUseDoTs,
			@"TryPreviewActionTarget\s*\(\s*Stormbite\s*,\s*out\s+var\s+stormbiteTarget\s*,\s*skipStatusProvideCheck\s*:\s*true\s*\).*?ShouldUseStormbiteOnTarget\s*\(\s*stormbiteTarget\s*\).*?Stormbite\.CanUse\s*\(\s*out\s+act\s*,\s*skipStatusProvideCheck\s*:\s*true\s*\).*?TryPreviewActionTarget\s*\(\s*CausticBite\s*,\s*out\s+var\s+causticTarget\s*,\s*skipStatusProvideCheck\s*:\s*true\s*\).*?ShouldUseCausticBiteOnTarget\s*\(\s*causticTarget\s*\).*?CausticBite\.CanUse\s*\(\s*out\s+act\s*,\s*skipStatusProvideCheck\s*:\s*true\s*\)",
			"TryUseDoTs should preview resolved targets before committing with named status skips");

		AssertSourceMatches(
			previewTarget,
			@"var\s+wasActionPreview\s*=\s*IBaseAction\.ActionPreview\s*;.*?try\s*\{.*?IBaseAction\.ActionPreview\s*=\s*true\s*;.*?action\.CanUse\s*\(.*?finally\s*\{.*?IBaseAction\.ActionPreview\s*=\s*wasActionPreview\s*;",
			"preview helper should restore ActionPreview after every probe");
		AssertSourceMatches(
			previewTarget,
			@"action\.PreviewTarget\?\.Target",
			"preview helper should expose the resolved preview target");
		AssertSourceDoesNotMatch(
			previewTarget,
			@"\b(skipStatusNeed|skipTargetStatusNeedCheck|skipComboCheck|skipCastingCheck|usedUp|skipAoeCheck|skipTTKCheck|gcdCountForAbility|checkActionManager|targetOverride)\b",
			"preview helper should expose only the status-provide skip required by DoT probes");

		AssertSourceDoesNotMatch(
			targetAwareHelpers,
			forbiddenHardTargetHelpers,
			"target aware candidate helpers should not reference hard target helpers");
		AssertSourceDoesNotMatch(
			source,
			@"\bprivate\s+static\s+bool\s+(TargetHasBossIcon|TargetIsBoss)\b",
			"hard-target boss helpers should be removed after resolved-target DoT gating");
		AssertSourceDoesNotMatch(
			dotTargetTimeToKill,
			@"\b(CurrentTarget|EffectiveTargetTimeToKill)\b",
			"target time to kill should be read from the resolved target");
		AssertSourceDoesNotMatch(
			dotBossTarget,
			@"\bTargetIsBoss\b",
			"boss fallback should be evaluated from the resolved target");
		AssertSourceMatches(
			hasTargetStatusData,
			@"return\s+action\.Setting\.TargetStatusProvide\s*!=\s*null\s*;",
			"status data should reject candidates before threshold evaluation when missing");
		AssertSourceMatches(
			shouldUseIronJaws,
			@"HasTargetStatusData\s*\(\s*Stormbite\s*\).*?HasTargetStatusData\s*\(\s*CausticBite\s*\).*?BardAscendedDecisionPolicy\.ShouldRefreshIronJaws",
			"Iron Jaws threshold evaluation should require target status data first");
		AssertSourceMatches(
			shouldUseStormbite,
			@"HasTargetStatusData\s*\(\s*Stormbite\s*\).*?HasTargetStatusData\s*\(\s*CausticBite\s*\).*?BardAscendedDecisionPolicy\.(ShouldApplyBothDots|ShouldApplyStormbiteOnly)",
			"Stormbite threshold evaluation should require target status data first");
		AssertSourceMatches(
			shouldUseStormbite,
			@"hasStormbite\s*&&\s*\(\s*IronJawsPvE\.EnoughLevel\s*\|\|\s*!\s*TargetDoTEnding\s*\(\s*target\s*,\s*Stormbite\s*\)\s*\).*?BardAscendedDecisionPolicy\.ShouldApplyStormbiteOnly",
			"Stormbite refresh without Iron Jaws should still apply the target TTK threshold");
		AssertSourceDoesNotMatch(
			shouldUseStormbite,
			@"return\s+!\s*IronJawsPvE\.EnoughLevel\s*&&\s*TargetDoTEnding\s*\(\s*target\s*,\s*Stormbite\s*\)",
			"Stormbite refresh should not bypass the target TTK threshold");
		AssertSourceMatches(
			shouldUseCausticBite,
			@"HasTargetStatusData\s*\(\s*Stormbite\s*\).*?HasTargetStatusData\s*\(\s*CausticBite\s*\).*?BardAscendedDecisionPolicy\.(ShouldApplyBothDots|ShouldApplyCausticOnly)",
			"Caustic Bite threshold evaluation should require target status data first");
		AssertSourceMatches(
			shouldUseCausticBite,
			@"hasCausticBite\s*&&\s*\(\s*IronJawsPvE\.EnoughLevel\s*\|\|\s*!\s*TargetDoTEnding\s*\(\s*target\s*,\s*CausticBite\s*\)\s*\).*?BardAscendedDecisionPolicy\.ShouldApplyCausticOnly",
			"Caustic Bite refresh without Iron Jaws should still apply the target TTK threshold");
		AssertSourceDoesNotMatch(
			shouldUseCausticBite,
			@"return\s+!\s*IronJawsPvE\.EnoughLevel\s*&&\s*TargetDoTEnding\s*\(\s*target\s*,\s*CausticBite\s*\)",
			"Caustic Bite refresh should not bypass the target TTK threshold");
	}

	static void BardAscendedSongPresetsMapToExpectedDurations()
	{
		var standard = BardAscendedDecisionPolicy.GetSongDurations(
			BardAscendedSongTiming.Standard,
			new BardAscendedSongDurations(1f, 2f, 3f));
		var cycle369 = BardAscendedDecisionPolicy.GetSongDurations(
			BardAscendedSongTiming.Cycle369,
			new BardAscendedSongDurations(1f, 2f, 3f));
		var adjustedStandard = BardAscendedDecisionPolicy.GetSongDurations(
			BardAscendedSongTiming.AdjustedStandard,
			new BardAscendedSongDurations(1f, 2f, 3f));
		var custom = BardAscendedDecisionPolicy.GetSongDurations(
			BardAscendedSongTiming.Custom,
			new BardAscendedSongDurations(40f, 38f, 37f));

		AssertEqual(new BardAscendedSongDurations(42f, 42f, 33f), standard, "standard should hold songs for the 3 3 12 preset");
		AssertEqual(new BardAscendedSongDurations(42f, 42f, 33f), adjustedStandard, "adjusted standard should hold songs for the standard preset");
		AssertEqual(new BardAscendedSongDurations(42f, 39f, 36f), cycle369, "cycle 3 6 9 should hold songs for the expected preset");
		AssertEqual(new BardAscendedSongDurations(40f, 38f, 37f), custom, "custom should return caller supplied durations");
	}

	static void BardAscendedRadiantFinaleUsesDamageBuffStatus()
	{
		var bardSource = StripSourceComments(File.ReadAllText(RepositoryPath(
			"RotationSolver.Basic",
			"Rotations",
			"Basic",
			"BardRotation.cs")));
		var ascendedSource = StripSourceComments(File.ReadAllText(RepositoryPath(
			"RotationSolver",
			"RebornRotations",
			"Ranged",
			"BRD_Ascended.cs")));

		AssertSourceMatches(
			bardSource,
			@"HasRadiantFinale\s*=>\s*StatusHelper\.PlayerHasStatus\s*\(\s*true\s*,\s*StatusID\.RadiantFinale_2964\s*,\s*StatusID\.RadiantFinale\s*\)",
			"shared Bard Radiant Finale detection should prefer the damage buff status and keep legacy status compatibility");
		AssertSourceMatches(
			ascendedSource,
			@"RadiantFinaleStatuses\s*=\s*\[\s*StatusID\.RadiantFinale_2964\s*\]",
			"BRD Ascended burst status sets should use the Radiant Finale damage buff");
		AssertSourceMatches(
			ascendedSource,
			@"RagingFinaleStatuses\s*=\s*\[\s*StatusID\.RagingStrikes\s*,\s*StatusID\.RadiantFinale_2964\s*\]",
			"BRD Ascended Raging Strikes and Radiant Finale burst set should use the Radiant Finale damage buff");
		AssertSourceMatches(
			ascendedSource,
			@"BattleFinaleStatuses\s*=\s*\[\s*StatusID\.BattleVoice\s*,\s*StatusID\.RadiantFinale_2964\s*\]",
			"BRD Ascended Battle Voice and Radiant Finale burst set should use the Radiant Finale damage buff");
		AssertSourceMatches(
			ascendedSource,
			@"FullBurstStatuses\s*=\s*\[\s*StatusID\.RagingStrikes\s*,\s*StatusID\.BattleVoice\s*,\s*StatusID\.RadiantFinale_2964\s*\]",
			"BRD Ascended full burst should use the Radiant Finale damage buff");
		AssertSourceDoesNotMatch(
			ascendedSource,
			@"FullBurstStatuses\s*=\s*\[\s*StatusID\.RagingStrikes\s*,\s*StatusID\.BattleVoice\s*,\s*StatusID\.RadiantFinale\s*\]",
			"BRD Ascended full burst should not wait on the non damage Radiant Finale status");
	}

	static void BardAscendedApexSpendsDuringBurstAndMageBalladWindows()
	{
		AssertTrue(
			ShouldSpendApex(BardAscendedSongPhase.WanderersMinuet, soulVoice: 80, isInBurst: true),
			"Apex should spend at 80 Soul Voice during burst");
		AssertFalse(
			ShouldSpendApex(BardAscendedSongPhase.WanderersMinuet, soulVoice: 79, isInBurst: true),
			"Apex should hold below 80 Soul Voice during burst");
		AssertTrue(
			ShouldSpendApex(BardAscendedSongPhase.MagesBallad, soulVoice: 100, songSecondsRemaining: 30f),
			"Apex should spend at 100 Soul Voice in Mage's Ballad");
		AssertTrue(
			ShouldSpendApex(BardAscendedSongPhase.MagesBallad, soulVoice: 80, songSecondsRemaining: 18f),
			"Apex should spend at the early Mage's Ballad window boundary");
		AssertTrue(
			ShouldSpendApex(BardAscendedSongPhase.MagesBallad, soulVoice: 80, songSecondsRemaining: 21f),
			"Apex should spend at the late Mage's Ballad window boundary");
		AssertFalse(
			ShouldSpendApex(BardAscendedSongPhase.MagesBallad, soulVoice: 80, songSecondsRemaining: 17.99f),
			"Apex should hold before the Mage's Ballad window");
		AssertFalse(
			ShouldSpendApex(BardAscendedSongPhase.MagesBallad, soulVoice: 80, songSecondsRemaining: 21.01f),
			"Apex should hold after the Mage's Ballad window");
	}

	static void BardAscendedApexHoldsDuringArmyPaeon()
	{
		AssertFalse(
			ShouldSpendApex(BardAscendedSongPhase.ArmysPaeon, soulVoice: 100),
			"Apex should hold through Army's Paeon when no end of fight dump is needed");
		AssertFalse(
			ShouldSpendApex(BardAscendedSongPhase.ArmysPaeon, soulVoice: 80, isInBurst: false),
			"Apex should not spend only because Army's Paeon has enough Soul Voice");
	}

	static void BardAscendedApexCapFallbackStaysInMageBallad()
	{
		AssertFalse(
			ShouldSpendApex(BardAscendedSongPhase.WanderersMinuet, soulVoice: 100),
			"Apex should hold capped Soul Voice in Wanderer's Minuet when no end of fight dump is needed");
		AssertFalse(
			ShouldSpendApex(BardAscendedSongPhase.ArmysPaeon, soulVoice: 100),
			"Apex should hold capped Soul Voice in Army's Paeon when no end of fight dump is needed");
		AssertTrue(
			ShouldSpendApex(BardAscendedSongPhase.MagesBallad, soulVoice: 100, songSecondsRemaining: 30f),
			"Apex should still spend capped Soul Voice in Mage's Ballad");
		AssertFalse(
			ShouldSpendApex(
				BardAscendedSongPhase.WanderersMinuet,
				soulVoice: 100,
				wouldUseIronJaws: true),
			"Iron Jaws should still block non end of fight Apex spending");
	}

	static void BardAscendedRuntimeKeepsBurstActionabilityAtBuffGates()
	{
		var source = StripSourceComments(File.ReadAllText(RepositoryPath("RotationSolver", "RebornRotations", "Ranged", "BRD_Ascended.cs")));
		var policySource = StripSourceComments(File.ReadAllText(RepositoryPath("RotationSolver", "RebornRotations", "Ranged", "BardAscendedDecisionPolicy.cs")));
		var pendingRadiantFinaleGate = ExtractMethodBody(source, "bool CanStartBurstWithRadiantFinale");
		var pendingBattleVoiceGate = ExtractMethodBody(source, "bool CanStartBurstWithBattleVoice");
		var pendingRagingGate = ExtractMethodBody(source, "bool CanStartBurstWithRagingStrikes");
		var tryUseRadiantFinale = ExtractMethodBody(source, "bool TryUseRadiantFinale");
		var tryUseBattleVoice = ExtractMethodBody(source, "bool TryUseBattleVoice");
		var tryUseRagingStrikes = ExtractMethodBody(source, "bool TryUseRagingStrikes");

		AssertSourceDoesNotMatch(
			policySource,
			@"BardAscendedApexDecisionInput\s*\([^)]*CanEnterBurst",
			"Apex policy input should not keep dead burst actionability data");
		AssertSourceDoesNotMatch(
			source,
			@"\bCanEnterBurst:\s*",
			"BRD Ascended Apex decisions should not receive runtime burst actionability");
		AssertSourceMatches(
			source,
			@"\bprivate\s+bool\s+CanEnterBurstWindow\s*\{.*?if\s*\(\s*!\s*CanBurst\s*\)\s*return\s+false\s*;.*?if\s*\(\s*InBurst\s*\)\s*return\s+true\s*;.*?return\s+CanStartBurstWithRadiantFinale\(out\s+_\)\s*\|\|\s*CanStartBurstWithBattleVoice\(out\s+_\)\s*\|\|\s*CanStartBurstWithRagingStrikes\(out\s+_\)\s*;",
			"BRD Ascended should treat Army's Paeon as non actionable when song-gated buffs cannot start");
		AssertSourceDoesNotMatch(
			pendingRadiantFinaleGate,
			@"\bCanLateWeave\b|\bCanEarlyWeave\b",
			"pending Radiant Finale burst entry should not depend on the current weave slot");
		AssertSourceDoesNotMatch(
			pendingBattleVoiceGate,
			@"\bCanLateWeave\b|\bCanEarlyWeave\b",
			"pending Battle Voice burst entry should not depend on the current weave slot");
		AssertSourceMatches(
			pendingRagingGate,
			@"\bif\s*\(\s*!\s*CanBurst\s*\)\s*return\s+false\s*;.*?return\s+RagingStrikesPvE\.CanUse\(out\s+act\)\s*;",
			"BRD Ascended should allow pending Raging Strikes to keep Apex aligned before the next weave slot");
		AssertSourceDoesNotMatch(
			pendingRagingGate,
			@"\bCanLateWeave\b",
			"pending Raging Strikes burst entry should not depend on the current weave slot");
		AssertSourceMatches(
			tryUseRadiantFinale,
			@"\bif\s*\(\s*Is369\s*&&\s*\(\s*IsFirstCycle\s*\?\s*!\s*CanLateWeave\s*:\s*!\s*CanEarlyWeave\s*\)\s*\)\s*return\s+false\s*;.*?if\s*\(\s*CanStartBurstWithRadiantFinale\(out\s+act\)\s*\)\s*\{.*?MarkDirtyStartRecoveryBurstStarted\s*\(\s*\)\s*;.*?return\s+true\s*;.*?\}.*?return\s+false\s*;",
			"TryUseRadiantFinale should keep 3 6 9 weave timing at the action-use boundary");
		AssertSourceMatches(
			tryUseBattleVoice,
			@"\bif\s*\(\s*UsesStandardBurstPath\s*&&\s*!\s*CanLateWeave\s*\)\s*return\s+false\s*;.*?if\s*\(\s*Is369\s*&&\s*\(\s*IsFirstCycle\s*\?\s*!\s*CanEarlyWeave\s*:\s*!\s*CanLateWeave\s*\)\s*\)\s*return\s+false\s*;.*?if\s*\(\s*CanStartBurstWithBattleVoice\(out\s+act\)\s*\)\s*\{.*?MarkDirtyStartRecoveryBurstStarted\s*\(\s*\)\s*;.*?return\s+true\s*;.*?\}.*?return\s+false\s*;",
			"TryUseBattleVoice should keep weave timing at the action-use boundary");
		AssertSourceMatches(
			tryUseRagingStrikes,
			@"\bif\s*\(\s*!\s*CanLateWeave\s*\)\s*return\s+false\s*;.*?if\s*\(\s*CanStartBurstWithRagingStrikes\(out\s+act\)\s*\)\s*\{.*?MarkDirtyStartRecoveryBurstStarted\s*\(\s*\)\s*;.*?return\s+true\s*;.*?\}.*?return\s+false\s*;",
			"TryUseRagingStrikes should keep current weave timing at the action-use boundary");
	}

	static void BardAscendedApexUsesPlannedKillTimeOverSongFallback()
	{
		AssertTrue(
			ShouldSpendApex(
				BardAscendedSongPhase.ArmysPaeon,
				soulVoice: 80,
				targetSecondsRemaining: 4.96f,
				weaponTotalSeconds: 2.48f),
			"Apex should spend at 80 Soul Voice when planned kill time leaves two GCDs");
		AssertTrue(
			ShouldSpendApex(
				BardAscendedSongPhase.ArmysPaeon,
				soulVoice: 80,
				wouldUseIronJaws: true,
				targetSecondsRemaining: 4.96f,
				weaponTotalSeconds: 2.48f),
			"Apex should spend at 80 Soul Voice over Iron Jaws when planned kill time leaves two GCDs");
		AssertFalse(
			ShouldSpendApex(
				BardAscendedSongPhase.ArmysPaeon,
				soulVoice: 79,
				targetSecondsRemaining: 4.96f,
				weaponTotalSeconds: 2.48f),
			"Apex should not use the two GCD end of fight dump below 80 Soul Voice");
		AssertTrue(
			ShouldSpendApex(
				BardAscendedSongPhase.ArmysPaeon,
				soulVoice: 32,
				targetSecondsRemaining: 2.48f,
				weaponTotalSeconds: 2.48f,
				noFutureBlastPossible: true),
			"Apex should dump at 32 Soul Voice over Burst Shot when only one GCD remains");
		AssertFalse(
			ShouldSpendApex(
				BardAscendedSongPhase.ArmysPaeon,
				soulVoice: 31,
				targetSecondsRemaining: 2.48f,
				weaponTotalSeconds: 2.48f,
				noFutureBlastPossible: true),
			"Apex should hold below the Burst Shot dump threshold");
		AssertTrue(
			ShouldSpendApex(
				BardAscendedSongPhase.ArmysPaeon,
				soulVoice: 40,
				targetSecondsRemaining: 2.48f,
				weaponTotalSeconds: 2.48f,
				wouldUseEnhancedFiller: true,
				noFutureBlastPossible: true),
			"Apex should dump at 40 Soul Voice over enhanced filler when only one GCD remains");
		AssertFalse(
			ShouldSpendApex(
				BardAscendedSongPhase.ArmysPaeon,
				soulVoice: 39,
				targetSecondsRemaining: 2.48f,
				weaponTotalSeconds: 2.48f,
				wouldUseEnhancedFiller: true,
				noFutureBlastPossible: true),
			"Apex should hold below the enhanced filler dump threshold");
		AssertTrue(
			ShouldSpendApex(
				BardAscendedSongPhase.ArmysPaeon,
				soulVoice: 40,
				wouldUseIronJaws: true,
				targetSecondsRemaining: 2.48f,
				weaponTotalSeconds: 2.48f,
				wouldUseEnhancedFiller: true,
				noFutureBlastPossible: true),
			"Apex should dump at 40 Soul Voice over Iron Jaws when no future Blast Arrow is possible");
		AssertFalse(
			ShouldSpendApex(
				BardAscendedSongPhase.ArmysPaeon,
				soulVoice: 40,
				targetSecondsRemaining: 2.48f,
				weaponTotalSeconds: 2.48f,
				wouldUseEnhancedFiller: true,
				noFutureBlastPossible: false),
			"Apex should not dump low Soul Voice when a future Blast Arrow is still possible");
	}

	static void BardAscendedBlastArrowWaitsForUrgentGcds()
	{
		AssertTrue(
			BardAscendedDecisionPolicy.ShouldUseBlastArrow(hasBlastReady: true, wouldUseDots: false, wouldUseIronJaws: false),
			"Blast Arrow should spend when Blast Ready is active and urgent GCDs are clear");
		AssertFalse(
			BardAscendedDecisionPolicy.ShouldUseBlastArrow(hasBlastReady: true, wouldUseDots: true, wouldUseIronJaws: false),
			"urgent DoTs should block Blast Ready spends");
		AssertFalse(
			BardAscendedDecisionPolicy.ShouldUseBlastArrow(hasBlastReady: true, wouldUseDots: false, wouldUseIronJaws: true),
			"Iron Jaws should block Blast Ready spends");
		AssertFalse(
			BardAscendedDecisionPolicy.ShouldUseBlastArrow(hasBlastReady: false, wouldUseDots: false, wouldUseIronJaws: false),
			"Blast Arrow should not spend without Blast Ready");
	}

	static void BardAscendedFillerWaitsForEnhancedFillerOrResonantReady()
	{
		AssertTrue(
			BardAscendedDecisionPolicy.ShouldUseFiller(hasEnhancedFiller: false, hasResonantReady: false),
			"filler should spend when no higher value filler or Resonant Ready is active");
		AssertFalse(
			BardAscendedDecisionPolicy.ShouldUseFiller(hasEnhancedFiller: true, hasResonantReady: false),
			"filler should wait for enhanced filler");
		AssertFalse(
			BardAscendedDecisionPolicy.ShouldUseFiller(hasEnhancedFiller: false, hasResonantReady: true),
			"filler should wait for Resonant Ready");
		AssertFalse(
			BardAscendedDecisionPolicy.ShouldUseFiller(hasEnhancedFiller: true, hasResonantReady: true),
			"filler should wait when both higher value actions are available");
	}

	static void BardAscendedRuntimeFallsBackWhenEnhancedFillerCannotResolve()
	{
		var source = StripSourceComments(File.ReadAllText(RepositoryPath("RotationSolver", "RebornRotations", "Ranged", "BRD_Ascended.cs")));
		var filler = ExtractMethodBody(source, "bool TryUseFiller");

		AssertSourceMatches(
			filler,
			@"\bif\s*\(\s*TryUseEnhancedFiller\s*\(\s*out\s+act\s*\)\s*\)\s*return\s+true\s*;.*?\bBardAscendedDecisionPolicy\.ShouldUseFiller\s*\(\s*hasEnhancedFiller:\s*false\s*,\s*hasResonantReady:\s*HasResonantArrow\s*\).*?\bActiveFiller\.CanUse\s*\(\s*out\s+act\s*,\s*skipComboCheck:\s*true\s*\)",
			"BRD Ascended should let normal filler recover when enhanced filler status exists but the proc action cannot be selected");
		AssertSourceDoesNotMatch(
			filler,
			@"\bBardAscendedDecisionPolicy\.ShouldUseFiller\s*\(\s*CanUseEnhancedFiller\s*,\s*HasResonantArrow\s*\)",
			"BRD Ascended should not block normal filler solely because an enhanced filler status exists");
	}

	static void BardAscendedAoeThresholdsDistinguishGcdAndOgcd()
	{
		AssertFalse(BardAscendedDecisionPolicy.ShouldUseGcdAoE(1), "GCD AoE should reject one target");
		AssertTrue(BardAscendedDecisionPolicy.ShouldUseGcdAoE(2), "GCD AoE should start at two targets");
		AssertFalse(BardAscendedDecisionPolicy.ShouldUseOgcdAoE(1), "oGCD AoE should reject one target");
		AssertTrue(BardAscendedDecisionPolicy.ShouldUseOgcdAoE(2), "oGCD AoE should start at two targets");
	}

	static void BardAscendedFreshDotsYieldToResolvedNormalAoe()
	{
		AssertTrue(
			BardAscendedDecisionPolicy.ShouldFreshDotYieldToNormalAoe(
				new BardAscendedFreshDotAoeInput(
					HasResolvedNormalAoeCandidate: true,
					NormalAoeAffectedTargets: 3,
					TargetSecondsRemaining: 29.99f,
					IsBossTarget: false)),
			"fresh DoTs should yield when normal AoE resolves three targets and the target is not high HP");
		AssertFalse(
			BardAscendedDecisionPolicy.ShouldFreshDotYieldToNormalAoe(
				new BardAscendedFreshDotAoeInput(
					HasResolvedNormalAoeCandidate: true,
					NormalAoeAffectedTargets: 2,
					TargetSecondsRemaining: 29.99f,
					IsBossTarget: false)),
			"fresh DoTs should not yield below the normal AoE override target threshold");
		AssertFalse(
			BardAscendedDecisionPolicy.ShouldFreshDotYieldToNormalAoe(
				new BardAscendedFreshDotAoeInput(
					HasResolvedNormalAoeCandidate: true,
					NormalAoeAffectedTargets: 1,
					TargetSecondsRemaining: 29.99f,
					IsBossTarget: false)),
			"fresh DoTs should not yield for a single resolved normal AoE target");
		AssertFalse(
			BardAscendedDecisionPolicy.ShouldFreshDotYieldToNormalAoe(
				new BardAscendedFreshDotAoeInput(
					HasResolvedNormalAoeCandidate: true,
					NormalAoeAffectedTargets: 3,
					TargetSecondsRemaining: 29.99f,
					IsBossTarget: true)),
			"fresh DoTs should not yield on boss targets");
		AssertFalse(
			BardAscendedDecisionPolicy.ShouldFreshDotYieldToNormalAoe(
				new BardAscendedFreshDotAoeInput(
					HasResolvedNormalAoeCandidate: true,
					NormalAoeAffectedTargets: 3,
					TargetSecondsRemaining: 30f,
					IsBossTarget: false)),
			"fresh DoTs should not yield on high HP non-boss targets");
		AssertFalse(
			BardAscendedDecisionPolicy.ShouldFreshDotYieldToNormalAoe(
				new BardAscendedFreshDotAoeInput(
					HasResolvedNormalAoeCandidate: true,
					NormalAoeAffectedTargets: 3,
					TargetSecondsRemaining: float.NaN,
					IsBossTarget: false)),
			"fresh DoTs should not yield when the target time is unknown");
		AssertFalse(
			BardAscendedDecisionPolicy.ShouldFreshDotYieldToNormalAoe(
				new BardAscendedFreshDotAoeInput(
					HasResolvedNormalAoeCandidate: false,
					NormalAoeAffectedTargets: 8,
					TargetSecondsRemaining: 29.99f,
					IsBossTarget: false)),
			"fresh DoTs should not yield when the normal AoE preview is unresolved");
	}

	static void BardAscendedBloodletterRecoveryForecastsPostSpendCharges()
	{
		var cases = new[]
		{
			(
				Name: "no charge available",
				Input: new BardAscendedBloodletterRecoveryInput
				{
					CurrentCharges = 0,
					MaximumCharges = 3,
					OneChargeRecastTime = 15f,
					RecoveryHorizon = 30f,
				},
				Expected: false),
			(
				Name: "zero recovery horizon",
				Input: new BardAscendedBloodletterRecoveryInput
				{
					CurrentCharges = 1,
					MaximumCharges = 3,
					OneChargeRecastTime = 15f,
					RecoveryHorizon = 0f,
				},
				Expected: false),
			(
				Name: "active tick recovers exactly by horizon",
				Input: new BardAscendedBloodletterRecoveryInput
				{
					CurrentCharges = 2,
					MaximumCharges = 3,
					IsCooldownTicking = true,
					FirstChargeTimeRemaining = 5f,
					OneChargeRecastTime = 15f,
					RecoveryHorizon = 20f,
				},
				Expected: true),
			(
				Name: "active tick misses horizon",
				Input: new BardAscendedBloodletterRecoveryInput
				{
					CurrentCharges = 2,
					MaximumCharges = 3,
					IsCooldownTicking = true,
					FirstChargeTimeRemaining = 5f,
					OneChargeRecastTime = 15f,
					RecoveryHorizon = 19.99f,
				},
				Expected: false),
			(
				Name: "full charge spend starts a fresh recast",
				Input: new BardAscendedBloodletterRecoveryInput
				{
					CurrentCharges = 3,
					MaximumCharges = 3,
					OneChargeRecastTime = 15f,
					RecoveryHorizon = 15f,
				},
				Expected: true),
			(
				Name: "full charge spend cannot recover before short horizon",
				Input: new BardAscendedBloodletterRecoveryInput
				{
					CurrentCharges = 3,
					MaximumCharges = 3,
					OneChargeRecastTime = 15f,
					RecoveryHorizon = 14.99f,
				},
				Expected: false),
		};

		foreach (var testCase in cases)
		{
			AssertEqual(
				testCase.Expected,
				BardAscendedDecisionPolicy.CanRecoverBloodletterChargesAfterSpend(testCase.Input),
				testCase.Name);
		}
	}

	static void BardAscendedRuntimeUsesResolvedAoeTargetCounts()
	{
		var source = StripSourceComments(File.ReadAllText(RepositoryPath("RotationSolver", "RebornRotations", "Ranged", "BRD_Ascended.cs")));
		var enhancedFiller = ExtractMethodBody(source, "bool TryUseEnhancedFiller");
		var enhancedAoeFiller = ExtractMethodBody(source, "bool TryUseEnhancedAoeFiller");
		var aoe = ExtractMethodBody(source, "bool TryUseAoE");
		var normalAoePreview = ExtractMethodBody(source, "bool TryPreviewNormalAoeFiller");
		var normalAoeFiller = ExtractMethodBody(source, "bool TryUseNormalAoeFiller");
		var freshDotYield = ExtractMethodBody(source, "bool ShouldFreshDotYieldToNormalAoe");
		var bloodletterVariant = ExtractMethodBody(source, "bool TryUseBloodletterVariant");

		AssertSourceMatches(
			source,
			@"\bprivate\s+static\s+bool\s+HasMinimumGcdAoETargets\s*\(\s*IAction\?\s+act\s*,\s*int\s+minimumAffectedTargets\s*\)\s*=>\s*act\s+is\s+IBaseAction\s+baseAction\s*&&\s*baseAction\.Target\.AffectedTargets\.Length\s*>=\s*minimumAffectedTargets\s*;.*?\bprivate\s+static\s+bool\s+HasEnoughGcdAoETargets\s*\(\s*IAction\?\s+act\s*\)\s*=>\s*HasMinimumGcdAoETargets\s*\(\s*act\s*,\s*BardAscendedDecisionPolicy\.GcdAoETargets\s*\)\s*;",
			"BRD Ascended should gate GCD AoE by the resolved action affected target count");
		AssertSourceMatches(
			source,
			@"\bprivate\s+static\s+bool\s+HasEnoughOgcdAoETargets\s*\(\s*IAction\?\s+act\s*\)\s*=>\s*act\s+is\s+IBaseAction\s+baseAction\s*&&\s*BardAscendedDecisionPolicy\.ShouldUseOgcdAoE\s*\(\s*baseAction\.Target\.AffectedTargets\.Length\s*\)\s*;",
			"BRD Ascended should gate oGCD AoE by the resolved action affected target count");

		AssertSourceDoesNotMatch(
			enhancedFiller,
			@"\bNumberOfHostilesInRange\b",
			"enhanced filler AoE should not use field hostiles before target resolution");
		AssertSourceDoesNotMatch(
			aoe,
			@"\bNumberOfHostilesInRange\b",
			"GCD AoE should not use field hostiles before target resolution");
		AssertSourceDoesNotMatch(
			freshDotYield,
			@"\bNumberOfHostilesInRange\b|\bAllHostileTargets\b|\bHostileTargets\b",
			"fresh DoT AoE comparison should not use field target counts");
		AssertSourceDoesNotMatch(
			bloodletterVariant,
			@"\bNumberOfHostilesInRange\b",
			"Rain of Death should not use field hostiles before target resolution");

		AssertSourceMatches(
			enhancedAoeFiller,
			@"\bprocAoE\.CanUse\s*\(\s*out\s+var\s+procAoEAct\s*,\s*skipAoeCheck\s*:\s*true\s*,\s*skipComboCheck\s*:\s*true\s*\)\s*&&\s*HasEnoughGcdAoETargets\s*\(\s*procAoEAct\s*\).*?\bact\s*=\s*procAoEAct\s*;",
			"enhanced filler AoE should assign only resolved targets that pass the Ascended GCD AoE threshold");
		AssertSourceMatches(
			aoe,
			@"TryUseEnhancedAoeFiller\s*\(\s*out\s+act\s*\).*?TryUseNormalAoeFiller\s*\(\s*out\s+act\s*\)",
			"GCD AoE should reuse the enhanced AoE helper before normal AoE filler");
		AssertSourceMatches(
			normalAoePreview,
			@"\baoeAction\.CanUse\s*\(\s*out\s+var\s+aoeActionAct\s*,\s*skipAoeCheck\s*:\s*true\s*\).*?\bAffectedTargets:\s*baseAction\.PreviewTarget\.Value\.AffectedTargets\.Length",
			"normal AoE preview should expose the preview affected target count for fresh DoT comparison");
		AssertSourceMatches(
			normalAoePreview,
			@"var\s+wasActionPreview\s*=\s*IBaseAction\.ActionPreview\s*;.*?try\s*\{.*?IBaseAction\.ActionPreview\s*=\s*true\s*;.*?aoeAction\.CanUse\s*\(\s*out\s+var\s+aoeActionAct\s*,\s*skipAoeCheck\s*:\s*true\s*\).*?finally\s*\{.*?IBaseAction\.ActionPreview\s*=\s*wasActionPreview\s*;",
			"normal AoE preview should restore ActionPreview after every probe");
		AssertSourceMatches(
			normalAoeFiller,
			@"normalAoePreview\.AffectedTargets\s*<\s*minimumAffectedTargets.*?aoeAction\.CanUse\s*\(\s*out\s+var\s+aoeActionAct\s*,\s*skipAoeCheck\s*:\s*true\s*\).*?HasMinimumGcdAoETargets\s*\(\s*aoeActionAct\s*,\s*minimumAffectedTargets\s*\).*?act\s*=\s*aoeActionAct",
			"normal AoE commit should recheck the final resolved affected target count");
		AssertSourceMatches(
			freshDotYield,
			@"HasResolvedNormalAoeCandidate:\s*normalAoePreview\.HasResolvedCandidate.*?NormalAoeAffectedTargets:\s*normalAoePreview\.AffectedTargets.*?TargetSecondsRemaining:\s*GetDotTargetTimeToKill\s*\(\s*target\s*\).*?IsBossTarget:\s*IsDotBossTarget\s*\(\s*target\s*\)",
			"fresh DoT AoE comparison should pass resolved AoE and per-DoT target facts into policy");
		AssertSourceMatches(
			bloodletterVariant,
			@"\bRainOfDeathPvE\.CanUse\s*\(\s*out\s+var\s+rainOfDeathAct\s*,\s*usedUp\s*:\s*usedUp\s*,\s*skipAoeCheck\s*:\s*true\s*\)\s*&&\s*HasEnoughOgcdAoETargets\s*\(\s*rainOfDeathAct\s*\).*?\bact\s*=\s*rainOfDeathAct\s*;",
			"Rain of Death should assign only resolved targets that pass the Ascended oGCD AoE threshold");
	}

	static void BardAscendedRuntimeUsesAoeApexAndBlastBeforeFreshDots()
	{
		var source = StripSourceComments(File.ReadAllText(RepositoryPath(
			"RotationSolver",
			"RebornRotations",
			"Ranged",
			"BRD_Ascended.cs")));
		var generalGcd = ExtractMethodBody(source, "GeneralGCD");
		var burst = ExtractMethodBody(source, "bool TryUseBurst");
		var aoeApex = ExtractMethodBody(source, "bool TryUseAoeApexArrow");
		var aoeBlast = ExtractMethodBody(source, "bool TryUseAoeBlastArrow");
		var enhancedAoeFiller = ExtractMethodBody(source, "bool TryUseEnhancedAoeFiller");
		var aoe = ExtractMethodBody(source, "bool TryUseAoE");
		var tryUseDots = ExtractMethodBody(source, "bool TryUseDoTs");
		var policySource = StripSourceComments(File.ReadAllText(RepositoryPath(
			"RotationSolver",
			"RebornRotations",
			"Ranged",
			"BardAscendedDecisionPolicy.cs")));

		AssertSourceMatches(
			generalGcd,
			@"TryUseOpenerGcd\s*\(\s*out\s+act\s*\).*?TryUseIronJaws\s*\(\s*out\s+act\s*\).*?TryUseBurst\s*\(\s*out\s+act\s*\).*?TryUseAoeApexArrow\s*\(\s*out\s+act\s*\).*?TryUseAoeBlastArrow\s*\(\s*out\s+act\s*\).*?TryUseEnhancedAoeFiller\s*\(\s*out\s+act\s*\).*?TryUseDoTs\s*\(\s*out\s+act\s*\).*?TryUseAoE\s*\(\s*out\s+act\s*\).*?TryUseApexArrow\s*\(\s*out\s+act\s*\).*?TryUseBlastArrow\s*\(\s*out\s+act\s*\).*?TryUseResonantArrow\s*\(\s*out\s+act\s*\).*?TryUseFiller\s*\(\s*out\s+act\s*\)",
			"BRD Ascended should keep fresh DoTs before the normal AoE fallback while allowing normal AoE to win inside the fresh DoT branch through policy");
		AssertSourceMatches(
			source,
			@"\bprivate\s+readonly\s+record\s+struct\s+NormalAoePreview\s*\(\s*bool\s+HasResolvedCandidate\s*,\s*int\s+AffectedTargets\s*\)",
			"BRD Ascended should represent unresolved normal AoE preview explicitly");
		AssertSourceMatches(
			tryUseDots,
			@"\bShouldUseStormbiteOnTarget\s*\(\s*stormbiteTarget\s*\).*?if\s*\(\s*ShouldFreshDotYieldToNormalAoe\s*\(\s*stormbiteTarget\s*\)\s*\)\s*\{.*?if\s*\(\s*TryUseNormalAoeFiller\s*\(\s*out\s+act\s*,\s*BardAscendedDecisionPolicy\.NormalAoeFreshDotOverrideTargets\s*\)\s*\)\s*return\s+true\s*;.*?if\s*\(\s*Stormbite\.CanUse\s*\(\s*out\s+act\s*,\s*skipStatusProvideCheck\s*:\s*true\s*\)\s*\)\s*return\s+true\s*;",
			"Stormbite should try normal AoE after a valid fresh DoT preview and fall back to Stormbite if AoE commit rejects");
		AssertSourceMatches(
			tryUseDots,
			@"\bShouldUseCausticBiteOnTarget\s*\(\s*causticTarget\s*\).*?if\s*\(\s*ShouldFreshDotYieldToNormalAoe\s*\(\s*causticTarget\s*\).*?TryUseNormalAoeFiller\s*\(\s*out\s+act\s*,\s*BardAscendedDecisionPolicy\.NormalAoeFreshDotOverrideTargets\s*\).*?return\s+true\s*;.*?return\s+CausticBite\.CanUse\s*\(\s*out\s+act\s*,\s*skipStatusProvideCheck\s*:\s*true\s*\)",
			"Caustic Bite should try normal AoE after a valid fresh DoT preview and fall back to Caustic Bite if AoE commit rejects");
		AssertSourceMatches(
			burst,
			@"TryUseRadiantEncore\s*\(\s*out\s+act\s*\).*?TryUseApexArrow\s*\(\s*out\s+act\s*\).*?TryUseBlastArrow\s*\(\s*out\s+act\s*\).*?TryUseResonantArrow\s*\(\s*out\s+act\s*\).*?TryUseEnhancedFiller\s*\(\s*out\s+act\s*\)",
			"burst GCDs should keep the burst package before returning to the outer priority");
		AssertSourceDoesNotMatch(
			burst,
			@"TryUseIronJaws\s*\(\s*out\s+act\s*\)|TryUseFiller\s*\(\s*out\s+act\s*\)|base\.GeneralGCD\s*\(\s*out\s+act\s*\)",
			"burst GCDs should not fall through to lower priority GCD fallback");
		AssertSourceMatches(
			aoeApex,
			@"SoulVoice\s*<\s*BardAscendedDecisionPolicy\.ApexBlastReadySoulVoice.*?ApexArrowPvE\.CanUse\s*\(\s*out\s+act\s*,\s*skipAoeCheck\s*:\s*true\s*\).*?HasEnoughGcdAoETargets\s*\(\s*act\s*\)",
			"AoE Apex should use resolved Apex targets and the 80 gauge threshold");
		AssertSourceMatches(
			aoeBlast,
			@"IsInSandbagMode.*?BlastArrowPvEReady.*?WouldUseIronJaws.*?BlastArrowPvE\.CanUse\s*\(\s*out\s+act\s*,\s*skipAoeCheck\s*:\s*true\s*,\s*skipComboCheck\s*:\s*true\s*\).*?HasEnoughGcdAoETargets\s*\(\s*act\s*\)",
			"AoE Blast should use resolved Blast targets and keep Iron Jaws protection");
		AssertSourceMatches(
			enhancedAoeFiller,
			@"CanUseEnhancedFiller.*?procAoE\.CanUse\s*\(\s*out\s+var\s+procAoEAct\s*,\s*skipAoeCheck\s*:\s*true\s*,\s*skipComboCheck\s*:\s*true\s*\).*?HasEnoughGcdAoETargets\s*\(\s*procAoEAct\s*\)",
			"enhanced AoE filler should use resolved proc AoE targets before fresh DoTs");
		AssertSourceDoesNotMatch(
			policySource,
			@"BardAscendedApexDecisionInput\s*\([^)]*(AffectedTargets|TargetCount|AoETargets)",
			"Apex policy input should not absorb live resolved target counts");
		AssertSourceDoesNotMatch(
			aoe,
			@"CanUseEnhancedFiller\s*&&\s*!\s*WouldUseDoTs",
			"fresh DoTs should not block enhanced AoE filler on packs");
		AssertSourceDoesNotMatch(
			aoe,
			@"\bvar\s+procAoE\b",
			"normal AoE filler should not carry the enhanced AoE proc branch");
	}

	static void BardAscendedDirtyStartRecoveryOnlyUsesDungeonSongStarts()
	{
		AssertTrue(
			BardAscendedDecisionPolicy.ShouldUseDirtyStartRecovery(
				enablePlannedFightMode: false,
				isFirstCycle: true,
				BardAscendedSongPhase.MagesBallad),
			"Mage's Ballad first-cycle dungeon starts should enter dirty-start recovery");
		AssertTrue(
			BardAscendedDecisionPolicy.ShouldUseDirtyStartRecovery(
				enablePlannedFightMode: false,
				isFirstCycle: true,
				BardAscendedSongPhase.ArmysPaeon),
			"Army's Paeon first-cycle dungeon starts should enter dirty-start recovery");
		AssertFalse(
			BardAscendedDecisionPolicy.ShouldUseDirtyStartRecovery(
				enablePlannedFightMode: true,
				isFirstCycle: true,
				BardAscendedSongPhase.MagesBallad),
			"planned fight mode should preserve strict opener alignment");
		AssertFalse(
			BardAscendedDecisionPolicy.ShouldUseDirtyStartRecovery(
				enablePlannedFightMode: false,
				isFirstCycle: false,
				BardAscendedSongPhase.MagesBallad),
			"later song cycles should not enter dirty-start recovery");
		AssertFalse(
			BardAscendedDecisionPolicy.ShouldUseDirtyStartRecovery(
				enablePlannedFightMode: false,
				isFirstCycle: true,
				BardAscendedSongPhase.WanderersMinuet),
			"Wanderer's Minuet starts should keep normal opener behavior");
		AssertFalse(
			BardAscendedDecisionPolicy.ShouldUseDirtyStartRecovery(
				enablePlannedFightMode: false,
				isFirstCycle: true,
				BardAscendedSongPhase.None),
			"no-song starts should keep normal opener behavior");
	}

	static void BardAscendedRuntimeUsesSupportActionHooks()
	{
		var source = StripSourceComments(File.ReadAllText(RepositoryPath("RotationSolver", "RebornRotations", "Ranged", "BRD_Ascended.cs")));
		var emergencyAbility = ExtractMethodBody(source, "EmergencyAbility");
		var dispelAbility = ExtractMethodBody(source, "DispelAbility");
		var healSingleAbility = ExtractMethodBody(source, "HealSingleAbility");
		var defenseAreaAbility = ExtractMethodBody(source, "DefenseAreaAbility");

		AssertSourceMatches(
			source,
			@"\bprivate\s+bool\s+UseWardenPaeanOnParty\s*\{\s*get;\s*set;\s*\}\s*=\s*true\s*;",
			"BRD Ascended should expose configurable party Warden's Paean usage");
		AssertSourceMatches(
			source,
			@"\bprivate\s+bool\s+PreventDefenseDuringBurst\s*\{\s*get;\s*set;\s*\}\s*=\s*true\s*;",
			"BRD Ascended should expose configurable Troubadour burst protection");
		AssertSourceMatches(
			emergencyAbility,
			@"StatusHelper\.PlayerHasStatus\s*\(\s*false\s*,\s*StatusID\.Doom\s*\).*?TheWardensPaeanPvE\.CanUse\s*\(\s*out\s+act\s*\)\s*\)\s*\{\s*return\s+true\s*;\s*\}\s*if\s*\(\s*TryUseOpenerAbility",
			"BRD Ascended should spend Warden's Paean on self Doom before DPS emergency actions");
		AssertSourceMatches(
			dispelAbility,
			@"UseWardenPaeanOnParty\s*&&\s*TheWardensPaeanPvE\.CanUse\s*\(\s*out\s+act\s*\)\s*\)\s*\{\s*return\s+true\s*;\s*\}\s*return\s+base\.DispelAbility\s*\(\s*nextGCD\s*,\s*out\s+act\s*\)",
			"BRD Ascended should use Warden's Paean from the dispel hook");
		AssertSourceMatches(
			healSingleAbility,
			@"NaturesMinnePvE\.CanUse\s*\(\s*out\s+act\s*\)\s*\)\s*\{\s*return\s+true\s*;\s*\}\s*return\s+base\.HealSingleAbility\s*\(\s*nextGCD\s*,\s*out\s+act\s*\)",
			"BRD Ascended should use Nature's Minne from the single-target heal hook");
		AssertSourceMatches(
			defenseAreaAbility,
			@"\(\s*!\s*PreventDefenseDuringBurst\s*\|\|\s*\(\s*!\s*InBurst\s*&&\s*!\s*IsDirtyStartRecoveryBurstWindow\s*\)\s*\)\s*&&\s*TroubadourPvE\.CanUse\s*\(\s*out\s+act\s*\)\s*\)\s*\{\s*return\s+true\s*;\s*\}\s*return\s+base\.DefenseAreaAbility\s*\(\s*nextGCD\s*,\s*out\s+act\s*\)",
			"BRD Ascended should use Troubadour from the area defense hook while respecting all burst windows");
	}

	static void BardAscendedRuntimeRecoversDirtySongStartsBeforePriority()
	{
		var source = StripSourceComments(File.ReadAllText(RepositoryPath("RotationSolver", "RebornRotations", "Ranged", "BRD_Ascended.cs")));
		var refreshCombatCycle = ExtractMethodBody(source, "void RefreshCombatCycleState");
		var startDirtyRecovery = ExtractMethodBody(source, "void StartDirtyStartRecoveryIfNeeded");
		var clearDirtyRecovery = ExtractMethodBody(source, "void ClearDirtyStartRecoveryIfResolved");
		var tryUseBurst = ExtractMethodBody(source, "bool TryUseBurst");
		var tryUseRadiantEncore = ExtractMethodBody(source, "bool TryUseRadiantEncore");
		var tryUseBarrage = ExtractMethodBody(source, "bool TryUseBarrage");
		var tryUseRadiantFinale = ExtractMethodBody(source, "bool TryUseRadiantFinale");
		var tryUseBattleVoice = ExtractMethodBody(source, "bool TryUseBattleVoice");
		var tryUseRagingStrikes = ExtractMethodBody(source, "bool TryUseRagingStrikes");
		var generalGcd = ExtractMethodBody(source, "bool GeneralGCD");
		var radiantFinaleGate = ExtractMethodBody(source, "bool CanStartBurstWithRadiantFinale");
		var battleVoiceGate = ExtractMethodBody(source, "bool CanStartBurstWithBattleVoice");

		AssertSourceMatches(
			source,
			@"\bprivate\s+enum\s+BardAscendedDirtyStartRecoveryState\s*\{[^}]*\bInactive\b[^}]*\bArmed\b[^}]*\bBurstStarted\b[^}]*\}",
			"BRD Ascended should model dirty-start recovery state explicitly");
		AssertSourceMatches(
			source,
			@"\bprivate\s+BardAscendedDirtyStartRecoveryState\s+_dirtyStartRecoveryState\s*;",
			"BRD Ascended should own the dirty-start recovery state in one field");
		AssertSourceMatches(
			refreshCombatCycle,
			@"StartDirtyStartRecoveryIfNeeded\s*\(\s*\)\s*;.*?if\s*\(\s*!\s*_isStrictOpenerActive\s*&&\s*!\s*IsDirtyStartRecoveryActive\s*\)\s*\{?\s*StartStrictOpener\s*\(\s*\)",
			"dirty-start recovery should run before strict opener can restart");
		AssertSourceMatches(
			startDirtyRecovery,
			@"ShouldUseDirtyStartRecovery\s*\(\s*EnablePlannedFightMode\s*,\s*IsFirstCycle\s*,\s*CurrentSongPhase\s*\).*?_dirtyStartRecoveryState\s*=\s*BardAscendedDirtyStartRecoveryState\.Armed\s*;.*?EndStrictOpener\s*\(\s*\)",
			"dirty-start recovery should be policy-gated and end strict opener tracking");
		AssertSourceMatches(
			clearDirtyRecovery,
			@"if\s*\(\s*!\s*IsDirtyStartRecoveryActive\s*\)\s*return\s*;.*?if\s*\(\s*_dirtyStartRecoveryState\s+is\s+BardAscendedDirtyStartRecoveryState\.Armed\s*\)\s*\{.*?if\s*\(\s*InWanderers\s*\)\s*ResetDirtyStartRecovery\s*\(\s*\).*?return\s*;.*?if\s*\(\s*!\s*PlayerHasAnyDirtyStartRecoveryBurstStatus\s*\(\s*\)\s*&&\s*!\s*WasLastDirtyStartRecoveryBurstAction\s*\(\s*\)\s*\)\s*\{.*?ResetDirtyStartRecovery\s*\(\s*\)",
			"dirty-start recovery should clear on Wanderer's recovery or after the recovered burst window ends");
		AssertSourceMatches(
			radiantFinaleGate,
			@"if\s*\(\s*!\s*InWanderers\s*&&\s*!\s*IsDirtyStartRecoveryActive\s*\)\s*return\s+false\s*;",
			"Radiant Finale should only loosen Wanderer's alignment during dirty-start recovery");
		AssertSourceMatches(
			battleVoiceGate,
			@"if\s*\(\s*!\s*InWanderers\s*&&\s*RadiantFinalePvE\.EnoughLevel\s*&&\s*!\s*IsDirtyStartRecoveryActive\s*\)\s*return\s+false\s*;",
			"Battle Voice should only loosen Wanderer's alignment during dirty-start recovery");
		AssertSourceMatches(
			tryUseRadiantFinale,
			@"CanStartBurstWithRadiantFinale\s*\(\s*out\s+act\s*\).*?MarkDirtyStartRecoveryBurstStarted\s*\(\s*\).*?return\s+true",
			"Radiant Finale should mark the recovered burst window after action selection");
		AssertSourceMatches(
			tryUseBattleVoice,
			@"CanStartBurstWithBattleVoice\s*\(\s*out\s+act\s*\).*?MarkDirtyStartRecoveryBurstStarted\s*\(\s*\).*?return\s+true",
			"Battle Voice should mark the recovered burst window after action selection");
		AssertSourceMatches(
			tryUseRagingStrikes,
			@"CanStartBurstWithRagingStrikes\s*\(\s*out\s+act\s*\).*?MarkDirtyStartRecoveryBurstStarted\s*\(\s*\).*?return\s+true",
			"Raging Strikes should mark the recovered burst window after action selection");
		AssertSourceMatches(
			tryUseBarrage,
			@"if\s*\(\s*IsInSandbagMode\s*\|\|\s*\(\s*!\s*InBurst\s*&&\s*!\s*IsDirtyStartRecoveryBurstWindow\s*\)\s*\)\s*return\s+false\s*;",
			"Barrage should only loosen its burst requirement during the recovered burst window");
		AssertSourceMatches(
			tryUseRadiantEncore,
			@"if\s*\(\s*!\s*HasRadiantFinale\s*&&\s*!\s*CanUseDirtyStartRecoveryRadiantEncore\s*\)\s*return\s+false\s*;.*?if\s*\(\s*!\s*InBurst\s*&&\s*!\s*IsDirtyStartRecoveryBurstWindow\s*\)\s*return\s+false\s*;",
			"Radiant Encore should only loosen its burst requirement during the recovered burst window");
		AssertSourceMatches(
			tryUseBurst,
			@"if\s*\(\s*!\s*InBurst\s*&&\s*!\s*IsDirtyStartRecoveryBurstWindow\s*\)\s*return\s+false\s*;",
			"burst GCD priority should be available during the recovered burst window");
		AssertSourceMatches(
			generalGcd,
			@"\bif\s*\(\s*TryUseResonantArrow\s*\(\s*out\s+act\s*\)\s*\)\s*return\s+true\s*;.*?return\s+TryUseFiller",
			"Resonant Arrow should remain available outside the full InBurst gate");
	}

	static void BardAscendedFirstCycleStartsOnCombatEntryAndTimerReset()
	{
		AssertTrue(
			BardAscendedDecisionPolicy.ShouldStartFirstCycle(
				isInCombat: true,
				hasCombatState: false,
				currentCombatTime: 0.5f,
				previousCombatTime: 0f),
			"first cycle should start when combat begins without countdown state");
		AssertFalse(
			BardAscendedDecisionPolicy.ShouldStartFirstCycle(
				isInCombat: true,
				hasCombatState: true,
				currentCombatTime: 15f,
				previousCombatTime: 10f),
			"first cycle should not restart while combat time advances");
		AssertTrue(
			BardAscendedDecisionPolicy.ShouldStartFirstCycle(
				isInCombat: true,
				hasCombatState: true,
				currentCombatTime: 0.25f,
				previousCombatTime: 120f),
			"first cycle should restart when a new pull resets combat time before an out of combat tick");
		AssertFalse(
			BardAscendedDecisionPolicy.ShouldStartFirstCycle(
				isInCombat: false,
				hasCombatState: true,
				currentCombatTime: 0f,
				previousCombatTime: 120f),
			"first cycle should not start while out of combat");
	}

	static void BardAscendedRuntimeDoesNotCacheLevelSyncedChoices()
	{
		var source = StripSourceComments(File.ReadAllText(RepositoryPath("RotationSolver", "RebornRotations", "Ranged", "BRD_Ascended.cs")));
		var potionCondition = ExtractMethodBody(source, "override bool IsConditionMet");

		AssertSourceDoesNotMatch(
			source,
			@"\bfield\s*\?\?=",
			"BRD Ascended should not cache action choices that depend on EnoughLevel");
		AssertSourceDoesNotMatch(
			source,
			@"\bif\s*\(\s*field\s*!=\s*null\s*\)\s*return\s+field\s*;",
			"BRD Ascended should not reuse stale field-backed action lists after level sync");
		AssertSourceDoesNotMatch(
			source,
			@"\bprivate\s+IBaseAction\[\]\s+DoTActions\b",
			"BRD Ascended should not allocate DoT action arrays in runtime paths");
		AssertSourceDoesNotMatch(
			source,
			@"\bprivate\s+static\s+StatusID\[\]\s+BurstStatus\b",
			"BRD Ascended burst status selection depends on instance action availability");
		AssertSourceDoesNotMatch(
			source,
			@"\bprivate\s+static\s+IBaseAction\s+(ActiveFiller|ActiveBloodletterVariant)\b",
			"BRD Ascended level-synced action choices depend on instance action availability");
		AssertSourceDoesNotMatch(
			source,
			@"\bprivate\s+static\s+bool\s+(HasBurstActions|HasSongActions)\b",
			"BRD Ascended action availability checks depend on instance action availability");
		AssertSourceMatches(
			source,
			@"\bprivate\s+IBaseAction\s+ActiveFiller\s*=>\s*BurstShotPvE\.EnoughLevel\s*\?\s*BurstShotPvE\s*:\s*HeavyShotPvE\s*;",
			"BRD Ascended should select Heavy Shot when Burst Shot is not level-synced");
		AssertSourceMatches(
			source,
			@"\bprivate\s+IBaseAction\s+ActiveBloodletterVariant\s*=>\s*HeartbreakShotPvE\.EnoughLevel\s*\?\s*HeartbreakShotPvE\s*:\s*BloodletterPvE\s*;",
			"BRD Ascended should use one canonical Bloodletter variant for fallback and cooldown checks");
		AssertSourceMatches(
			source,
			@"\bTryUseFirstAvailableSong\s*\(\s*out\s+IAction\?\s+act\s*\).*?MagesBalladPvE\.EnoughLevel.*?MagesBalladPvE\.CanUse\(out\s+act\).*?ArmysPaeonPvE\.EnoughLevel\s*&&\s*ArmysPaeonPvE\.CanUse\(out\s+act\)",
			"BRD Ascended should start the first level-synced song instead of depending on unavailable song cooldowns");
		AssertSourceMatches(
			source,
			@"\bActiveBloodletterVariant\.CanUse\(out\s+act,\s*usedUp:\s*usedUp\)",
			"BRD Ascended prepull and combat Bloodletter paths should use the level-synced active variant");
		AssertSourceMatches(
			source,
			@"\bprivate\s+static\s+readonly\s+BardAscendedPotions\s+AscendedPotions\s*=\s*new\s*\(\s*\)\s*;",
			"BRD Ascended should keep potion config state available during base config discovery");
		AssertSourceDoesNotMatch(
			source,
			@"\bprivate\s+readonly\s+BardAscendedPotions\s+_ascendedPotions\b",
			"BRD Ascended should not put rotation config state behind post-base-constructor instance initialization");
		AssertSourceMatches(
			source,
			@"\bpublic\s+bool\s+ShouldUsePotion\s*\(\s*BRD_Ascended\s+rotation\s*,\s*out\s+IAction\?\s+act\s*,\s*bool\s+clippingCheck\s*=\s*true\s*\)",
			"BRD Ascended potion conditions should receive the active rotation at runtime");
		AssertSourceDoesNotMatch(
			potionCondition,
			@"\bif\s*\(\s*InBurst\s*\)\s*return\s+true\s*;",
			"BRD Ascended nested potion conditions should not read instance burst state without an owner");
		AssertSourceMatches(
			potionCondition,
			@"\bif\s*\(\s*_rotation\?\.InBurst\s*==\s*true\s*\)\s*return\s+true\s*;",
			"BRD Ascended nested potion conditions should read burst state from the active rotation context");
		AssertSourceMatches(
			source,
			@"\bfinally\s*\{.*?_rotation\s*=\s*null\s*;.*?\}",
			"BRD Ascended potion conditions should clear the active rotation after each check");
		AssertSourceDoesNotMatch(
			source,
			@"\bif\s*\(\s*!\s*Is369\s*\|\|\s*!\s*ShouldSwapSong\s*\)\s*return\s+false\s*;",
			"BRD Ascended custom song timing should not be blocked from Army's Paeon");
	}

	static void BardAscendedPotionConfigIsConstructorSafe()
	{
		var source = StripSourceComments(File.ReadAllText(RepositoryPath("RotationSolver", "RebornRotations", "Ranged", "BRD_Ascended.cs")));

		AssertSourceMatches(
			source,
			@"\bprivate\s+static\s+readonly\s+BardAscendedPotions\s+AscendedPotions\s*=\s*new\s*\(\s*\)\s*;",
			"BRD Ascended potion config state should be available before base rotation config discovery runs");
		AssertSourceDoesNotMatch(
			source,
			@"\bprivate\s+readonly\s+BardAscendedPotions\s+_ascendedPotions\b",
			"BRD Ascended should not initialize potion config state after the base constructor reads rotation configs");
		AssertSourceDoesNotMatch(
			source,
			@"\bget\s*=>\s*_ascendedPotions\.",
			"BRD Ascended rotation config getters should not depend on post-base-constructor instance fields");
		AssertSourceMatches(
			source,
			@"\bAscendedPotions\.ShouldUsePotion\s*\(\s*this\s*,\s*out\s+(var\s+)?(?:potionAct|act)\s*\)",
			"BRD Ascended should pass the active rotation when checking potion conditions");
		AssertSourceMatches(
			source,
			@"\bpublic\s+bool\s+ShouldUsePotion\s*\(\s*BRD_Ascended\s+rotation\s*,\s*out\s+IAction\?\s+act\s*,\s*bool\s+clippingCheck\s*=\s*true\s*\)",
			"BRD Ascended potion helper should accept the active rotation during runtime checks");
	}

	static void BardAscendedDefaultsFavorGuideTimings()
	{
		var source = StripSourceComments(File.ReadAllText(RepositoryPath(
			"RotationSolver",
			"RebornRotations",
			"Ranged",
			"BRD_Ascended.cs")));

		AssertSourceMatches(
			source,
			@"private\s+const\s+float\s+ArmyHeartbreakHoldThreshold\s*=\s*35f\s*;",
			"Army's Paeon Heartbreak pooling should begin at 35 seconds remaining");
		AssertSourceMatches(
			source,
			@"public\s+BardAscendedPotionTiming\s+Timing\s*\{\s*get;\s*set;\s*\}\s*=\s*BardAscendedPotionTiming\.Opener\s*;",
			"BRD Ascended should default new potion settings to opener potion usage");
	}

	static void BardAscendedRuntimeSpendsResonantReadyBeforeFiller()
	{
		var source = StripSourceComments(File.ReadAllText(RepositoryPath("RotationSolver", "RebornRotations", "Ranged", "BRD_Ascended.cs")));
		var generalGcd = ExtractMethodBody(source, "GeneralGCD");

		AssertSourceMatches(
			generalGcd,
			@"TryUseBurst\(out\s+act\).*?TryUseAoeApexArrow\(out\s+act\).*?TryUseAoeBlastArrow\(out\s+act\).*?TryUseEnhancedAoeFiller\(out\s+act\).*?TryUseDoTs\(out\s+act\).*?TryUseAoE\(out\s+act\).*?TryUseApexArrow\(out\s+act\).*?TryUseBlastArrow\(out\s+act\).*?TryUseResonantArrow\(out\s+act\).*?TryUseFiller\(out\s+act\)",
			"BRD Ascended should reach Resonant Arrow before filler even when burst is inactive");
	}

	static void BardAscendedRuntimeSpendsPitchPerfectBeforeBurstHold()
	{
		var source = StripSourceComments(File.ReadAllText(RepositoryPath("RotationSolver", "RebornRotations", "Ranged", "BRD_Ascended.cs")));
		var pitchPerfect = ExtractMethodBody(source, "bool TryUsePitchPerfect");

		AssertSourceDoesNotMatch(
			pitchPerfect,
			@"!\s*InBurst\s*&&\s*!\s*RagingStrikesPvE\.Cooldown\.IsCoolingDown",
			"Pitch Perfect should not inherit the pre-stack burst-ready hold");
		AssertSourceMatches(
			pitchPerfect,
			@"\bPitchPerfectPvE\.CanUse\s*\(\s*out\s+act\s*,\s*skipAoeCheck\s*:\s*true\s*,\s*skipComboCheck\s*:\s*true\s*\)",
			"Pitch Perfect should skip AoE and combo checks before evaluating stack safety");
		AssertSourceMatches(
			pitchPerfect,
			@"\bif\s*\(\s*Repertoire\s*==\s*3\s*\)\s*return\s+true\s*;",
			"Pitch Perfect should still spend immediately at three stacks");
	}

	static void BardAscendedCustomTimingFollowsStandardBurstPath()
	{
		AssertTrue(
			BardAscendedDecisionPolicy.UsesStandardBurstPath(BardAscendedSongTiming.Standard),
			"standard timing should use the standard burst path");
		AssertTrue(
			BardAscendedDecisionPolicy.UsesStandardBurstPath(BardAscendedSongTiming.AdjustedStandard),
			"adjusted standard timing should use the standard burst path");
		AssertTrue(
			BardAscendedDecisionPolicy.UsesStandardBurstPath(BardAscendedSongTiming.Custom),
			"custom timing should use the standard burst path with custom song durations");
		AssertFalse(
			BardAscendedDecisionPolicy.UsesStandardBurstPath(BardAscendedSongTiming.Cycle369),
			"3 6 9 timing keeps its dedicated burst path");
	}

	static void BardAscendedBattleVoiceWaitsOnlyForAvailableRadiantFinale()
	{
		AssertTrue(
			BardAscendedDecisionPolicy.ShouldWaitForRadiantFinaleBeforeBattleVoice(
				radiantFinaleEnoughLevel: true,
				radiantFinaleCanUse: true,
				hasRadiantFinale: false,
				wasRadiantFinaleLastAction: false),
			"Battle Voice should wait when Radiant Finale is available but not applied");
		AssertFalse(
			BardAscendedDecisionPolicy.ShouldWaitForRadiantFinaleBeforeBattleVoice(
				radiantFinaleEnoughLevel: false,
				radiantFinaleCanUse: false,
				hasRadiantFinale: false,
				wasRadiantFinaleLastAction: false),
			"Battle Voice should not wait for Radiant Finale below Radiant Finale level");
		AssertFalse(
			BardAscendedDecisionPolicy.ShouldWaitForRadiantFinaleBeforeBattleVoice(
				radiantFinaleEnoughLevel: true,
				radiantFinaleCanUse: false,
				hasRadiantFinale: false,
				wasRadiantFinaleLastAction: false),
			"Battle Voice should not wait when Radiant Finale is unlocked but unavailable");
		AssertFalse(
			BardAscendedDecisionPolicy.ShouldWaitForRadiantFinaleBeforeBattleVoice(
				radiantFinaleEnoughLevel: true,
				radiantFinaleCanUse: true,
				hasRadiantFinale: true,
				wasRadiantFinaleLastAction: false),
			"Battle Voice should not wait after Radiant Finale status is active");
		AssertFalse(
			BardAscendedDecisionPolicy.ShouldWaitForRadiantFinaleBeforeBattleVoice(
				radiantFinaleEnoughLevel: true,
				radiantFinaleCanUse: true,
				hasRadiantFinale: false,
				wasRadiantFinaleLastAction: true),
			"Battle Voice should not wait immediately after Radiant Finale was used");
	}

	static void BardAscendedPotionPresetsMapToExpectedTimings()
	{
		AssertSequenceEqual(
			[0f],
			BardAscendedDecisionPolicy.GetPotionTimings(BardAscendedPotionTiming.Opener, []),
			"opener potion timing should be pull only");
		AssertSequenceEqual(
			[120f, 480f],
			BardAscendedDecisionPolicy.GetPotionTimings(BardAscendedPotionTiming.TwoEight, []),
			"two eight potion timing should mirror the 2 and 8 minute preset");
		AssertSequenceEqual(
			[0f, 360f],
			BardAscendedDecisionPolicy.GetPotionTimings(BardAscendedPotionTiming.ZeroSix, []),
			"zero six potion timing should mirror the opener and 6 minute preset");
		AssertSequenceEqual(
			[0f, 300f, 600f],
			BardAscendedDecisionPolicy.GetPotionTimings(BardAscendedPotionTiming.ZeroFiveTen, []),
			"zero five ten potion timing should mirror the opener, 5 minute, and 10 minute preset");
		AssertSequenceEqual(
			[15f, 180f, 420f],
			BardAscendedDecisionPolicy.GetPotionTimings(BardAscendedPotionTiming.Custom, [15f, 180f, 420f]),
			"custom potion timing should return caller supplied timing arrays");
		AssertSequenceEqual(
			[],
			BardAscendedDecisionPolicy.GetPotionTimings((BardAscendedPotionTiming)99, []),
			"unknown potion timing should fail closed");
	}

	static void BardAscendedCustomPotionTimingsRejectEmptyInput()
	{
		AssertSequenceEqual(
			[],
			BardAscendedDecisionPolicy.GetPotionTimings(BardAscendedPotionTiming.Custom, null!),
			"custom potion timing should reject null timing arrays");
		AssertSequenceEqual(
			[],
			BardAscendedDecisionPolicy.GetPotionTimings(BardAscendedPotionTiming.Custom, []),
			"custom potion timing should reject empty timing arrays");
		AssertSequenceEqual(
			[],
			BardAscendedDecisionPolicy.GetPotionTimings(BardAscendedPotionTiming.Custom, [0f, 0f]),
			"custom potion timing should reject all zero timing arrays");
		AssertSequenceEqual(
			[300f],
			BardAscendedDecisionPolicy.GetPotionTimings(BardAscendedPotionTiming.Custom, [300f, 0f, 0f]),
			"custom potion timing should filter unused zero timing slots");
		AssertSequenceEqual(
			[300f],
			BardAscendedDecisionPolicy.GetPotionTimings(BardAscendedPotionTiming.Custom, [0f, 300f]),
			"custom potion timing should treat zero as an unused custom timing slot");
	}

	static void BardAscendedStrictStandardOpenerEmitsScriptedRequests()
	{
		var state = BardAscendedOpenerState.Start(BardAscendedSongTiming.Standard);

		AssertNextGcd(ref state, BardAscendedOpenerAction.Stormbite, expectedStep: 1);
		AssertNextAbility(ref state, BardAscendedOpenerAction.HeartbreakShot, BardAscendedWeaveSlot.Early, expectedStep: 1);
		AssertNextAbility(ref state, BardAscendedOpenerAction.TheWanderersMinuet, BardAscendedWeaveSlot.Late, expectedStep: 1);
		AssertNextGcd(ref state, BardAscendedOpenerAction.CausticBite, expectedStep: 2);
		AssertNextAbility(ref state, BardAscendedOpenerAction.EmpyrealArrow, BardAscendedWeaveSlot.Early, expectedStep: 2);
		AssertNextAbility(ref state, BardAscendedOpenerAction.RadiantFinale, BardAscendedWeaveSlot.Late, expectedStep: 2);
		AssertNextGcd(ref state, BardAscendedOpenerAction.FlexibleFiller, expectedStep: 3);
		AssertNextAbility(ref state, BardAscendedOpenerAction.BattleVoice, BardAscendedWeaveSlot.Early, expectedStep: 3);
		AssertNextAbility(ref state, BardAscendedOpenerAction.RagingStrikes, BardAscendedWeaveSlot.Late, expectedStep: 3);
		AssertNextGcd(ref state, BardAscendedOpenerAction.FlexibleFiller, expectedStep: 4);
		AssertNextAbility(ref state, BardAscendedOpenerAction.Barrage, BardAscendedWeaveSlot.Early, expectedStep: 4);
		AssertNextGcd(ref state, BardAscendedOpenerAction.RefulgentArrow, expectedStep: 5);
		AssertNextAbility(ref state, BardAscendedOpenerAction.Sidewinder, BardAscendedWeaveSlot.Early, expectedStep: 5);
		AssertNextGcd(ref state, BardAscendedOpenerAction.RadiantEncore, expectedStep: 6);
		AssertNextGcd(ref state, BardAscendedOpenerAction.ResonantArrow, expectedStep: 7);
		AssertNextGcd(ref state, BardAscendedOpenerAction.FlexibleFiller, expectedStep: 8);
		AssertNextAbility(ref state, BardAscendedOpenerAction.EmpyrealArrow, BardAscendedWeaveSlot.Early, expectedStep: 8);
		AssertNextGcd(ref state, BardAscendedOpenerAction.FlexibleFiller, expectedStep: 9);
		AssertNextGcd(ref state, BardAscendedOpenerAction.IronJaws, expectedStep: 10);
		AssertNextGcd(ref state, BardAscendedOpenerAction.FlexibleFiller, expectedStep: 11);
		AssertNextAbility(ref state, BardAscendedOpenerAction.PitchPerfect, BardAscendedWeaveSlot.Early, expectedStep: 11, pitchPerfectStacks: 1, willBurstBuffEndBeforeNextGcd: true);
		AssertComplete(state);
	}

	static void BardAscendedStrictAdjustedOpenerEmitsScriptedRequests()
	{
		var state = BardAscendedOpenerState.Start(BardAscendedSongTiming.AdjustedStandard);

		AssertNextAbility(ref state, BardAscendedOpenerAction.HeartbreakShot, BardAscendedWeaveSlot.Prepull, expectedStep: 0);
		AssertNextGcd(ref state, BardAscendedOpenerAction.Stormbite, expectedStep: 1);
		AssertNextAbility(ref state, BardAscendedOpenerAction.TheWanderersMinuet, BardAscendedWeaveSlot.Early, expectedStep: 1);
		AssertNextAbility(ref state, BardAscendedOpenerAction.EmpyrealArrow, BardAscendedWeaveSlot.Late, expectedStep: 1);
		AssertNextGcd(ref state, BardAscendedOpenerAction.CausticBite, expectedStep: 2);
		AssertNextAbility(ref state, BardAscendedOpenerAction.Potion, BardAscendedWeaveSlot.Early, expectedStep: 2);
		AssertNextAbility(ref state, BardAscendedOpenerAction.BattleVoice, BardAscendedWeaveSlot.Late, expectedStep: 2);
		AssertNextGcd(ref state, BardAscendedOpenerAction.FlexibleFiller, expectedStep: 3);
		AssertNextAbility(ref state, BardAscendedOpenerAction.RadiantFinale, BardAscendedWeaveSlot.Early, expectedStep: 3);
		AssertNextAbility(ref state, BardAscendedOpenerAction.RagingStrikes, BardAscendedWeaveSlot.Late, expectedStep: 3);
		AssertNextGcd(ref state, BardAscendedOpenerAction.FlexibleFiller, expectedStep: 4);
		AssertNextAbility(ref state, BardAscendedOpenerAction.Barrage, BardAscendedWeaveSlot.Early, expectedStep: 4);
		AssertNextGcd(ref state, BardAscendedOpenerAction.RefulgentArrow, expectedStep: 5);
		AssertNextAbility(ref state, BardAscendedOpenerAction.Sidewinder, BardAscendedWeaveSlot.Early, expectedStep: 5);
		AssertNextGcd(ref state, BardAscendedOpenerAction.RadiantEncore, expectedStep: 6);
		AssertNextGcd(ref state, BardAscendedOpenerAction.ResonantArrow, expectedStep: 7);
		AssertNextGcd(ref state, BardAscendedOpenerAction.FlexibleFiller, expectedStep: 8);
		AssertNextAbility(ref state, BardAscendedOpenerAction.EmpyrealArrow, BardAscendedWeaveSlot.Early, expectedStep: 8);
		AssertNextGcd(ref state, BardAscendedOpenerAction.FlexibleFiller, expectedStep: 9);
		AssertNextGcd(ref state, BardAscendedOpenerAction.IronJaws, expectedStep: 10);
		AssertNextGcd(ref state, BardAscendedOpenerAction.FlexibleFiller, expectedStep: 11);
		AssertNextAbility(ref state, BardAscendedOpenerAction.PitchPerfect, BardAscendedWeaveSlot.Early, expectedStep: 11, pitchPerfectStacks: 1, willBurstBuffEndBeforeNextGcd: true);
		AssertComplete(state);
	}

	static void BardAscendedStrict369OpenerEmitsScriptedRequests()
	{
		var state = BardAscendedOpenerState.Start(BardAscendedSongTiming.Cycle369);

		AssertNextGcd(ref state, BardAscendedOpenerAction.Stormbite, expectedStep: 1);
		AssertNextAbility(ref state, BardAscendedOpenerAction.HeartbreakShot, BardAscendedWeaveSlot.Early, expectedStep: 1);
		AssertNextAbility(ref state, BardAscendedOpenerAction.TheWanderersMinuet, BardAscendedWeaveSlot.Late, expectedStep: 1);
		AssertNextGcd(ref state, BardAscendedOpenerAction.CausticBite, expectedStep: 2);
		AssertNextAbility(ref state, BardAscendedOpenerAction.Potion, BardAscendedWeaveSlot.Early, expectedStep: 2);
		AssertNextAbility(ref state, BardAscendedOpenerAction.RadiantFinale, BardAscendedWeaveSlot.Late, expectedStep: 2);
		AssertNextGcd(ref state, BardAscendedOpenerAction.FlexibleFiller, expectedStep: 3);
		AssertNextAbility(ref state, BardAscendedOpenerAction.BattleVoice, BardAscendedWeaveSlot.Early, expectedStep: 3);
		AssertNextGcd(ref state, BardAscendedOpenerAction.FlexibleFiller, expectedStep: 4);
		AssertNextAbility(ref state, BardAscendedOpenerAction.RagingStrikes, BardAscendedWeaveSlot.Early, expectedStep: 4);
		AssertNextAbility(ref state, BardAscendedOpenerAction.EmpyrealArrow, BardAscendedWeaveSlot.Late, expectedStep: 4);
		AssertNextGcd(ref state, BardAscendedOpenerAction.RadiantEncore, expectedStep: 5);
		AssertNextAbility(ref state, BardAscendedOpenerAction.Barrage, BardAscendedWeaveSlot.Early, expectedStep: 5);
		AssertNextGcd(ref state, BardAscendedOpenerAction.RefulgentArrow, expectedStep: 6);
		AssertNextAbility(ref state, BardAscendedOpenerAction.Sidewinder, BardAscendedWeaveSlot.Early, expectedStep: 6);
		AssertNextGcd(ref state, BardAscendedOpenerAction.ResonantArrow, expectedStep: 7);
		AssertNextGcd(ref state, BardAscendedOpenerAction.FlexibleFiller, expectedStep: 8);
		AssertNextGcd(ref state, BardAscendedOpenerAction.FlexibleFiller, expectedStep: 9);
		AssertNextGcd(ref state, BardAscendedOpenerAction.IronJaws, expectedStep: 10);
		AssertNextAbility(ref state, BardAscendedOpenerAction.EmpyrealArrow, BardAscendedWeaveSlot.Early, expectedStep: 10);
		AssertNextGcd(ref state, BardAscendedOpenerAction.FlexibleFiller, expectedStep: 11);
		AssertNextAbility(ref state, BardAscendedOpenerAction.PitchPerfect, BardAscendedWeaveSlot.Early, expectedStep: 11, pitchPerfectStacks: 1, willBurstBuffEndBeforeNextGcd: true);
		AssertComplete(state);
	}

	static void BardAscendedStrictOpenerPreservesPitchPerfectSafety()
	{
		var safetyState = new BardAscendedOpenerState(
			BardAscendedSongTiming.Standard,
			Step: 3,
			NextGcdIndex: 3,
			NextWeaveSlot: BardAscendedWeaveSlot.Early,
			IsTerminal: false);

		var threeStackSafety = BardAscendedOpenerController.GetNextRequest(BardAscendedOpenerInput.ForAbility(
			safetyState,
			pitchPerfectStacks: 3,
			willGainPitchPerfectStackBeforeNextWeave: true));

		AssertEqual(BardAscendedOpenerResultKind.Continue, threeStackSafety.Kind, "three stack safety should request Pitch Perfect");
		AssertEqual(BardAscendedOpenerAction.PitchPerfect, threeStackSafety.Action, "three stack safety should spend Pitch Perfect");
		AssertEqual(safetyState, threeStackSafety.NextState, "three stack safety should not advance opener state");

		var twoStackSafety = BardAscendedOpenerController.GetNextRequest(BardAscendedOpenerInput.ForAbility(
			safetyState,
			pitchPerfectStacks: 2,
			isEmpyrealArrowNextScriptedAbility: true));

		AssertEqual(BardAscendedOpenerResultKind.Continue, twoStackSafety.Kind, "two stack Empyreal Arrow safety should request Pitch Perfect");
		AssertEqual(BardAscendedOpenerAction.PitchPerfect, twoStackSafety.Action, "two stack Empyreal Arrow safety should spend Pitch Perfect");
		AssertEqual(safetyState, twoStackSafety.NextState, "two stack Empyreal Arrow safety should not advance opener state");

		var twoStackTickSafety = BardAscendedOpenerController.GetNextRequest(BardAscendedOpenerInput.ForAbility(
			safetyState,
			pitchPerfectStacks: 2,
			willGainPitchPerfectStackBeforeNextWeave: true));

		AssertEqual(BardAscendedOpenerResultKind.Continue, twoStackTickSafety.Kind, "two stack song tick safety should request Pitch Perfect");
		AssertEqual(BardAscendedOpenerAction.PitchPerfect, twoStackTickSafety.Action, "two stack song tick safety should spend Pitch Perfect");
		AssertEqual(safetyState, twoStackTickSafety.NextState, "two stack song tick safety should not advance opener state");

		var burstEndSafety = BardAscendedOpenerController.GetNextRequest(BardAscendedOpenerInput.ForAbility(
			safetyState,
			pitchPerfectStacks: 1,
			willBurstBuffEndBeforeNextGcd: true));

		AssertEqual(BardAscendedOpenerResultKind.Continue, burstEndSafety.Kind, "burst end safety should request Pitch Perfect");
		AssertEqual(BardAscendedOpenerAction.PitchPerfect, burstEndSafety.Action, "burst end safety should spend Pitch Perfect");
		AssertEqual(safetyState, burstEndSafety.NextState, "burst end safety should not advance opener state");

		var dumpState = new BardAscendedOpenerState(
			BardAscendedSongTiming.Standard,
			Step: 11,
			NextGcdIndex: 12,
			NextWeaveSlot: BardAscendedWeaveSlot.Early,
			IsTerminal: false);

		var zeroStackDump = BardAscendedOpenerController.GetNextRequest(BardAscendedOpenerInput.ForAbility(
			dumpState,
			pitchPerfectStacks: 0));

		AssertEqual(BardAscendedOpenerResultKind.Skip, zeroStackDump.Kind, "zero stack Pitch Perfect dump should skip");
		AssertEqual(BardAscendedOpenerRequestKind.None, zeroStackDump.RequestKind, "skipped dump should not request an action kind");
		AssertEqual(BardAscendedOpenerAction.None, zeroStackDump.Action, "skipped dump should not request an action");
		AssertEqual(BardAscendedWeaveSlot.None, zeroStackDump.NextState.NextWeaveSlot, "skipped dump should clear the pending weave slot");

		var oneStackHold = BardAscendedOpenerController.GetNextRequest(BardAscendedOpenerInput.ForAbility(
			dumpState,
			pitchPerfectStacks: 1));

		AssertEqual(BardAscendedOpenerResultKind.Skip, oneStackHold.Kind, "one stack Pitch Perfect dump should skip when no burst or song buff is ending");
		AssertEqual(BardAscendedOpenerAction.None, oneStackHold.Action, "one stack hold should not request Pitch Perfect");
		AssertEqual(BardAscendedWeaveSlot.None, oneStackHold.NextState.NextWeaveSlot, "one stack hold should clear the pending dump slot");

		var oneStackDump = BardAscendedOpenerController.GetNextRequest(BardAscendedOpenerInput.ForAbility(
			dumpState,
			pitchPerfectStacks: 1,
			willBurstBuffEndBeforeNextGcd: true));

		AssertEqual(BardAscendedOpenerResultKind.Continue, oneStackDump.Kind, "one stack Pitch Perfect dump should continue");
		AssertEqual(BardAscendedOpenerAction.PitchPerfect, oneStackDump.Action, "one stack Pitch Perfect dump should request Pitch Perfect");
	}

	static void BardAscendedStrictOpenerCompletesAndBreaksExplicitly()
	{
		var completeState = new BardAscendedOpenerState(
			BardAscendedSongTiming.Standard,
			Step: 12,
			NextGcdIndex: 12,
			NextWeaveSlot: BardAscendedWeaveSlot.None,
			IsTerminal: false);

		var complete = BardAscendedOpenerController.GetNextRequest(BardAscendedOpenerInput.ForGcd(completeState));

		AssertEqual(BardAscendedOpenerResultKind.Complete, complete.Kind, "exhausted opener should complete");
		AssertTrue(complete.NextState.IsTerminal, "complete result should mark terminal state");

		var blockedGcd = BardAscendedOpenerController.GetNextRequest(BardAscendedOpenerInput.ForGcd(
			BardAscendedOpenerState.Start(BardAscendedSongTiming.Standard),
			canUseRequestedAction: false));

		AssertEqual(BardAscendedOpenerResultKind.Break, blockedGcd.Kind, "unusable required GCD should break the opener");
		AssertTrue(blockedGcd.NextState.IsTerminal, "blocked GCD should mark terminal state");

		var pendingPrepull = new BardAscendedOpenerState(
			BardAscendedSongTiming.AdjustedStandard,
			Step: 0,
			NextGcdIndex: 1,
			NextWeaveSlot: BardAscendedWeaveSlot.Prepull,
			IsTerminal: false);

		var prematureGcd = BardAscendedOpenerController.GetNextRequest(BardAscendedOpenerInput.ForGcd(pendingPrepull));

		AssertEqual(BardAscendedOpenerResultKind.Break, prematureGcd.Kind, "GCD request should break while a required weave is pending");
		AssertTrue(prematureGcd.NextState.IsTerminal, "pending required weave break should mark terminal state");
	}

	static void BardAscendedStrictOpenerDelaysAdjustedPrepullHeartbreakUntilPull()
	{
		var request = BardAscendedOpenerController.GetNextRequest(BardAscendedOpenerInput.ForAbility(
			BardAscendedOpenerState.Start(BardAscendedSongTiming.AdjustedStandard)));

		AssertEqual(BardAscendedOpenerAction.HeartbreakShot, request.Action, "adjusted standard prepull should request Heartbreak Shot");
		AssertEqual(BardAscendedWeaveSlot.Prepull, request.WeaveSlot, "adjusted standard Heartbreak Shot should be a prepull request");

		var early = BardAscendedOpenerController.IsCountdownPrepullRequestReady(BardAscendedSongTiming.AdjustedStandard, request, 0.5f);
		var atPull = BardAscendedOpenerController.IsCountdownPrepullRequestReady(BardAscendedSongTiming.AdjustedStandard, request, 0f);

		AssertFalse(early, "adjusted standard prepull Heartbreak Shot should not fire before countdown zero");
		AssertTrue(atPull, "adjusted standard prepull Heartbreak Shot should fire at countdown zero");
	}

	static void BardAscendedStrictOpenerHoldsPullGcdUntilAdjustedPrepullHeartbreakReady()
	{
		var request = BardAscendedOpenerController.GetNextRequest(BardAscendedOpenerInput.ForAbility(
			BardAscendedOpenerState.Start(BardAscendedSongTiming.AdjustedStandard)));

		AssertTrue(BardAscendedOpenerController.HasPendingCountdownPrepullRequest(request), "adjusted standard Heartbreak Shot should remain pending before pull");
		AssertFalse(
			BardAscendedOpenerController.IsCountdownPrepullRequestReady(BardAscendedSongTiming.AdjustedStandard, request, 0.05f),
			"adjusted standard Heartbreak Shot should not be ready in the final pre-pull DoT window");

		var source = StripSourceComments(File.ReadAllText(RepositoryPath("RotationSolver", "RebornRotations", "Ranged", "BRD_Ascended.cs")));
		var openerCountdownAction = ExtractMethodBody(source, "bool TryUseOpenerCountdownAction");

		AssertSourceMatches(
			openerCountdownAction,
			@"\bvar\s+hasPendingPrepull\s*=\s*BardAscendedOpenerController\.HasPendingCountdownPrepullRequest\s*\(\s*abilityRequest\s*\)\s*;.*?IsCountdownPrepullRequestReady\s*\(\s*SongTimings\s*,\s*abilityRequest\s*,\s*remainTime\s*\).*?TryUseRequestedOpenerAction\s*\(\s*abilityRequest\s*,\s*out\s+act\s*\).*?\bif\s*\(\s*hasPendingPrepull\s*\)\s*return\s+false\s*;.*?\bvar\s+gcdRequest\s*=",
			"BRD Ascended should not request pull GCDs while a prepull opener ability is still pending");
	}

	static void AssertNextGcd(ref BardAscendedOpenerState state, BardAscendedOpenerAction expectedAction, int expectedStep)
	{
		AssertEqual(expectedStep, state.Step, "opener GCD request should be emitted at the expected step");

		var result = BardAscendedOpenerController.GetNextRequest(BardAscendedOpenerInput.ForGcd(state));

		AssertEqual(BardAscendedOpenerResultKind.Continue, result.Kind, "opener GCD should continue");
		AssertEqual(BardAscendedOpenerRequestKind.Gcd, result.RequestKind, "opener GCD should request a GCD");
		AssertEqual(expectedAction, result.Action, "opener GCD should request the scripted action");
		AssertEqual(BardAscendedWeaveSlot.None, result.WeaveSlot, "opener GCD should not occupy a weave slot");
		AssertFalse(result.NextState.IsTerminal, "opener GCD should leave the opener active");

		state = result.NextState;
	}

	static void AssertNextAbility(
		ref BardAscendedOpenerState state,
		BardAscendedOpenerAction expectedAction,
		BardAscendedWeaveSlot expectedSlot,
		int expectedStep,
		int pitchPerfectStacks = 0,
		bool willBurstBuffEndBeforeNextGcd = false)
	{
		AssertEqual(expectedStep, state.Step, "opener ability request should be emitted at the expected step");
		AssertEqual(expectedSlot, state.NextWeaveSlot, "opener ability request should be emitted in the expected weave slot");

		var result = BardAscendedOpenerController.GetNextRequest(BardAscendedOpenerInput.ForAbility(
			state,
			pitchPerfectStacks: pitchPerfectStacks,
			willBurstBuffEndBeforeNextGcd: willBurstBuffEndBeforeNextGcd));

		AssertEqual(BardAscendedOpenerResultKind.Continue, result.Kind, "opener ability should continue");
		AssertEqual(BardAscendedOpenerRequestKind.Ability, result.RequestKind, "opener ability should request an ability");
		AssertEqual(expectedAction, result.Action, "opener ability should request the scripted action");
		AssertEqual(expectedSlot, result.WeaveSlot, "opener ability should preserve the requested weave slot");
		AssertFalse(result.NextState.IsTerminal, "opener ability should leave the opener active");

		state = result.NextState;
	}

	static void AssertComplete(BardAscendedOpenerState state)
	{
		var result = BardAscendedOpenerController.GetNextRequest(BardAscendedOpenerInput.ForGcd(state));

		AssertEqual(BardAscendedOpenerResultKind.Complete, result.Kind, "opener should complete after the final scripted GCD");
		AssertTrue(result.NextState.IsTerminal, "opener complete result should mark terminal state");
	}

	static void BardAscendedRuntimeEntersStrictOpenerBeforePriorityGcds()
	{
		var source = StripSourceComments(File.ReadAllText(RepositoryPath("RotationSolver", "RebornRotations", "Ranged", "BRD_Ascended.cs")));
		var generalGcd = ExtractMethodBody(source, "GeneralGCD");

		AssertSourceMatches(
			generalGcd,
			@"TryUseOpenerGcd\s*\(\s*out\s+act\s*\).*?TryUseIronJaws\s*\(\s*out\s+act\s*\).*?TryUseBurst\s*\(\s*out\s+act\s*\).*?TryUseAoeApexArrow\s*\(\s*out\s+act\s*\).*?TryUseAoeBlastArrow\s*\(\s*out\s+act\s*\).*?TryUseEnhancedAoeFiller\s*\(\s*out\s+act\s*\).*?TryUseDoTs\s*\(\s*out\s+act\s*\)",
			"BRD Ascended should attempt strict opener GCD before normal priority GCDs");
	}

	static void BardAscendedRuntimeEntersStrictOpenerDuringCountdown()
	{
		var source = StripSourceComments(File.ReadAllText(RepositoryPath("RotationSolver", "RebornRotations", "Ranged", "BRD_Ascended.cs")));
		var countDownAction = ExtractMethodBody(source, "CountDownAction");
		var resetCountdown = ExtractMethodBody(source, "void ResetStrictOpenerForCountdown");
		var countdownPotionFallback = ExtractMethodBody(source, "bool ShouldUseCountdownPotionFallback");
		var openerCountdownAction = ExtractMethodBody(source, "bool TryUseOpenerCountdownAction");
		var refreshCombatCycle = ExtractMethodBody(source, "void RefreshCombatCycleState");

		AssertSourceMatches(
			countDownAction,
			@"\bResetStrictOpenerForCountdown\s*\(\s*remainTime\s*\)\s*;.*?\bStartStrictOpenerForCountdown\s*\(\s*\)\s*;.*?\bif\s*\(\s*TryUseOpenerCountdownAction\s*\(\s*remainTime\s*,\s*out\s+var\s+openerAct\s*\)\s*\)\s*return\s+openerAct\s*;.*?\bShouldUseCountdownPotionFallback\s*\(\s*\)\s*&&\s*AscendedPotions\.ShouldUsePotion",
			"BRD Ascended should attempt strict opener countdown actions before legacy countdown fallbacks");
		AssertSourceMatches(
			resetCountdown,
			@"\bvar\s+isNewCountdown\s*=\s*remainTime\s*>\s*_lastCountdownRemainTime\s*\+\s*CountdownResetToleranceSeconds\s*;.*?_lastCountdownRemainTime\s*=\s*remainTime\s*;.*?if\s*\(\s*_isStrictOpenerActive\s*&&\s*!\s*isNewCountdown\s*\)\s*return\s*;.*?ResetStrictOpenerProgress\s*\(\s*\)\s*;",
			"BRD Ascended should reset stale active opener state for a fresh countdown before starting prepull actions");
		AssertSourceMatches(
			openerCountdownAction,
			@"\bvar\s+abilityRequest\s*=\s*BardAscendedOpenerController\.GetNextRequest\s*\(\s*BuildOpenerAbilityInput\s*\(\s*\)\s*\)\s*;.*?BardAscendedOpenerController\.IsCountdownPrepullRequestReady\s*\(\s*SongTimings\s*,\s*abilityRequest\s*,\s*remainTime\s*\).*?TryUseRequestedOpenerAction\s*\(\s*abilityRequest\s*,\s*out\s+act\s*\).*?\bvar\s+gcdRequest\s*=\s*BardAscendedOpenerController\.GetNextRequest\s*\(\s*BuildOpenerGcdInput\s*\(\s*\)\s*\)\s*;.*?TryUseRequestedOpenerAction\s*\(\s*gcdRequest\s*,\s*out\s+act\s*\)",
			"BRD Ascended countdown should advance strict opener state for pull GCDs instead of using legacy DoT fallback");
		AssertSourceMatches(
			countDownAction,
			@"return\s+!\s*_isStrictOpenerActive\s*&&\s*remainTime\s*<=\s*CountdownDotWindowSeconds\s*&&\s*TryUseDoTs\s*\(\s*out\s+act\s*\)",
			"BRD Ascended should not use legacy countdown DoTs while strict opener state is active");
		AssertSourceMatches(
			refreshCombatCycle,
			@"\bvar\s+hadCombatCycleState\s*=\s*HasCombatCycleState\s*;.*?HasCombatCycleState\s*=\s*false\s*;.*?if\s*\(\s*hadCombatCycleState\s*&&\s*Service\.CountDownTime\s*<=\s*0f\s*\)\s*\{.*?ResetStrictOpenerTracking\s*\(\s*\)\s*;.*?\}",
			"BRD Ascended should not clear countdown opener state before combat starts");
		AssertSourceMatches(
			countdownPotionFallback,
			@"return\s+SongTimings\s+is\s+BardAscendedSongTiming\.Standard\s+or\s+BardAscendedSongTiming\.Custom\s*;",
			"BRD Ascended should reserve non-standard opener potion slots for the strict opener script");
		AssertSourceMatches(
			countDownAction,
			@"!\s*_isStrictOpenerActive\s*&&\s*SongTimings\s*==\s*BardAscendedSongTiming\.AdjustedStandard\s*&&\s*remainTime\s*<=\s*BardAscendedOpenerController\.AdjustedStandardPrepullHeartbreakWindowSeconds",
			"BRD Ascended should gate legacy adjusted prepull Heartbreak fallback off during strict opener mode");
		AssertSourceMatches(
			countDownAction,
			@"!\s*_isStrictOpenerActive\s*&&\s*Is369\s*&&\s*EnablePrepullHeartbreakShot\s*&&\s*remainTime\s*<\s*Cycle369PrepullHeartbreakWindowSeconds",
			"BRD Ascended should gate legacy 3 6 9 prepull Heartbreak fallback off during strict opener mode");
	}

	static void BardAscendedRuntimeEntersStrictOpenerBeforePriorityAbilities()
	{
		var source = StripSourceComments(File.ReadAllText(RepositoryPath("RotationSolver", "RebornRotations", "Ranged", "BRD_Ascended.cs")));
		var emergencyAbility = ExtractMethodBody(source, "EmergencyAbility");
		var attackAbility = ExtractMethodBody(source, "AttackAbility");

		AssertSourceMatches(
			emergencyAbility,
			@"\bif\s*\(\s*TryUseOpenerAbility\s*\(\s*out\s+act\s*\)\s*\)\s*return\s+true\s*;.*?\bAscendedPotions\.ShouldUsePotion\s*\(\s*this\s*,\s*out\s+act\s*\).*?\bTryUseEmpyrealArrow\s*\(\s*out\s+act\s*\).*?\bTryUseBarrage\s*\(\s*out\s+act\s*\).*?\bTryUsePitchPerfect\s*\(\s*out\s+act\s*\)",
			"BRD Ascended should attempt strict opener emergency abilities before normal emergency priority");
		AssertSourceMatches(
			attackAbility,
			@"\bif\s*\(\s*TryUseOpenerAbility\s*\(\s*out\s+act\s*\)\s*\)\s*return\s+true\s*;.*?\bTryUseRadiantFinale\s*\(\s*out\s+act\s*\).*?\bTryUseBattleVoice\s*\(\s*out\s+act\s*\).*?\bTryUseRagingStrikes\s*\(\s*out\s+act\s*\).*?\bTryUseHeartBreakShot\s*\(\s*out\s+act\s*\).*?\bTryUseSideWinder\s*\(\s*out\s+act\s*\)",
			"BRD Ascended should attempt strict opener attack abilities before normal attack priority");
	}

	static void BardAscendedBloodletterUsesLiberalSpendingWithBurstReservation()
	{
		var source = StripSourceComments(File.ReadAllText(RepositoryPath("RotationSolver", "RebornRotations", "Ranged", "BRD_Ascended.cs")));
		var policySource = StripSourceComments(File.ReadAllText(RepositoryPath("RotationSolver", "RebornRotations", "Ranged", "BardAscendedDecisionPolicy.cs")));
		var emergencyAbility = ExtractMethodBody(source, "EmergencyAbility");
		var attackAbility = ExtractMethodBody(source, "AttackAbility");
		var tryUseHeartbreak = ExtractMethodBody(source, "bool TryUseHeartBreakShot");
		var reservationActive = ExtractMethodBody(source, "bool IsBloodletterBurstReservationActive");
		var reservationHorizon = ExtractMethodBody(source, "float GetBloodletterBurstEntryHorizon");
		var chargeForecast = ExtractMethodBody(policySource, "bool CanRecoverBloodletterChargesAfterSpend");

		AssertSourceDoesNotMatch(
			tryUseHeartbreak,
			@"\b(isInWanderersHold|isInMagesHold|isEmpyrealBlocking|holdForRagingOrCap)\b",
			"BRD Ascended should remove broad non-reservation Bloodletter holds");
		AssertSourceMatches(
			reservationActive,
			@"return\s+CanEnterBurstWindow\s*\|\|\s*\(\s*InArmys\s*&&\s*SongTime\s*<=\s*ArmyHeartbreakHoldThreshold\s*\)\s*;",
			"Bloodletter reservation should be active only for immediate burst entry or Army's Paeon pooling");
		AssertSourceMatches(
			reservationHorizon,
			@"if\s*\(\s*CanEnterBurstWindow\s*\)\s*return\s+0f\s*;.*?return\s+InArmys\s*&&\s*SongTime\s*<=\s*ArmyHeartbreakHoldThreshold\s*\?\s*Math\.Max\s*\(\s*0f\s*,\s*SongTime\s*-\s*ArmyRemainTime\s*\)\s*:\s*0f\s*;",
			"Bloodletter reservation horizon should use the planned Army song swap point");
		AssertSourceMatches(
			policySource,
			@"internal\s+readonly\s+record\s+struct\s+BardAscendedBloodletterRecoveryInput\s*\{.*?int\s+CurrentCharges\s*\{\s*get;\s*init;\s*\}.*?int\s+MaximumCharges\s*\{\s*get;\s*init;\s*\}.*?bool\s+IsCooldownTicking\s*\{\s*get;\s*init;\s*\}.*?float\s+FirstChargeTimeRemaining\s*\{\s*get;\s*init;\s*\}.*?float\s+OneChargeRecastTime\s*\{\s*get;\s*init;\s*\}.*?float\s+RecoveryHorizon\s*\{\s*get;\s*init;\s*\}",
			"Bloodletter recovery inputs should be grouped to avoid primitive parameter ordering");
		AssertSourceMatches(
			tryUseHeartbreak,
			@"BardAscendedDecisionPolicy\.CanRecoverBloodletterChargesAfterSpend\s*\(\s*new\s+BardAscendedBloodletterRecoveryInput\s*\{.*?CurrentCharges\s*=\s*cooldown\.CurrentCharges.*?MaximumCharges\s*=\s*BloodletterMax.*?IsCooldownTicking\s*=\s*cooldown\.IsCoolingDown.*?FirstChargeTimeRemaining\s*=\s*cooldown\.RecastTimeRemainOneCharge.*?OneChargeRecastTime\s*=\s*cooldown\.RecastTimeOneChargeRaw.*?RecoveryHorizon\s*=\s*GetBloodletterBurstEntryHorizon\s*\(\s*\)",
			"Bloodletter runtime should delegate grouped recovery inputs to the policy");
		AssertSourceMatches(
			chargeForecast,
			@"var\s+chargesAfterSpend\s*=\s*Math\.Max\s*\(\s*input\.CurrentCharges\s*-\s*1\s*,\s*0\s*\).*?var\s+chargesNeeded\s*=\s*input\.MaximumCharges\s*-\s*chargesAfterSpend",
			"Bloodletter recovery should forecast from the post-spend charge count");
		AssertSourceMatches(
			chargeForecast,
			@"var\s+firstChargeRecoveryTime\s*=\s*input\.IsCooldownTicking\s*&&\s*input\.CurrentCharges\s*<\s*input\.MaximumCharges\s*\?\s*Math\.Max\s*\(\s*0f\s*,\s*input\.FirstChargeTimeRemaining\s*\)\s*:\s*input\.OneChargeRecastTime\s*;",
			"Bloodletter recovery should distinguish an existing cooldown tick from a fresh full-charge spend");
		AssertSourceMatches(
			tryUseHeartbreak,
			@"var\s+reservationActive\s*=\s*IsBloodletterBurstReservationActive\s*\(\s*\)\s*;.*?if\s*\(\s*InBurst\s*\|\|\s*!\s*reservationActive\s*\)\s*\{.*?return\s+TryUseBloodletterVariant\s*\(\s*out\s+act\s*,\s*usedUp:\s*true\s*\)\s*;.*?\}",
			"Bloodletter should spend freely outside burst reservation");
		AssertSourceMatches(
			tryUseHeartbreak,
			@"var\s+canRecoverAfterSpend\s*=\s*BardAscendedDecisionPolicy\.CanRecoverBloodletterChargesAfterSpend\s*\(.*?\).*?return\s+\(\s*canRecoverAfterSpend\s*\|\|\s*willHaveMaxCharges\s*\)\s*&&\s*TryUseBloodletterVariant\s*\(\s*out\s+act\s*,\s*usedUp:\s*true\s*\)\s*;",
			"Bloodletter reservation should spend only when recovery or overcap protection allows it");
		AssertSourceMatches(
			emergencyAbility,
			@"TryUseEmpyrealArrow\s*\(\s*out\s+act\s*\).*?TryUseBarrage\s*\(\s*out\s+act\s*\).*?TryUsePitchPerfect\s*\(\s*out\s+act\s*\)",
			"Emergency ability priority should keep Empyreal Arrow, Barrage, and Pitch Perfect ahead of attack ability spending");
		AssertSourceMatches(
			attackAbility,
			@"TryUseRadiantFinale\s*\(\s*out\s+act\s*\).*?TryUseBattleVoice\s*\(\s*out\s+act\s*\).*?TryUseRagingStrikes\s*\(\s*out\s+act\s*\).*?TryUseHeartBreakShot\s*\(\s*out\s+act\s*\).*?TryUseSideWinder\s*\(\s*out\s+act\s*\)",
			"Attack ability priority should keep burst buffs before Bloodletter and Sidewinder after Bloodletter");
	}

	static void BardAscendedRuntimeAdvancesStrictOpenerOnlyAfterActionSuccess()
	{
		var source = StripSourceComments(File.ReadAllText(RepositoryPath("RotationSolver", "RebornRotations", "Ranged", "BRD_Ascended.cs")));
		var startStrictOpener = ExtractMethodBody(source, "void StartStrictOpener");
		var openerAttempt = ExtractMethodBody(source, "bool TryUseRequestedOpenerAction");
		var refreshCombatCycle = ExtractMethodBody(source, "void RefreshCombatCycleState");

		AssertSourceMatches(
			source,
			@"\bprivate\s+BardAscendedOpenerState\s+_openerState\s*=\s*BardAscendedOpenerState\.Start\s*\(\s*BardAscendedSongTiming\.Standard\s*\)\s*;",
			"BRD Ascended should own mutable opener state for the active combat cycle");
		AssertSourceMatches(
			source,
			@"\bprivate\s+void\s+RefreshCombatCycleState\s*\(\s*\)",
			"BRD Ascended combat cycle refresh should be instance scoped so it can start opener state");
		AssertSourceMatches(
			source,
			@"\bprivate\s+bool\s+_isStrictOpenerActive\s*;",
			"BRD Ascended should track whether strict opener mode is active");
		AssertSourceMatches(
			source,
			@"\bprivate\s+bool\s+_hasStrictOpenerEndedThisCycle\s*;",
			"BRD Ascended should prevent strict opener restart after completion or break in the same combat cycle");
		AssertSourceMatches(
			startStrictOpener,
			@"\bif\s*\(\s*IsCustom\s*\)\s*return\s*;",
			"BRD Ascended should keep Custom song timing on the existing priority path");
		AssertSourceMatches(
			openerAttempt,
			@"\bif\s*\(\s*request\.Action\s*==\s*BardAscendedOpenerAction\.Potion\s*\)\s*\{.*?if\s*\(\s*!\s*TryUseStrictOpenerPotion\s*\(\s*out\s+act\s*\)\s*\)\s*\{.*?_openerState\s*=\s*request\.NextState\s*;.*?return\s+false\s*;.*?\}.*?_openerState\s*=\s*request\.NextState\s*;.*?return\s+true\s*;.*?\}",
			"BRD Ascended should skip disabled scripted potion slots without breaking the opener");
		AssertSourceMatches(
			source,
			@"\bprivate\s+bool\s+TryUseStrictOpenerPotion\s*\(\s*out\s+IAction\?\s+act\s*\)\s*\{.*?act\s*=\s*null\s*;.*?if\s*\(\s*!\s*PotionUsageEnabled\s*\|\|\s*IsMedicated\s*\)\s*return\s+false\s*;.*?return\s+UseBurstMedicine\s*\(\s*out\s+act\s*\)\s*;.*?\}",
			"BRD Ascended scripted potion slots should use the potion item without inheriting countdown timing checks");
		AssertSourceMatches(
			openerAttempt,
			@"\bif\s*\(\s*!\s*TryResolveOpenerAction\s*\(\s*request\.Action\s*,\s*out\s+var\s+requestedAction\s*\)\s*\)\s*\{.*?EndStrictOpener\s*\(\s*\)\s*;.*?return\s+false\s*;.*?\}.*?if\s*\(\s*!\s*requestedAction\.CanUse\s*\(\s*out\s+act.*?\)\s*\)\s*\{.*?EndStrictOpener\s*\(\s*\)\s*;.*?return\s+false\s*;.*?\}.*?_openerState\s*=\s*request\.NextState\s*;",
			"BRD Ascended should apply opener state only after requested action resolution and CanUse succeed");
		AssertSourceMatches(
			openerAttempt,
			@"\bif\s*\(\s*request\.Kind\s*==\s*BardAscendedOpenerResultKind\.Skip\s*\)\s*\{.*?_openerState\s*=\s*request\.NextState\s*;.*?return\s+false\s*;.*?\}",
			"BRD Ascended should advance opener state when the controller skips an optional slot");
		AssertSourceMatches(
			openerAttempt,
			@"\bif\s*\(\s*request\.Kind\s*==\s*BardAscendedOpenerResultKind\.Complete\s*\|\|\s*request\.Kind\s*==\s*BardAscendedOpenerResultKind\.Break\s*\)\s*\{.*?EndStrictOpener\s*\(\s*\)\s*;.*?return\s+false\s*;.*?\}",
			"BRD Ascended should disable opener requests after completion or break");
		AssertSourceMatches(
			refreshCombatCycle,
			@"\bIsFirstCycle\s*=\s*true\s*;.*?StartDirtyStartRecoveryIfNeeded\s*\(\s*\)\s*;.*?\bif\s*\(\s*!\s*_isStrictOpenerActive\s*&&\s*!\s*IsDirtyStartRecoveryActive\s*\)\s*\{?\s*StartStrictOpener\s*\(\s*\)",
			"BRD Ascended should preserve countdown opener state when combat starts");
	}

	static bool ShouldSpendApex(
		BardAscendedSongPhase songPhase,
		byte soulVoice,
		bool isInBurst = false,
		bool wouldUseIronJaws = false,
		float songSecondsRemaining = 45f,
		float targetSecondsRemaining = float.PositiveInfinity,
		float weaponTotalSeconds = 2.48f,
		bool wouldUseEnhancedFiller = false,
		bool noFutureBlastPossible = false)
	{
		var input = new BardAscendedApexDecisionInput(
			SongPhase: songPhase,
			SoulVoice: soulVoice,
			IsInBurst: isInBurst,
			WouldUseIronJaws: wouldUseIronJaws,
			SongSecondsRemaining: songSecondsRemaining,
			TargetSecondsRemaining: targetSecondsRemaining,
			WeaponTotalSeconds: weaponTotalSeconds,
			WouldUseEnhancedFiller: wouldUseEnhancedFiller,
			NoFutureBlastPossible: noFutureBlastPossible);

		return BardAscendedDecisionPolicy.ShouldSpendApex(input);
	}

	static string RepositoryPath(params string[] parts)
	{
		var root = FindRepositoryRoot();
		var segments = new string[parts.Length + 1];
		segments[0] = root;
		Array.Copy(parts, 0, segments, 1, parts.Length);
		return Path.Combine(segments);
	}

	static string FindRepositoryRoot()
	{
		var directory = new DirectoryInfo(AppContext.BaseDirectory);
		while (directory != null)
		{
			var gitPath = Path.Combine(directory.FullName, ".git");
			if (Directory.Exists(gitPath) || File.Exists(gitPath))
			{
				return directory.FullName;
			}

			directory = directory.Parent;
		}

		throw new InvalidOperationException("Could not locate repository root");
	}

	static void AssertSourceMatches(string source, string pattern, string message)
	{
		AssertTrue(SourcePattern(pattern).IsMatch(source), message);
	}

	static void AssertSourceDoesNotMatch(string source, string pattern, string message)
	{
		AssertFalse(SourcePattern(pattern).IsMatch(source), message);
	}

	static Regex SourcePattern(string pattern)
	{
		return new Regex(pattern, RegexOptions.CultureInvariant | RegexOptions.Singleline);
	}

	static string ExtractMethodBody(string source, string methodName)
	{
		var methodStart = source.IndexOf($"{methodName}(", StringComparison.Ordinal);
		if (methodStart < 0)
		{
			throw new InvalidOperationException($"Could not locate method {methodName}");
		}

		var bodyStart = source.IndexOf('{', methodStart);
		if (bodyStart < 0)
		{
			throw new InvalidOperationException($"Could not locate method body for {methodName}");
		}

		var depth = 0;
		for (var index = bodyStart; index < source.Length; index++)
		{
			if (source[index] == '{') depth++;
			if (source[index] != '}') continue;

			depth--;
			if (depth == 0)
			{
				return source[bodyStart..(index + 1)];
			}
		}

		throw new InvalidOperationException($"Could not locate method end for {methodName}");
	}

	static string StripSourceComments(string source)
	{
		return Regex.Replace(
			source,
			@"//.*?$|/\*.*?\*/",
			string.Empty,
			RegexOptions.CultureInvariant | RegexOptions.Multiline | RegexOptions.Singleline);
	}
}
