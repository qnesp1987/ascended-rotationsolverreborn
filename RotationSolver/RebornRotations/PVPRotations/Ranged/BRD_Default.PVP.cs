using System.Numerics;
using Dalamud.Game.ClientState.Objects.SubKinds;
using RotationSolver.Basic.Actions.PvPTargetSelection;
using RotationSolver.Basic.Actions.PvPTargetSelection.Factors;

namespace RotationSolver.RebornRotations.PVPRotations.Ranged;

[Rotation("Default PVP", CombatType.PvP, GameVersion = "7.5")]
[SourceCode(Path = "main/RebornRotations/PVPRotations/Ranged/BRD_Default.PvP.cs")]

public sealed class BRD_DefaultPvP : BardRotation
{
	private const uint BurstExpiryGcdWindow = 1;
	private const float BurstExpiryOffset = 0f;
	private const float PaeanCriticalHpThreshold = 0.35f;
	private const float PaeanLowHpThreshold = 0.55f;
	private const float PaeanHealthyEngageThreshold = 0.65f;
	private const float PaeanShortCombatDistance = 12f;
	private const int PaeanMaxFocusedHostilesForEngage = 1;
	private const float RepellingBackstepYalms = 10f;
	private const float PaeanPeelScoreThreshold = 2f;
	private const float PaeanEngageScoreThreshold = 3f;
	private const float PaeanCleanseBaseWeight = 100f;
	private const float PaeanCriticalHpWeight = 6f;
	private const float PaeanLowHpWeight = 2.5f;
	private const float PaeanFocusedHostileWeight = 2f;
	private const float PaeanHealerSupportRoleWeight = 3f;
	private const float PaeanRangedSupportRoleWeight = 2f;
	private const float PaeanMeleeSupportRoleWeight = 1.25f;
	private const float PaeanTankSupportRoleWeight = 0.5f;
	private const float PaeanDistanceWeight = 1f;
	private const float PaeanTankEngageWeight = 2.5f;
	private const float PaeanMeleeEngageWeight = 2f;
	private const float PaeanSmartTargetWeight = 1.5f;
	private const float OffensiveAllyBurstRadiusYalms = 8f;
	private const float OffensiveSplashRadiusYalms = 5f;
	private const float OffensiveLineHalfWidthYalms = 3f;
	private const double GuardReactionWindowSeconds = 1.25;
	private const double PowerfulShotPotency = 6_000.0;
	private const double PitchPerfectPotency = 9_000.0;
	private const double ApexArrowPotency = 8_000.0;
	private const double HarmonicArrowPotency = 9_000.0;
	private const double BlastArrowPotency = 10_000.0;
	private const double EncoreOfLightPotency = 10_000.0;
	private const double EagleEyeShotPotency = 12_000.0;

	// PvP Recuperate restores a fixed amount; the codebase commits to 16,000 (matches MCH's
	// RecuperatePotency). Exact figure is an assumption; the anti-heal gap does not hinge on it.
	private const double RecuperateHealPotency = 16_000.0;

	// PvP Recuperate MP cost. Sources vary (2,000-2,500); tunable, not load-bearing.
	private const double RecuperateMpCost = 2_000.0;

	private readonly record struct PaeanCandidate(IBattleChara Target, float Score);

	private enum PaeanCastIntent
	{
		Cleanse,
		Protect,
	}

	#region Configurations

	[RotationConfig(CombatType.PvP, Name = "Use Warden's Paean on other players")]
	public bool BRDEsuna2 { get; set; } = true;
	#endregion

	#region oGCDs
	protected override bool EmergencyAbility(IAction nextGCD, out IAction? action)
	{
		if (StatusHelper.PlayerHasStatus(false, StatusHelper.PurifyPvPStatuses))
		{
			if (TheWardensPaeanPvP.CanUse(out action, targetOverride: TargetType.Self))
			{
				return true;
			}
		}

		if (BRDEsuna2 && TryUseSupportPaean(out action))
		{
			return true;
		}

		if (BraveryPvP.CanUse(out action))
		{
			if (InCombat)
			{
				return true;
			}
		}

		if (DervishPvP.CanUse(out action))
		{
			if (InCombat)
			{
				return true;
			}
		}

		return base.EmergencyAbility(nextGCD, out action);
	}

	private bool TryUseSupportPaean(out IAction? action)
	{
		action = null;

		foreach (var cleanseTarget in SelectCleansePaeanTargets())
		{
			if (TryUseWardensPaeanOn(TheWardensPaeanPvP, cleanseTarget.Target, PaeanCastIntent.Cleanse, out action))
			{
				return true;
			}
		}

		foreach (var peelTarget in SelectProtectivePaeanTargets())
		{
			if (TryUseWardensPaeanOn(TheWardensPaeanPvP, peelTarget.Target, PaeanCastIntent.Protect, out action))
			{
				return true;
			}
		}

		foreach (var engageTarget in SelectEngagePaeanTargets())
		{
			if (TryUseWardensPaeanOn(TheWardensPaeanPvP, engageTarget.Target, PaeanCastIntent.Protect, out action))
			{
				return true;
			}
		}

		return false;
	}

	private List<PaeanCandidate> SelectCleansePaeanTargets()
	{
		List<PaeanCandidate> candidates = [];

		foreach (var member in PartyMembers)
		{
			if (!IsValidPaeanTarget(member) || !member.HasStatus(false, StatusHelper.PurifyPvPStatuses))
			{
				continue;
			}

			var score = PaeanCleanseBaseWeight
				+ ScorePaeanHealth(member)
				+ CountHostilesTargeting(member) * PaeanFocusedHostileWeight
				+ ScoreSupportRole(member)
				+ ScorePaeanDistance(member);

			candidates.Add(new PaeanCandidate(member, score));
		}

		candidates.Sort((left, right) => right.Score.CompareTo(left.Score));
		return candidates;
	}

	private List<PaeanCandidate> SelectProtectivePaeanTargets()
	{
		List<PaeanCandidate> candidates = [];

		foreach (var member in PartyMembers)
		{
			if (!IsValidPaeanTarget(member)
				|| member.HasStatus(false, StatusHelper.PurifyPvPStatuses)
				|| HasPaeanLockout(member))
			{
				continue;
			}

			var focusCount = CountHostilesTargeting(member);
			var healthRatio = member.GetHealthRatio();
			if (!BardPvPDecisionPolicy.ShouldUseProtectivePaean(
				healthRatio,
				focusCount))
			{
				continue;
			}

			var score = ScorePaeanHealth(member)
				+ focusCount * PaeanFocusedHostileWeight
				+ ScoreSupportRole(member)
				+ ScorePaeanDistance(member);

			if (score < PaeanPeelScoreThreshold)
			{
				continue;
			}

			candidates.Add(new PaeanCandidate(member, score));
		}

		candidates.Sort((left, right) => right.Score.CompareTo(left.Score));
		return candidates;
	}

	private List<PaeanCandidate> SelectEngagePaeanTargets()
	{
		List<PaeanCandidate> candidates = [];

		foreach (var member in PartyMembers)
		{
			if (!IsValidPaeanTarget(member)
				|| member.HasStatus(false, StatusHelper.PurifyPvPStatuses)
				|| HasPaeanLockout(member)
				|| member.GetHealthRatio() < PaeanHealthyEngageThreshold
				|| CountHostilesTargeting(member) > PaeanMaxFocusedHostilesForEngage
				|| !IsEngageRole(member)
				|| !IsPushingIntoEnemies(member))
			{
				continue;
			}

			var score = ScoreEngageRole(member) + ScoreSmartTargetProximity(member);
			if (score < PaeanEngageScoreThreshold)
			{
				continue;
			}

			candidates.Add(new PaeanCandidate(member, score));
		}

		candidates.Sort((left, right) => right.Score.CompareTo(left.Score));
		return candidates;
	}

	private bool IsValidPaeanTarget(IBattleChara? member)
	{
		return member != null
			&& member.GameObjectId != 0
			&& Player != null
			&& member.GameObjectId != Player.GameObjectId
			&& member.CurrentHp > 0
			&& member.DistanceToPlayer() <= TheWardensPaeanPvP.TargetInfo.Range;
	}

	private static bool HasPaeanLockout(IBattleChara member)
	{
		return member.HasStatus(false, StatusID.TheWardensPaean_3143, StatusID.WardensGrace);
	}

	private static float ScorePaeanHealth(IBattleChara member)
	{
		var healthRatio = member.GetHealthRatio();
		if (healthRatio <= PaeanCriticalHpThreshold)
		{
			return PaeanCriticalHpWeight;
		}

		return healthRatio <= PaeanLowHpThreshold ? PaeanLowHpWeight : 0f;
	}

	private static float ScoreSupportRole(IBattleChara member)
	{
		if (member.IsJobCategory(JobRole.Healer))
		{
			return PaeanHealerSupportRoleWeight;
		}

		if (member.IsJobCategory(JobRole.RangedPhysical) || member.IsJobCategory(JobRole.RangedMagical))
		{
			return PaeanRangedSupportRoleWeight;
		}

		if (member.IsJobCategory(JobRole.Melee))
		{
			return PaeanMeleeSupportRoleWeight;
		}

		return member.IsJobCategory(JobRole.Tank) ? PaeanTankSupportRoleWeight : 0f;
	}

	private static bool IsEngageRole(IBattleChara member)
	{
		return member.IsJobCategory(JobRole.Tank) || member.IsJobCategory(JobRole.Melee);
	}

	private static float ScoreEngageRole(IBattleChara member)
	{
		if (member.IsJobCategory(JobRole.Tank))
		{
			return PaeanTankEngageWeight;
		}

		return member.IsJobCategory(JobRole.Melee) ? PaeanMeleeEngageWeight : 0f;
	}

	private float ScorePaeanDistance(IBattleChara member)
	{
		var range = TheWardensPaeanPvP.TargetInfo.Range;
		if (range <= 0f)
		{
			return 0f;
		}

		var distanceRatio = Math.Clamp(member.DistanceToPlayer() / range, 0f, 1f);
		return (1f - distanceRatio) * PaeanDistanceWeight;
	}

	private static int CountHostilesTargeting(IBattleChara ally)
	{
		var count = 0;
		foreach (var hostile in AllHostileTargets)
		{
			if (hostile != null && hostile.TargetObjectId == ally.GameObjectId)
			{
				count++;
			}
		}

		return count;
	}

	private static bool IsPushingIntoEnemies(IBattleChara ally)
	{
		var smartTarget = HostileTarget;
		if (Player == null || smartTarget == null)
		{
			return false;
		}

		if (DistanceToNearestHostile(ally) > PaeanShortCombatDistance)
		{
			return false;
		}

		var allyDistanceToSmartTarget = Vector3.Distance(ally.Position, smartTarget.Position);
		var bardDistanceToSmartTarget = Vector3.Distance(Player.Position, smartTarget.Position);
		return allyDistanceToSmartTarget < bardDistanceToSmartTarget;
	}

	private static float ScoreSmartTargetProximity(IBattleChara ally)
	{
		var smartTarget = HostileTarget;
		if (smartTarget == null)
		{
			return 0f;
		}

		return Vector3.Distance(ally.Position, smartTarget.Position) <= PaeanShortCombatDistance
			? PaeanSmartTargetWeight
			: 0f;
	}

	private static float DistanceToNearestHostile(IBattleChara ally)
	{
		var nearestDistance = float.MaxValue;
		foreach (var hostile in AllHostileTargets)
		{
			if (hostile == null || hostile.CurrentHp == 0)
			{
				continue;
			}

			var distance = Vector3.Distance(ally.Position, hostile.Position) - hostile.HitboxRadius;
			if (distance < nearestDistance)
			{
				nearestDistance = distance;
			}
		}

		return nearestDistance;
	}

	private static bool TryUseWardensPaeanOn(IBaseAction wardensPaean, IBattleChara? target, PaeanCastIntent intent, out IAction? action)
	{
		action = null;

		if (target == null || target.GameObjectId == 0 || target.CurrentHp == 0)
		{
			return false;
		}

		return PvPSingleTargetActionUse.TryUseOn(
			wardensPaean,
			target.GameObjectId,
			new PvPSingleTargetActionOptions(
				SkipTargetStatusNeedCheck: intent == PaeanCastIntent.Protect,
				TargetOverride: TargetType.Nearest),
			out action);
	}

	protected override bool AttackAbility(IAction nextGCD, out IAction? action)
	{
		if (RepellingShotPvP.CanUse(out action))
		{
			var input = BuildShutdownInput(
				RepellingShotPvP,
				IsRepellingBackstepSafe(RepellingShotPvP.Target.Target));

			if (BardPvPDecisionPolicy.ShouldUseRepellingShot(input))
			{
				return true;
			}
		}

		if (SilentNocturnePvP.CanUse(out action))
		{
			var facts = BuildKillSecureFacts(SilentNocturnePvP.Target.Target);
			var input = BuildShutdownInput(SilentNocturnePvP, safeBackstepExists: true, facts);
			if (BardPvPDecisionPolicy.ShouldUseSilentNocturne(input))
			{
				return true;
			}
		}

		if (TryUseFrontlineEagleEyeShot(out action))
		{
			return true;
		}

		if (TryUsePolicyAction(
			EncoreOfLightPvP,
			BardPvPActionIntent.EncoreOfLight,
			BardPvPOffensiveDecisionPolicy.ShouldUseEncoreOfLight,
			out action,
			skipAoeCheck: true))
		{
			return true;
		}

		return base.AttackAbility(nextGCD, out action);
	}

	private bool TryUseFrontlineEagleEyeShot(out IAction? action)
	{
		action = null;

		return TryUsePolicyAction(
			EagleEyeShotPvP,
			BardPvPActionIntent.EagleEyeShot,
			ShouldUseFrontlineEagleEyeShot,
			out action);
	}

	private static bool ShouldUseFrontlineEagleEyeShot(BardPvPOffensiveDecisionInput input)
	{
		var target = input.Target;
		var eagleEyeShotInput = new FrontlineEagleEyeShotInput(
			Job: FrontlinePvPRangedJob.Bard,
			IsInFrontline: DataCenter.IsInFrontline,
			IsInCrystallineConflict: DataCenter.IsInCrystallineConflict,
			Target: new FrontlineEagleEyeShotTargetState(
				HealthRatio: target.HealthRatio,
				CurrentMp: target.CurrentMp,
				HasGuard: target.HasGuard,
				HasResilience: target.HasResilience,
				HasNonGuardInvulnerability: target.HasInvulnerability,
				HasAllyFocus: target.HasAllyFocus,
				IsObjectiveRelevant: target.IsObjectiveRelevant,
				IsControlled: target.IsControlled,
				IsBurstWorthy: target.IsVulnerable,
				TargetCommitted: input.TargetCommitted,
				ImmediateFollowUpAvailable: input.FollowUpAvailable,
				HasWildfire: false,
				ExpectedDamageRatio: input.ExpectedDamageRatio));

		return FrontlinePvPRoleActionPolicy.ShouldUseEagleEyeShot(eagleEyeShotInput);
	}

	private static bool IsControlledForEagleEyeShot(IBattleChara target)
	{
		return target.HasStatus(
			false,
			StatusID.Silenced,
			StatusID.Bind,
			StatusID.Bind_1345,
			StatusID.Stun,
			StatusID.Stun_1343,
			StatusID.DeepFreeze_3219,
			StatusID.MiracleOfNature);
	}

	private static BardPvPShutdownInput BuildShutdownInput(
		IBaseAction action,
		bool safeBackstepExists,
		BardPvPKillSecureFacts killSecureFacts = default)
	{
		var target = action.Target.Target;
		return new BardPvPShutdownInput(
			TargetHasResilience: target.HasStatus(false, StatusID.Resilience),
			TargetIsCasting: target.IsCasting,
			TargetThreatensFragileAlly: TargetThreatensProtectedAlly(target),
			TargetIsBurstWorthy: IsBurstWorthy(target),
			TargetHealthRatio: target.GetHealthRatio(),
			TargetDistance: target.DistanceToPlayer(),
			SafeBackstepExists: safeBackstepExists,
			ObjectiveControlNeeded: IsObjectiveRelevantTarget(target),
			KillSecure: killSecureFacts);
	}

	private BardPvPKillSecureFacts BuildKillSecureFacts(IBattleChara target)
	{
		if (target.MaxHp == 0)
		{
			return default;
		}

		var database = PvPMitigationDatabaseProvider.Current;
		var effectiveHp = EffectiveHpCalculator.Compute(target, database);
		var effectiveHpRatio = double.IsPositiveInfinity(effectiveHp)
			? double.PositiveInfinity
			: effectiveHp / target.MaxHp;

		return new BardPvPKillSecureFacts(
			EffectiveHpRatio: effectiveHpRatio,
			ExpectedDamageRatio: BestAvailableBurstPotency() / target.MaxHp,
			RecuperateRatio: RecuperateHealPotency / target.MaxHp,
			TargetCanRecuperate: target.CurrentMp >= RecuperateMpCost,
			HasGuard: target.HasStatus(false, StatusID.Guard));
	}

	// Best immediately-available Bard damaging GCD: Pitch Perfect when Repertoire is up else Powerful
	// Shot, raised by Blast Arrow (when its window is up) or Apex Arrow (charged, no Blast window).
	private double BestAvailableBurstPotency()
	{
		var spammable = StatusHelper.PlayerHasStatus(true, StatusID.Repertoire)
			? PitchPerfectPotency
			: PowerfulShotPotency;

		var hasBlastWindow = StatusHelper.PlayerHasStatus(true, StatusID.BlastArrowReady_3142);
		var blast = hasBlastWindow ? BlastArrowPotency : 0.0;
		var apex = !hasBlastWindow && ApexArrowPvP.Cooldown.HasOneCharge ? ApexArrowPotency : 0.0;

		return Math.Max(spammable, Math.Max(blast, apex));
	}

	private static bool TargetThreatensProtectedAlly(IBattleChara target)
	{
		if (target.TargetObjectId == 0)
		{
			return false;
		}

		return ThreatenedAllyState.BuildThreatenedAllyIds().Contains(target.TargetObjectId);
	}

	private static bool IsBurstWorthy(IBattleChara target)
	{
		if (target.MaxHp <= 0 || !target.IsEnemy())
		{
			return false;
		}

		var database = PvPMitigationDatabaseProvider.Current;
		var effectiveHp = EffectiveHpCalculator.Compute(target, database);
		var effectiveHpRatio = double.IsPositiveInfinity(effectiveHp)
			? double.PositiveInfinity
			: effectiveHp / target.MaxHp;

		var score = PvPTargetScorer.Explain(target, PvPScoringContextBuilder.BuildCurrent(GetContextHostiles(target)));
		var input = new PvPBurstDecisionInput(
			Intent: PvPBurstIntent.Burst,
			EffectiveHpRatio: effectiveHpRatio,
			ActiveDamageReduction: MitigationPenalty.Compute(target, database),
			Score: score);

		return PvPBurstDecision.Evaluate(input) != PvPBurstRecommendation.Hold;
	}

	private static IReadOnlyList<IBattleChara> GetContextHostiles(IBattleChara target)
	{
		return DataCenter.AllHostileTargets.Count > 0 ? DataCenter.AllHostileTargets : [target];
	}

	private static bool IsObjectiveRelevantTarget(IBattleChara target)
	{
		return PvPObjectiveState.BuildObjectiveRelevantTargetIds().Contains(target.GameObjectId);
	}

	private static bool IsRepellingBackstepSafe(IBattleChara target)
	{
		if (Player == null)
		{
			return false;
		}

		var awayFromTarget = Player.Position - target.Position;
		if (awayFromTarget.LengthSquared() <= float.Epsilon)
		{
			return false;
		}

		var destination = Player.Position + Vector3.Normalize(awayFromTarget) * RepellingBackstepYalms;
		return DataCenter.IsMovementDestinationSafe(destination)
			&& DataCenter.IsFixedDashSafe(Player.Position, destination);
	}
	#endregion

	#region GCDs
	protected override bool GeneralGCD(out IAction? action)
	{
		if (TryUsePolicyAction(
			HarmonicArrowPvP,
			BardPvPActionIntent.HarmonicArrow,
			BardPvPOffensiveDecisionPolicy.ShouldUseHarmonicArrow,
			out action))
		{
			return true;
		}

		if (TryUsePolicyAction(
			PitchPerfectPvP,
			BardPvPActionIntent.PitchPerfect,
			BardPvPOffensiveDecisionPolicy.ShouldUsePitchPerfect,
			out action,
			skipAoeCheck: true))
		{
			return true;
		}

		if (TryUsePolicyAction(
			BlastArrowPvP,
			BardPvPActionIntent.BlastArrow,
			BardPvPOffensiveDecisionPolicy.ShouldUseBlastArrow,
			out action))
		{
			return true;
		}

		if (TryUsePolicyAction(
			ApexArrowPvP,
			BardPvPActionIntent.ApexArrow,
			BardPvPOffensiveDecisionPolicy.ShouldUseApexArrow,
			out action,
			skipStatusProvideCheck: true))
		{
			return true;
		}

		return TryUsePolicyAction(
				PowerfulShotPvP,
				BardPvPActionIntent.PowerfulShot,
				BardPvPOffensiveDecisionPolicy.ShouldUsePowerfulShot,
				out action)
			|| base.GeneralGCD(out action);
	}

	private bool TryUsePolicyAction(
		IBaseAction baseAction,
		BardPvPActionIntent intent,
		Func<BardPvPOffensiveDecisionInput, bool> shouldUse,
		out IAction? action,
		bool skipAoeCheck = false,
		bool skipStatusProvideCheck = false)
	{
		action = null;

		var rankedFrame = RankTargets(intent, baseAction.TargetInfo.Range);
		foreach (var targetSnapshot in rankedFrame.RankedTargets)
		{
			var target = PvPLiveTargetFactsBuilder.FindLiveTargetById(AllHostileTargets, targetSnapshot.TargetId);
			if (target == null)
			{
				continue;
			}

			var input = CreateDecisionInput(targetSnapshot, target, intent, baseAction, rankedFrame.LiveFrame.Allies);
			if (!shouldUse(input))
			{
				continue;
			}

			var refreshedFrame = RefreshTargetSnapshot(
				targetSnapshot,
				target,
				baseAction.TargetInfo.Range);
			var refreshedInput = CreateDecisionInput(
				refreshedFrame.Snapshot,
				target,
				intent,
				baseAction,
				refreshedFrame.LiveFrame.Allies);
			if (!shouldUse(refreshedInput))
			{
				continue;
			}

			if (TryUseActionOn(baseAction, refreshedFrame.Snapshot.TargetId, out action, skipAoeCheck, skipStatusProvideCheck))
			{
				return true;
			}
		}

		return false;
	}

	private readonly record struct BardPvPLiveTargetFrame(
		PvPLiveTargetFactsContext FactsContext,
		IReadOnlyList<PvPCombatantSnapshot> Allies,
		IReadOnlyList<PvPCombatantSnapshot> Hostiles);

	private readonly record struct BardPvPRankedTargetFrame(
		IReadOnlyList<BardPvPTargetSnapshot> RankedTargets,
		BardPvPLiveTargetFrame LiveFrame);

	private readonly record struct BardPvPRefreshedTargetFrame(
		BardPvPTargetSnapshot Snapshot,
		BardPvPLiveTargetFrame LiveFrame);

	private static BardPvPRankedTargetFrame RankTargets(BardPvPActionIntent intent, float range)
	{
		List<BardPvPTargetSnapshot> snapshots = [];
		var liveFrame = CreateLiveTargetFrame(range);

		foreach (var hostile in AllHostileTargets)
		{
			if (hostile == null
				|| hostile.CurrentHp == 0)
			{
				continue;
			}

			snapshots.Add(CreateTargetSnapshot(hostile, intent, liveFrame));
		}

		return new BardPvPRankedTargetFrame(
			BardPvPTargetPolicy.Rank(snapshots, intent),
			liveFrame);
	}

	private static BardPvPLiveTargetFrame CreateLiveTargetFrame(float range)
	{
		var hostiles = PvPLiveTargetFactsBuilder.ToCombatantSnapshots(
			AllHostileTargets,
			target => target.GetHealthRatio());
		var allies = PvPLiveTargetFactsBuilder.ToCombatantSnapshots(
			PartyMembers,
			target => target.GetHealthRatio(),
			excludedObjectId: Player?.GameObjectId ?? 0);
		var objectiveTargets = PvPObjectiveState.BuildObjectiveRelevantTargetIds();

		return new BardPvPLiveTargetFrame(
			CreateFactsContext(range, allies, objectiveTargets),
			allies,
			hostiles);
	}

	private static PvPLiveTargetFactsContext CreateFactsContext(
		float range,
		IReadOnlyList<PvPCombatantSnapshot> allies,
		IReadOnlySet<ulong> objectiveTargets)
	{
		return new PvPLiveTargetFactsContext(
			MitigationDatabase: PvPMitigationDatabaseProvider.Current,
			ObjectiveRelevantTargetIds: objectiveTargets,
			Allies: allies,
			CurrentTime: TimeSpan.FromMilliseconds(Environment.TickCount64),
			GuardCooldownTracker: DataCenter.PvPGuardCooldownTracker,
			GuardReactionWindow: TimeSpan.FromSeconds(GuardReactionWindowSeconds),
			ActionRange: range,
			DistanceToPlayerProvider: target => target.DistanceToPlayer(),
			HealthRatioProvider: target => target.GetHealthRatio(),
			HasStatus: (target, statusId) => target.HasStatus(false, statusId));
	}

	private static BardPvPTargetSnapshot CreateTargetSnapshot(
		IBattleChara target,
		BardPvPActionIntent intent,
		BardPvPLiveTargetFrame liveFrame)
	{
		var facts = PvPLiveTargetFactsBuilder.Create(target, liveFrame.FactsContext);

		return new BardPvPTargetSnapshot(
			TargetId: facts.TargetId,
			HealthRatio: facts.HealthRatio,
			CurrentMp: facts.CurrentMp,
			HasGuard: facts.HasGuard,
			HasResilience: facts.HasResilience,
			IsObjectiveRelevant: facts.IsObjectiveRelevant,
			AllyFocusCount: facts.AllyFocusCount,
			IsVulnerable: IsBurstWorthy(target),
			IsControlled: IsControlledForEagleEyeShot(target),
			HasInvulnerability: facts.HasNonGuardInvulnerability,
			ExpectedDamageRatio: ExpectedDamageRatio(intent, target),
			EffectiveHealthRatio: facts.EffectiveHealthRatio,
			GuardPiercingEffectiveHealthRatio: facts.GuardPiercingEffectiveHealthRatio,
			ActiveDamageReduction: facts.ActiveDamageReduction,
			IsExposed: facts.IsExposed,
			IsInNormalRange: facts.IsInNormalRange,
			LineTargetCount: CountHostilesInLine(
				liveFrame.Hostiles,
				target,
				liveFrame.FactsContext.ActionRange),
			SplashTargetCount: PvPCombatantQueries.CountHostilesNear(
				liveFrame.Hostiles,
				target.Position,
				OffensiveSplashRadiusYalms),
			GuardAvailability: facts.GuardAvailability);
	}

	private static BardPvPRefreshedTargetFrame RefreshTargetSnapshot(
		BardPvPTargetSnapshot snapshot,
		IBattleChara target,
		float range)
	{
		var liveFrame = CreateLiveTargetFrame(range);
		var facts = PvPLiveTargetFactsBuilder.Create(target, liveFrame.FactsContext);

		var refreshedSnapshot = snapshot with
		{
			HealthRatio = facts.HealthRatio,
			CurrentMp = facts.CurrentMp,
			HasGuard = facts.HasGuard,
			HasResilience = facts.HasResilience,
			IsObjectiveRelevant = facts.IsObjectiveRelevant,
			AllyFocusCount = facts.AllyFocusCount,
			IsVulnerable = IsBurstWorthy(target),
			IsControlled = IsControlledForEagleEyeShot(target),
			HasInvulnerability = facts.HasNonGuardInvulnerability,
			EffectiveHealthRatio = facts.EffectiveHealthRatio,
			GuardPiercingEffectiveHealthRatio = facts.GuardPiercingEffectiveHealthRatio,
			ActiveDamageReduction = facts.ActiveDamageReduction,
			GuardAvailability = facts.GuardAvailability,
		};

		return new BardPvPRefreshedTargetFrame(
			BardPvPTargetSnapshotRefresher.RefreshSpatialState(
				refreshedSnapshot,
				new BardPvPTargetSpatialState(
					IsInNormalRange: facts.IsInNormalRange,
					LineTargetCount: CountHostilesInLine(
						liveFrame.Hostiles,
						target,
						liveFrame.FactsContext.ActionRange),
					SplashTargetCount: PvPCombatantQueries.CountHostilesNear(
						liveFrame.Hostiles,
						target.Position,
						OffensiveSplashRadiusYalms))),
			liveFrame);
	}

	private BardPvPOffensiveDecisionInput CreateDecisionInput(
		BardPvPTargetSnapshot snapshot,
		IBattleChara target,
		BardPvPActionIntent intent,
		IBaseAction action,
		IReadOnlyList<PvPCombatantSnapshot> allies)
	{
		var objectiveControlNeeded = snapshot.IsObjectiveRelevant;
		return new BardPvPOffensiveDecisionInput(
			Target: snapshot,
			FollowUpAvailable: HasImmediateFollowUp(intent),
			AlliesCanBurst: snapshot.HasAllyFocus
				|| PvPCombatantQueries.CountAlliesNear(allies, target.Position, OffensiveAllyBurstRadiusYalms) > 0,
			ObjectiveControlNeeded: objectiveControlNeeded,
			TargetCommitted: objectiveControlNeeded
				|| snapshot.HasAllyFocus
				|| snapshot.CurrentMp <= PvPScoringFactors.MediumMp
				|| target.TargetObjectId != 0,
			HasFinalFantasia: StatusHelper.PlayerHasStatus(true, StatusID.FinalFantasia),
			HasFrontlinersMarch: StatusHelper.PlayerHasStatus(true, StatusID.FrontlinersMarch),
			HasRepertoire: StatusHelper.PlayerHasStatus(true, StatusID.Repertoire),
			HasBlastArrowReady: StatusHelper.PlayerHasStatus(true, StatusID.BlastArrowReady_3142),
			HarmonicWouldOvercap: intent == BardPvPActionIntent.HarmonicArrow
				&& action.Cooldown.WillHaveXChargesGCD(action.Cooldown.MaxCharges, BurstExpiryGcdWindow, BurstExpiryOffset),
			ForcedExpiryWindow: HasForcedExpiryWindow(intent),
			PeelValueNeeded: TargetThreatensProtectedAlly(target),
			ExpectedDamageRatio: snapshot.ExpectedDamageRatio,
			HasGuardCooldownKnowledge: DataCenter.IsInCrystallineConflict);
	}

	private static double ExpectedDamageRatio(BardPvPActionIntent intent, IBattleChara target)
	{
		if (target.MaxHp == 0)
		{
			return 0.0;
		}

		return intent switch
		{
			BardPvPActionIntent.PowerfulShot => PowerfulShotPotency / target.MaxHp,
			BardPvPActionIntent.HarmonicArrow => HarmonicArrowPotency / target.MaxHp,
			BardPvPActionIntent.PitchPerfect => PitchPerfectPotency / target.MaxHp,
			BardPvPActionIntent.ApexArrow => ApexArrowPotency / target.MaxHp,
			BardPvPActionIntent.BlastArrow => BlastArrowPotency / target.MaxHp,
			BardPvPActionIntent.EncoreOfLight => EncoreOfLightPotency / target.MaxHp,
			BardPvPActionIntent.EagleEyeShot => EagleEyeShotPotency / target.MaxHp,
			_ => 0.0,
		};
	}

	private bool HasForcedExpiryWindow(BardPvPActionIntent intent)
	{
		return intent switch
		{
			BardPvPActionIntent.HarmonicArrow => HarmonicArrowPvP.Cooldown.WillHaveXChargesGCD(
				HarmonicArrowPvP.Cooldown.MaxCharges,
				BurstExpiryGcdWindow,
				BurstExpiryOffset),
			BardPvPActionIntent.PitchPerfect => StatusHelper.PlayerHasStatus(true, StatusID.Repertoire)
				&& StatusHelper.PlayerWillStatusEndGCD(BurstExpiryGcdWindow, BurstExpiryOffset, true, StatusID.Repertoire),
			BardPvPActionIntent.ApexArrow => StatusHelper.PlayerHasStatus(true, StatusID.FrontlinersMarch)
				&& StatusHelper.PlayerWillStatusEndGCD(BurstExpiryGcdWindow, BurstExpiryOffset, true, StatusID.FrontlinersMarch),
			BardPvPActionIntent.BlastArrow => StatusHelper.PlayerHasStatus(true, StatusID.BlastArrowReady_3142)
				&& StatusHelper.PlayerWillStatusEndGCD(BurstExpiryGcdWindow, BurstExpiryOffset, true, StatusID.BlastArrowReady_3142),
			BardPvPActionIntent.EncoreOfLight => StatusHelper.PlayerHasStatus(true, StatusID.EncoreOfLightReady)
				&& StatusHelper.PlayerWillStatusEndGCD(BurstExpiryGcdWindow, BurstExpiryOffset, true, StatusID.EncoreOfLightReady),
			_ => false,
		};
	}

	private bool HasImmediateFollowUp(BardPvPActionIntent intent)
	{
		if (intent != BardPvPActionIntent.HarmonicArrow && HarmonicArrowPvP.Cooldown.HasOneCharge)
		{
			return true;
		}

		if (intent != BardPvPActionIntent.PitchPerfect
			&& PitchPerfectPvP.Cooldown.HasOneCharge
			&& StatusHelper.PlayerHasStatus(true, StatusID.Repertoire))
		{
			return true;
		}

		if (intent != BardPvPActionIntent.ApexArrow
			&& ApexArrowPvP.Cooldown.HasOneCharge
			&& !StatusHelper.PlayerHasStatus(true, StatusID.BlastArrowReady_3142))
		{
			return true;
		}

		if (intent != BardPvPActionIntent.BlastArrow
			&& BlastArrowPvP.Cooldown.HasOneCharge
			&& StatusHelper.PlayerHasStatus(true, StatusID.BlastArrowReady_3142))
		{
			return true;
		}

		return intent != BardPvPActionIntent.EncoreOfLight
			&& EncoreOfLightPvP.Cooldown.HasOneCharge
			&& StatusHelper.PlayerHasStatus(true, StatusID.EncoreOfLightReady);
	}

	private static int CountHostilesInLine(
		IReadOnlyList<PvPCombatantSnapshot> hostiles,
		IBattleChara target,
		float range)
	{
		if (Player == null)
		{
			return 0;
		}

		return PvPCombatantQueries.CountHostilesInLine(
			hostiles,
			Player.Position,
			target.Position,
			range,
			OffensiveLineHalfWidthYalms);
	}

	private static bool TryUseActionOn(
		IBaseAction baseAction,
		ulong targetId,
		out IAction? action,
		bool skipAoeCheck,
		bool skipStatusProvideCheck)
	{
		action = null;
		return PvPSingleTargetActionUse.TryUseOn(
			baseAction,
			targetId,
			new PvPSingleTargetActionOptions(
				SkipStatusProvideCheck: skipStatusProvideCheck,
				SkipAoeCheck: skipAoeCheck,
				TargetOverride: TargetType.Nearest),
			out action);
	}
	#endregion
}
