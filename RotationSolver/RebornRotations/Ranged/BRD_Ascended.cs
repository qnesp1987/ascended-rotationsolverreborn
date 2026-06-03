namespace RotationSolver.RebornRotations.Ranged;

/// <summary>
/// Selectable Patch 7.5 Bard PvE rotation that keeps encounter state in the runtime layer and delegates threshold decisions to the tested Ascended policy.
/// </summary>
[Rotation("BRD Ascended", CombatType.PvE, GameVersion = "7.5",
	Description = "Patch 7.5 Bard PvE tuning for high end raids, alliance raids, and dungeon runs.")]
[SourceCode(Path = "main/RebornRotations/Ranged/BRD_Ascended.cs")]
public sealed class BRD_Ascended : BardRotation
{
	#region Properties

	#region Constants
	private const float DoTEndBuffer = 0.5f;
	private const float ArmyHeartbreakHoldThreshold = 35f;
	private const float SidewinderBuffLookahead = 10f;
	private const float HeartbreakChargeLookahead = 5f;
	private const float Cycle369PrepullHeartbreakWindowSeconds = 1.65f;
	private const float CountdownDotWindowSeconds = 0.1f;
	private const float CountdownResetToleranceSeconds = 0.25f;
	#endregion

	#region Status Sets

	private static readonly StatusID[] NoBurstStatuses = [];
	private static readonly StatusID[] RagingStrikesStatuses = [StatusID.RagingStrikes];
	private static readonly StatusID[] BattleVoiceStatuses = [StatusID.BattleVoice];
	private static readonly StatusID[] RadiantFinaleStatuses = [StatusID.RadiantFinale_2964];
	private static readonly StatusID[] RagingBattleStatuses = [StatusID.RagingStrikes, StatusID.BattleVoice];
	private static readonly StatusID[] RagingFinaleStatuses = [StatusID.RagingStrikes, StatusID.RadiantFinale_2964];
	private static readonly StatusID[] BattleFinaleStatuses = [StatusID.BattleVoice, StatusID.RadiantFinale_2964];
	private static readonly StatusID[] FullBurstStatuses =
		[StatusID.RagingStrikes, StatusID.BattleVoice, StatusID.RadiantFinale_2964];

	#endregion

	#region Song Timings

	private static bool Is369 => SongTimings == BardAscendedSongTiming.Cycle369;
	private static bool IsCustom => SongTimings == BardAscendedSongTiming.Custom;
	private static bool UsesStandardBurstPath => BardAscendedDecisionPolicy.UsesStandardBurstPath(SongTimings);
	private static bool IsStandardTiming =>
		SongTimings is BardAscendedSongTiming.Standard
			or BardAscendedSongTiming.AdjustedStandard;

	private BardAscendedSongDurations CurrentSongDurations =>
		BardAscendedDecisionPolicy.GetSongDurations(
			SongTimings,
			new BardAscendedSongDurations(CustomWandTime, CustomMageTime, CustomArmyTime));

	private float WandTime => CurrentSongDurations.Wanderers;
	private float MageTime => CurrentSongDurations.Mages;
	private float ArmyTime => CurrentSongDurations.Armys;
	private float WandRemainTime => BardAscendedDecisionPolicy.SongMaxDuration - WandTime;
	private float MageRemainTime => BardAscendedDecisionPolicy.SongMaxDuration - MageTime;
	private float ArmyRemainTime => BardAscendedDecisionPolicy.SongMaxDuration - ArmyTime;

	#endregion

	#region Player Status

	private StatusID[] BurstStatus => (
		RagingStrikesPvE.EnoughLevel,
		BattleVoicePvE.EnoughLevel,
		RadiantFinalePvE.EnoughLevel) switch
	{
		(true, true, true) => FullBurstStatuses,
		(true, true, false) => RagingBattleStatuses,
		(true, false, true) => RagingFinaleStatuses,
		(false, true, true) => BattleFinaleStatuses,
		(true, false, false) => RagingStrikesStatuses,
		(false, true, false) => BattleVoiceStatuses,
		(false, false, true) => RadiantFinaleStatuses,
		_ => NoBurstStatuses
	};

	private IBaseAction Stormbite => StormbitePvE.EnoughLevel ? StormbitePvE : WindbitePvE;

	private IBaseAction CausticBite => CausticBitePvE.EnoughLevel ? CausticBitePvE : VenomousBitePvE;
	private IBaseAction ActiveBloodletterVariant =>
		HeartbreakShotPvE.EnoughLevel ? HeartbreakShotPvE : BloodletterPvE;
	private IBaseAction ActiveFiller =>
		BurstShotPvE.EnoughLevel ? BurstShotPvE : HeavyShotPvE;

	private bool HasBurstActions =>
		RagingStrikesPvE.EnoughLevel
		|| BattleVoicePvE.EnoughLevel
		|| RadiantFinalePvE.EnoughLevel;

	private bool HasSongActions =>
		TheWanderersMinuetPvE.EnoughLevel
		|| MagesBalladPvE.EnoughLevel
		|| ArmysPaeonPvE.EnoughLevel;

	private bool BurstEndGCD(uint gcdCount) => StatusHelper.PlayerHasStatus(true, BurstStatus)
											   && StatusHelper.PlayerWillStatusEndGCD(gcdCount, DataCenter.CalculatedActionAhead, true, BurstStatus);
	private static bool CanUseEnhancedFiller => HasBarrage || HasHawksEye;
	private static bool IsMedicated => StatusHelper.PlayerHasStatus(true, StatusID.Medicated) &&
									   !StatusHelper.PlayerWillStatusEnd(0f, true, StatusID.Medicated);
	private static bool InOddMinuteWindow => InMages && SongTime > 15f;
	private static float WeaponAhead => WeaponRemain + DataCenter.CalculatedActionAhead;

	private bool InBurst
	{
		get
		{
			if (BurstStatus.Length == 0) return false;
			foreach (var status in BurstStatus)
			{
				if (!StatusHelper.PlayerHasStatus(true, status)) return false;
			}
			return !StatusHelper.PlayerWillStatusEnd(0f, true, BurstStatus);
		}
	}

	private bool CanBurst
	{
		get
		{
			if (!MergedStatus.HasFlag(AutoStatus.Burst)) return false;

			if (!HasBurstActions) return false;
			if (RagingStrikesPvE.EnoughLevel && !RagingStrikesPvE.IsEnabled) return false;
			if (BattleVoicePvE.EnoughLevel && !BattleVoicePvE.IsEnabled) return false;
			return !RadiantFinalePvE.EnoughLevel || RadiantFinalePvE.IsEnabled;
		}
	}

	private bool CanEnterBurstWindow
	{
		get
		{
			if (!CanBurst) return false;
			if (InBurst) return true;

			return CanStartBurstWithRadiantFinale(out _)
				   || CanStartBurstWithBattleVoice(out _)
				   || CanStartBurstWithRagingStrikes(out _);
		}
	}

	private static bool IsFirstCycle { get; set; }
	private static bool HasCombatCycleState { get; set; }
	private static float LastCombatTimeRaw { get; set; }
	private enum BardAscendedDirtyStartRecoveryState
	{
		Inactive,
		Armed,
		BurstStarted
	}

	private BardAscendedOpenerState _openerState = BardAscendedOpenerState.Start(BardAscendedSongTiming.Standard);
	private bool _isStrictOpenerActive;
	private bool _hasStrictOpenerEndedThisCycle;
	private BardAscendedDirtyStartRecoveryState _dirtyStartRecoveryState;
	private float _lastCountdownRemainTime;

	#endregion

	#region Target Status

	private static bool TargetHasDoT(IBaseAction action)
	{
		return CurrentTarget != null
			   && action.Setting.TargetStatusProvide != null
			   && CurrentTarget.HasStatus(true, action.Setting.TargetStatusProvide);
	}

	#endregion

	#endregion

	#region Config Options

	[RotationConfig(CombatType.PvE, Name = "Only use DOTs on targets with Boss Icon")]
	private bool DoTsBoss { get; set; } = false;

	[RotationConfig(CombatType.PvE, Name = "Enable Planned Fight Mode")]
	private bool EnablePlannedFightMode { get; set; } = false;

	[Range(0, 1200, ConfigUnitType.Seconds, 1)]
	[RotationConfig(CombatType.PvE, Name = "Planned Fight Kill Time", Parent = nameof(EnablePlannedFightMode))]
	private float PlannedFightKillTime { get; set; }

	[RotationConfig(CombatType.PvE, Name = "Choose Bard Song Timing Preset")]
	private static BardAscendedSongTiming SongTimings { get; set; }

	[Range(1, 45, ConfigUnitType.Seconds, 1)]
	[RotationConfig(CombatType.PvE, Name = "Custom Wanderer's Minuet Uptime", Parent = nameof(SongTimings),
		ParentValue = BardAscendedSongTiming.Custom)]
	private float CustomWandTime { get; set; } = 45f;

	[Range(1, 45, ConfigUnitType.Seconds, 1)]
	[RotationConfig(CombatType.PvE, Name = "Custom Mage's Ballad Uptime", Parent = nameof(SongTimings),
		ParentValue = BardAscendedSongTiming.Custom)]
	private float CustomMageTime { get; set; } = 45f;

	[Range(1, 45, ConfigUnitType.Seconds, 1)]
	[RotationConfig(CombatType.PvE, Name = "Custom Army's Paeon Uptime", Parent = nameof(SongTimings),
		ParentValue = BardAscendedSongTiming.Custom)]
	private float CustomArmyTime { get; set; } = 45f;

	[RotationConfig(CombatType.PvE, Name = "Custom Wanderer's Weave Slot Timing", Parent = nameof(SongTimings),
		ParentValue = BardAscendedSongTiming.Custom)]
	private BardAscendedWandererWeave WanderersWeave { get; set; } = BardAscendedWandererWeave.Early;

	[RotationConfig(CombatType.PvE, Name = "Enable PrepullHeartbreak Shot? - Use with BMR Auto Attack Manager")]
	private bool EnablePrepullHeartbreakShot { get; set; } = true;

	[RotationConfig(CombatType.PvE, Name = "Use Warden's Paean on other players")]
	private bool UseWardenPaeanOnParty { get; set; } = true;

	[RotationConfig(CombatType.PvE, Name = "Prevent the use of defense abilities during burst")]
	private bool PreventDefenseDuringBurst { get; set; } = true;

	private static readonly BardAscendedPotions AscendedPotions = new();

	[RotationConfig(CombatType.PvE, Name = "Enable Potion Usage")]
	private bool PotionUsageEnabled
	{
		get => AscendedPotions.Enabled;
		set => AscendedPotions.Enabled = value;
	}

	[RotationConfig(CombatType.PvE, Name = "Potion Usage Preset", Parent = nameof(PotionUsageEnabled))]
	private BardAscendedPotionTiming PotionUsagePreset
	{
		get => AscendedPotions.Timing;
		set => AscendedPotions.Timing = value;
	}

	[Range(0, 20, ConfigUnitType.Seconds, 0)]
	[RotationConfig(CombatType.PvE,
		Name = "Use Opener Potion at minus time in seconds",
		Parent = nameof(PotionUsageEnabled))]
	private float OpenerPotionTime
	{
		get => AscendedPotions.OpenerPotionTime;
		set => AscendedPotions.OpenerPotionTime = value;
	}

	[Range(0, 1200, ConfigUnitType.Seconds, 0)]
	[RotationConfig(CombatType.PvE, Name = "Use 1st Custom Potion at seconds", Parent = nameof(PotionUsagePreset),
		ParentValue = BardAscendedPotionTiming.Custom)]
	private float FirstPotionTiming
	{
		get;
		set
		{
			field = value;
			UpdateCustomTimings();
		}
	}

	[Range(0, 1200, ConfigUnitType.Seconds, 0)]
	[RotationConfig(CombatType.PvE, Name = "Use 2nd Custom Potion at seconds", Parent = nameof(PotionUsagePreset),
		ParentValue = BardAscendedPotionTiming.Custom)]
	private float SecondPotionTiming
	{
		get;
		set
		{
			field = value;
			UpdateCustomTimings();
		}
	}

	[Range(0, 1200, ConfigUnitType.Seconds, 0)]
	[RotationConfig(CombatType.PvE, Name = "Use 3rd Custom Potion at seconds", Parent = nameof(PotionUsagePreset),
		ParentValue = BardAscendedPotionTiming.Custom)]
	private float ThirdPotionTiming
	{
		get;
		set
		{
			field = value;
			UpdateCustomTimings();
		}
	}

	private void UpdateCustomTimings()
	{
		AscendedPotions.CustomTimings = new Potions.CustomTimingsData
		{
			Timings = [FirstPotionTiming, SecondPotionTiming, ThirdPotionTiming]
		};
	}

	[RotationConfig(CombatType.PvE, Name = "Enable Sandbag Mode?")]
	private static bool EnableSandbagMode { get; set; } = false;

	#endregion

	#region Main Combat Logic

	#region Countdown Logic
	protected override IAction? CountDownAction(float remainTime)
	{
		RefreshCombatCycleState();
		IsFirstCycle = true;
		ResetStrictOpenerForCountdown(remainTime);
		StartStrictOpenerForCountdown();

		if (TryUseOpenerCountdownAction(remainTime, out var openerAct)) return openerAct;
		if (ShouldUseCountdownPotionFallback()
			&& AscendedPotions.ShouldUsePotion(this, out var potionAct))
		{
			return potionAct;
		}

		IAction? act;
		if (!_isStrictOpenerActive
			&& SongTimings == BardAscendedSongTiming.AdjustedStandard
			&& remainTime <= BardAscendedOpenerController.AdjustedStandardPrepullHeartbreakWindowSeconds)
		{
			if (ActiveBloodletterVariant.CanUse(out act)) return act;
		}

		if (!_isStrictOpenerActive
			&& Is369
			&& EnablePrepullHeartbreakShot
			&& remainTime < Cycle369PrepullHeartbreakWindowSeconds
			&& ActiveBloodletterVariant.CanUse(out act))
		{
			return act;
		}

		return !_isStrictOpenerActive && remainTime <= CountdownDotWindowSeconds && TryUseDoTs(out act)
			? act
			: base.CountDownAction(remainTime);
	}

	#endregion

	#region oGCD Logic

	protected override bool EmergencyAbility(IAction nextGCD, out IAction? act)
	{
		RefreshCombatCycleState();
		act = null;

		if (StatusHelper.PlayerHasStatus(false, StatusID.Doom)
			&& TheWardensPaeanPvE.CanUse(out act))
		{
			return true;
		}

		if (TryUseOpenerAbility(out act)) return true;
		if (AscendedPotions.ShouldUsePotion(this, out act)) return true;

		if (IsFirstCycle && InArmys && !RadiantFinalePvE.Cooldown.IsCoolingDown) IsFirstCycle = false;

		if (!CanWeave) return false;
		return TryUseEmpyrealArrow(out act)
			   || TryUseBarrage(out act)
			   || TryUsePitchPerfect(out act)
			   || base.EmergencyAbility(nextGCD, out act);
	}

	[RotationDesc(ActionID.TheWardensPaeanPvE)]
	protected override bool DispelAbility(IAction nextGCD, out IAction? act)
	{
		if (UseWardenPaeanOnParty && TheWardensPaeanPvE.CanUse(out act))
		{
			return true;
		}

		return base.DispelAbility(nextGCD, out act);
	}

	[RotationDesc(ActionID.NaturesMinnePvE)]
	protected override bool HealSingleAbility(IAction nextGCD, out IAction? act)
	{
		if (NaturesMinnePvE.CanUse(out act))
		{
			return true;
		}

		return base.HealSingleAbility(nextGCD, out act);
	}

	[RotationDesc(ActionID.TroubadourPvE)]
	protected override bool DefenseAreaAbility(IAction nextGCD, out IAction? act)
	{
		if ((!PreventDefenseDuringBurst || (!InBurst && !IsDirtyStartRecoveryBurstWindow)) && TroubadourPvE.CanUse(out act))
		{
			return true;
		}

		return base.DefenseAreaAbility(nextGCD, out act);
	}

	protected override bool GeneralAbility(IAction nextGCD, out IAction? act)
	{
		RefreshCombatCycleState();
		act = null;
		return TryUseSong(out act)
			   || base.GeneralAbility(nextGCD, out act);
	}

	protected override bool AttackAbility(IAction nextGcd, out IAction? act)
	{
		RefreshCombatCycleState();
		act = null;
		if (!CanWeave) return false;
		if (TryUseOpenerAbility(out act)) return true;
		return TryUseRadiantFinale(out act)
			   || TryUseBattleVoice(out act)
			   || TryUseRagingStrikes(out act)
			   || TryUseHeartBreakShot(out act)
			   || TryUseSideWinder(out act)
			   || base.AttackAbility(nextGcd, out act);
	}

	#endregion

	#region GCD Logic

	protected override bool GeneralGCD(out IAction? act)
	{
		RefreshCombatCycleState();
		if (TryUseOpenerGcd(out act)) return true;
		if (TryUseIronJaws(out act)) return true;
		if (TryUseBurst(out act)) return true;
		if (TryUseAoeApexArrow(out act)
			|| TryUseAoeBlastArrow(out act)
			|| TryUseEnhancedAoeFiller(out act)) return true;
		if (TryUseDoTs(out act)) return true;
		if (TryUseAoE(out act)) return true;
		if (TryUseApexArrow(out act)
			|| TryUseBlastArrow(out act)) return true;
		if (TryUseResonantArrow(out act)) return true;
		return TryUseFiller(out act)
			   || base.GeneralGCD(out act);
	}

	#endregion

	#endregion

	#region Extra Methods

	private void StartStrictOpener()
	{
		if (IsCustom) return;
		if (_hasStrictOpenerEndedThisCycle) return;

		_openerState = BardAscendedOpenerState.Start(SongTimings);
		_isStrictOpenerActive = true;
	}

	private void StartStrictOpenerForCountdown()
	{
		if (_isStrictOpenerActive || _hasStrictOpenerEndedThisCycle) return;
		StartStrictOpener();
	}

	private void ResetStrictOpenerForCountdown(float remainTime)
	{
		if (InCombat) return;

		var isNewCountdown = remainTime > _lastCountdownRemainTime + CountdownResetToleranceSeconds;
		_lastCountdownRemainTime = remainTime;
		if (_isStrictOpenerActive && !isNewCountdown) return;
		if (_hasStrictOpenerEndedThisCycle && !isNewCountdown) return;

		ResetStrictOpenerProgress();
	}

	private void ResetStrictOpenerProgress()
	{
		_openerState = BardAscendedOpenerState.Start(SongTimings);
		_isStrictOpenerActive = false;
		_hasStrictOpenerEndedThisCycle = false;
	}

	private void ResetStrictOpenerTracking()
	{
		ResetStrictOpenerProgress();
		_lastCountdownRemainTime = 0f;
	}

	private void ResetStrictOpenerIfNeeded()
	{
		if (!InCombat)
		{
			if (Service.CountDownTime > 0f) return;

			ResetStrictOpenerTracking();
			return;
		}

		if (IsFirstCycle && !_isStrictOpenerActive && !_hasStrictOpenerEndedThisCycle)
		{
			StartStrictOpener();
		}
	}

	private void EndStrictOpener()
	{
		_openerState = _openerState.Complete();
		_isStrictOpenerActive = false;
		_hasStrictOpenerEndedThisCycle = true;
	}

	private void StartDirtyStartRecoveryIfNeeded()
	{
		if (!BardAscendedDecisionPolicy.ShouldUseDirtyStartRecovery(
				EnablePlannedFightMode,
				IsFirstCycle,
				CurrentSongPhase))
		{
			return;
		}

		_dirtyStartRecoveryState = BardAscendedDirtyStartRecoveryState.Armed;
		EndStrictOpener();
	}

	private void ResetDirtyStartRecovery()
	{
		_dirtyStartRecoveryState = BardAscendedDirtyStartRecoveryState.Inactive;
	}

	private void ClearDirtyStartRecoveryIfResolved()
	{
		if (!IsDirtyStartRecoveryActive) return;

		if (_dirtyStartRecoveryState is BardAscendedDirtyStartRecoveryState.Armed)
		{
			if (InWanderers) ResetDirtyStartRecovery();
			return;
		}

		if (!PlayerHasAnyDirtyStartRecoveryBurstStatus() && !WasLastDirtyStartRecoveryBurstAction())
		{
			ResetDirtyStartRecovery();
		}
	}

	private void MarkDirtyStartRecoveryBurstStarted()
	{
		if (IsDirtyStartRecoveryActive)
		{
			_dirtyStartRecoveryState = BardAscendedDirtyStartRecoveryState.BurstStarted;
		}
	}

	private bool PlayerHasAnyDirtyStartRecoveryBurstStatus()
	{
		return HasRagingStrikes || HasBattleVoice || HasRadiantFinale;
	}

	private bool WasLastDirtyStartRecoveryBurstAction()
	{
		return IsLastAbility(ActionID.RadiantFinalePvE)
			|| IsLastAbility(ActionID.BattleVoicePvE)
			|| IsLastAbility(ActionID.RagingStrikesPvE);
	}

	private bool IsDirtyStartRecoveryActive =>
		_dirtyStartRecoveryState is not BardAscendedDirtyStartRecoveryState.Inactive;

	private bool IsDirtyStartRecoveryBurstWindow =>
		IsDirtyStartRecoveryActive
		&& (_dirtyStartRecoveryState is BardAscendedDirtyStartRecoveryState.BurstStarted
			|| PlayerHasAnyDirtyStartRecoveryBurstStatus()
			|| WasLastDirtyStartRecoveryBurstAction());

	private bool CanUseDirtyStartRecoveryRadiantEncore =>
		IsDirtyStartRecoveryBurstWindow && IsLastAbility(ActionID.RadiantFinalePvE);

	private static bool ShouldUseCountdownPotionFallback()
	{
		return SongTimings is BardAscendedSongTiming.Standard or BardAscendedSongTiming.Custom;
	}

	private BardAscendedOpenerInput BuildOpenerGcdInput()
	{
		return BardAscendedOpenerInput.ForGcd(_openerState);
	}

	private BardAscendedOpenerInput BuildOpenerAbilityInput()
	{
		return BardAscendedOpenerInput.ForAbility(
			_openerState,
			pitchPerfectStacks: Repertoire,
			willGainPitchPerfectStackBeforeNextWeave: EmpyrealArrowPvE.Cooldown.WillHaveOneChargeGCD(1),
			isEmpyrealArrowNextScriptedAbility: IsNextScriptedOpenerAbility(BardAscendedOpenerAction.EmpyrealArrow),
			willBurstBuffEndBeforeNextGcd: BurstEndGCD(1) || SongEndAfter(WandRemainTime - DataCenter.CalculatedActionAhead + AnimationLock));
	}

	private bool IsNextScriptedOpenerAbility(BardAscendedOpenerAction action)
	{
		var request = BardAscendedOpenerController.GetNextRequest(BardAscendedOpenerInput.ForAbility(_openerState));
		return request.Kind == BardAscendedOpenerResultKind.Continue
			   && request.Action == action;
	}

	private bool TryUseOpenerGcd(out IAction? act)
	{
		act = null;
		ResetStrictOpenerIfNeeded();
		if (!_isStrictOpenerActive) return false;

		var request = BardAscendedOpenerController.GetNextRequest(BuildOpenerGcdInput());
		return TryUseRequestedOpenerAction(request, out act);
	}

	private bool TryUseOpenerAbility(out IAction? act)
	{
		act = null;
		ResetStrictOpenerIfNeeded();
		if (!_isStrictOpenerActive || !CanWeave) return false;

		var request = BardAscendedOpenerController.GetNextRequest(BuildOpenerAbilityInput());
		return TryUseRequestedOpenerAction(request, out act);
	}

	private bool TryUseOpenerCountdownAction(float remainTime, out IAction? act)
	{
		act = null;
		if (!_isStrictOpenerActive) return false;

		var abilityRequest = BardAscendedOpenerController.GetNextRequest(BuildOpenerAbilityInput());
		var hasPendingPrepull = BardAscendedOpenerController.HasPendingCountdownPrepullRequest(abilityRequest);
		if (BardAscendedOpenerController.IsCountdownPrepullRequestReady(SongTimings, abilityRequest, remainTime)
			&& TryUseRequestedOpenerAction(abilityRequest, out act))
		{
			return true;
		}

		if (hasPendingPrepull) return false;
		if (remainTime > CountdownDotWindowSeconds) return false;

		var gcdRequest = BardAscendedOpenerController.GetNextRequest(BuildOpenerGcdInput());
		return TryUseRequestedOpenerAction(gcdRequest, out act);
	}

	private bool TryUseRequestedOpenerAction(BardAscendedOpenerResult request, out IAction? act)
	{
		act = null;

		if (request.Kind == BardAscendedOpenerResultKind.Complete
			|| request.Kind == BardAscendedOpenerResultKind.Break)
		{
			EndStrictOpener();
			return false;
		}

		if (request.Kind == BardAscendedOpenerResultKind.Skip)
		{
			_openerState = request.NextState;
			return false;
		}

		if (request.Kind != BardAscendedOpenerResultKind.Continue) return false;

		if (request.Action == BardAscendedOpenerAction.Potion)
		{
			if (!TryUseStrictOpenerPotion(out act))
			{
				_openerState = request.NextState;
				return false;
			}

			_openerState = request.NextState;
			return true;
		}

		if (!TryResolveOpenerAction(request.Action, out var requestedAction))
		{
			EndStrictOpener();
			return false;
		}

		if (!requestedAction.CanUse(out act, skipComboCheck: ShouldSkipComboForOpenerAction(request.Action)))
		{
			EndStrictOpener();
			return false;
		}

		_openerState = request.NextState;
		if (_openerState.IsTerminal) _isStrictOpenerActive = false;
		return true;
	}

	private bool TryUseStrictOpenerPotion(out IAction? act)
	{
		act = null;
		if (!PotionUsageEnabled || IsMedicated) return false;
		return UseBurstMedicine(out act);
	}

	private bool TryResolveOpenerAction(BardAscendedOpenerAction action, out IBaseAction? requestedAction)
	{
		requestedAction = action switch
		{
			BardAscendedOpenerAction.FlexibleFiller => HasHawksEye ? RefulgentArrowPvE : ActiveFiller,
			BardAscendedOpenerAction.Stormbite => Stormbite,
			BardAscendedOpenerAction.CausticBite => CausticBite,
			BardAscendedOpenerAction.RefulgentArrow => RefulgentArrowPvE,
			BardAscendedOpenerAction.IronJaws => IronJawsPvE,
			BardAscendedOpenerAction.RadiantEncore => RadiantEncorePvE,
			BardAscendedOpenerAction.ResonantArrow => ResonantArrowPvE,
			BardAscendedOpenerAction.HeartbreakShot => ActiveBloodletterVariant,
			BardAscendedOpenerAction.TheWanderersMinuet => TheWanderersMinuetPvE,
			BardAscendedOpenerAction.EmpyrealArrow => EmpyrealArrowPvE,
			BardAscendedOpenerAction.RadiantFinale => RadiantFinalePvE,
			BardAscendedOpenerAction.BattleVoice => BattleVoicePvE,
			BardAscendedOpenerAction.RagingStrikes => RagingStrikesPvE,
			BardAscendedOpenerAction.Barrage => BarragePvE,
			BardAscendedOpenerAction.Sidewinder => SidewinderPvE,
			BardAscendedOpenerAction.PitchPerfect => PitchPerfectPvE,
			_ => null
		};

		return requestedAction != null;
	}

	private static bool ShouldSkipComboForOpenerAction(BardAscendedOpenerAction action)
	{
		return action is BardAscendedOpenerAction.RadiantEncore
			or BardAscendedOpenerAction.ResonantArrow
			or BardAscendedOpenerAction.PitchPerfect;
	}

	#region GCD Skills

	#region DoTs

	private float EffectiveTargetTimeToKill
	{
		get
		{
			if (EnablePlannedFightMode && PlannedFightKillTime > 0f)
			{
				return Math.Max(0f, PlannedFightKillTime - DataCenter.CombatTimeRaw);
			}

			return CurrentTarget?.GetTTK() ?? float.NaN;
		}
	}

	private void RefreshCombatCycleState()
	{
		if (!InCombat)
		{
			var hadCombatCycleState = HasCombatCycleState;
			HasCombatCycleState = false;
			LastCombatTimeRaw = 0f;
			ResetDirtyStartRecovery();
			if (hadCombatCycleState && Service.CountDownTime <= 0f)
			{
				ResetStrictOpenerTracking();
			}
			return;
		}

		if (BardAscendedDecisionPolicy.ShouldStartFirstCycle(
				isInCombat: true,
				hasCombatState: HasCombatCycleState,
				currentCombatTime: DataCenter.CombatTimeRaw,
				previousCombatTime: LastCombatTimeRaw))
		{
			IsFirstCycle = true;
			StartDirtyStartRecoveryIfNeeded();
			if (!_isStrictOpenerActive && !IsDirtyStartRecoveryActive)
			{
				StartStrictOpener();
			}
		}

		ClearDirtyStartRecoveryIfResolved();
		HasCombatCycleState = true;
		LastCombatTimeRaw = DataCenter.CombatTimeRaw;
	}

	private bool WouldUseIronJaws => HasTargetAwareIronJawsCandidate();

	private bool WouldUseDoTs => HasTargetAwareDoTCandidate();

	private static bool TargetHasDoT(IBattleChara target, IBaseAction action)
	{
		var targetStatusProvide = action.Setting.TargetStatusProvide;
		return targetStatusProvide != null
			   && target.HasStatus(true, targetStatusProvide);
	}

	private static bool HasTargetStatusData(IBaseAction action)
	{
		return action.Setting.TargetStatusProvide != null;
	}

	private static bool TargetDoTEnding(IBattleChara target, IBaseAction action)
	{
		var targetStatusProvide = action.Setting.TargetStatusProvide;
		return TargetHasDoT(target, action)
			   && targetStatusProvide != null
			   && target.WillStatusEndGCD(1, DoTEndBuffer, true, targetStatusProvide);
	}

	private bool CanUseDoTOnTarget(IBattleChara target)
	{
		return !DoTsBoss || target.IsBossFromIcon();
	}

	private float GetDotTargetTimeToKill(IBattleChara target)
	{
		if (EnablePlannedFightMode && PlannedFightKillTime > 0f)
		{
			return Math.Max(0f, PlannedFightKillTime - DataCenter.CombatTimeRaw);
		}

		return target.GetTTK();
	}

	private static bool IsDotBossTarget(IBattleChara target)
	{
		return target.IsBossFromIcon()
			   || target.IsBossFromTTK();
	}

	private static bool TryPreviewActionTarget(
		IBaseAction action,
		out IBattleChara target,
		bool skipStatusProvideCheck = false)
	{
		target = null!;
		var wasActionPreview = IBaseAction.ActionPreview;

		try
		{
			IBaseAction.ActionPreview = true;
			if (!action.CanUse(
					out _,
					skipStatusProvideCheck: skipStatusProvideCheck))
			{
				return false;
			}

			var previewTarget = action.PreviewTarget?.Target;
			if (previewTarget == null)
			{
				return false;
			}

			target = previewTarget;
			return true;
		}
		finally
		{
			IBaseAction.ActionPreview = wasActionPreview;
		}
	}

	private bool ShouldUseIronJawsOnTarget(IBattleChara target)
	{
		if (!CanUseDoTOnTarget(target)) return false;
		if (!HasTargetStatusData(Stormbite) || !HasTargetStatusData(CausticBite)) return false;
		if (!TargetHasDoT(target, Stormbite) || !TargetHasDoT(target, CausticBite)) return false;
		if (!BardAscendedDecisionPolicy.ShouldRefreshIronJaws(
				GetDotTargetTimeToKill(target),
				IsDotBossTarget(target),
				CanUseEnhancedFiller))
		{
			return false;
		}

		var hasEndingDoT = TargetDoTEnding(target, Stormbite)
						   || TargetDoTEnding(target, CausticBite);
		if (!InBurst && hasEndingDoT) return true;
		return InBurst && BurstEndGCD(1) && !IsLastGCD(ActionID.IronJawsPvE);
	}

	private bool ShouldUseStormbiteOnTarget(IBattleChara target)
	{
		if (!CanUseDoTOnTarget(target)) return false;
		if (!HasTargetStatusData(Stormbite) || !HasTargetStatusData(CausticBite)) return false;

		var hasStormbite = TargetHasDoT(target, Stormbite);
		var hasCausticBite = TargetHasDoT(target, CausticBite);
		if (hasStormbite && (IronJawsPvE.EnoughLevel || !TargetDoTEnding(target, Stormbite)))
		{
			return false;
		}

		var targetTimeToKill = GetDotTargetTimeToKill(target);
		var isBossTarget = IsDotBossTarget(target);
		if (!hasStormbite && !hasCausticBite)
		{
			return BardAscendedDecisionPolicy.ShouldApplyBothDots(
				targetTimeToKill,
				isBossTarget,
				CanUseEnhancedFiller);
		}

		return BardAscendedDecisionPolicy.ShouldApplyStormbiteOnly(targetTimeToKill, isBossTarget);
	}

	private bool ShouldUseCausticBiteOnTarget(IBattleChara target)
	{
		if (!CanUseDoTOnTarget(target)) return false;
		if (!HasTargetStatusData(Stormbite) || !HasTargetStatusData(CausticBite)) return false;

		var hasStormbite = TargetHasDoT(target, Stormbite);
		var hasCausticBite = TargetHasDoT(target, CausticBite);
		if (hasCausticBite && (IronJawsPvE.EnoughLevel || !TargetDoTEnding(target, CausticBite)))
		{
			return false;
		}

		var targetTimeToKill = GetDotTargetTimeToKill(target);
		var isBossTarget = IsDotBossTarget(target);
		if (!hasStormbite && !hasCausticBite)
		{
			return BardAscendedDecisionPolicy.ShouldApplyBothDots(
				targetTimeToKill,
				isBossTarget,
				CanUseEnhancedFiller);
		}

		return BardAscendedDecisionPolicy.ShouldApplyCausticOnly(targetTimeToKill, isBossTarget);
	}

	private bool HasTargetAwareIronJawsCandidate()
	{
		return IronJawsPvE.EnoughLevel
			   && TryPreviewActionTarget(IronJawsPvE, out var target, skipStatusProvideCheck: true)
			   && ShouldUseIronJawsOnTarget(target);
	}

	private bool HasTargetAwareStormbiteCandidate()
	{
		return TryPreviewActionTarget(Stormbite, out var stormbiteTarget, skipStatusProvideCheck: true)
			   && ShouldUseStormbiteOnTarget(stormbiteTarget);
	}

	private bool HasTargetAwareCausticBiteCandidate()
	{
		return TryPreviewActionTarget(CausticBite, out var causticTarget, skipStatusProvideCheck: true)
			   && ShouldUseCausticBiteOnTarget(causticTarget);
	}

	private bool HasTargetAwareDoTCandidate()
	{
		return HasTargetAwareStormbiteCandidate()
			   || HasTargetAwareCausticBiteCandidate();
	}

	private bool TryUseIronJaws(out IAction? act)
	{
		act = null;
		return HasTargetAwareIronJawsCandidate()
			   && IronJawsPvE.CanUse(out act, skipStatusProvideCheck: true);
	}

	private bool TryUseDoTs(out IAction? act)
	{
		act = null;
		if (HasTargetAwareStormbiteCandidate()
			&& Stormbite.CanUse(out act, skipStatusProvideCheck: true))
		{
			return true;
		}

		return HasTargetAwareCausticBiteCandidate()
			   && CausticBite.CanUse(out act, skipStatusProvideCheck: true);
	}

	#endregion

	#region Burst GCDs

	private bool TryUseBurst(out IAction? act)
	{
		act = null;
		if (!InBurst && !IsDirtyStartRecoveryBurstWindow) return false;
		if (TryUseRadiantEncore(out act)) return true;
		if (TryUseApexArrow(out act) || TryUseBlastArrow(out act)) return true;
		if (TryUseResonantArrow(out act)) return true;
		return TryUseEnhancedFiller(out act);
	}

	private static BardAscendedSongPhase CurrentSongPhase =>
		Song switch
		{
			Song.WanderersMinuet => BardAscendedSongPhase.WanderersMinuet,
			Song.MagesBallad => BardAscendedSongPhase.MagesBallad,
			Song.ArmysPaeon => BardAscendedSongPhase.ArmysPaeon,
			_ => BardAscendedSongPhase.None
		};

	private BardAscendedApexDecisionInput ApexDecisionInput => new(
		SongPhase: CurrentSongPhase,
		SoulVoice: SoulVoice,
		IsInBurst: InBurst,
		WouldUseIronJaws: WouldUseIronJaws,
		SongSecondsRemaining: SongTime,
		TargetSecondsRemaining: EffectiveTargetTimeToKill,
		WeaponTotalSeconds: WeaponTotal,
		WouldUseEnhancedFiller: CanUseEnhancedFiller,
		NoFutureBlastPossible: !float.IsNaN(EffectiveTargetTimeToKill) && EffectiveTargetTimeToKill <= WeaponTotal);

	private bool CanSpendSoulVoice =>
		BardAscendedDecisionPolicy.ShouldSpendApex(ApexDecisionInput);

	private bool TryUseApexArrow(out IAction? act)
	{
		act = null;
		if (IsInSandbagMode || !CanSpendSoulVoice) return false;
		return ApexArrowPvE.CanUse(out act);
	}

	private bool TryUseAoeApexArrow(out IAction? act)
	{
		act = null;
		if (IsInSandbagMode || SoulVoice < BardAscendedDecisionPolicy.ApexBlastReadySoulVoice)
		{
			return false;
		}

		return ApexArrowPvE.CanUse(out act, skipAoeCheck: true)
			   && HasEnoughGcdAoETargets(act);
	}

	private bool TryUseBlastArrow(out IAction? act)
	{
		act = null;
		if (!BardAscendedDecisionPolicy.ShouldUseBlastArrow(
				BlastArrowPvEReady,
				WouldUseDoTs,
				WouldUseIronJaws))
		{
			return false;
		}

		return BlastArrowPvE.CanUse(out act, skipComboCheck: true);
	}

	private bool TryUseAoeBlastArrow(out IAction? act)
	{
		act = null;
		if (IsInSandbagMode || !BlastArrowPvEReady || WouldUseIronJaws)
		{
			return false;
		}

		return BlastArrowPvE.CanUse(out act, skipAoeCheck: true, skipComboCheck: true)
			   && HasEnoughGcdAoETargets(act);
	}

	private bool TryUseRadiantEncore(out IAction? act)
	{
		act = null;
		if (!HasRadiantFinale && !CanUseDirtyStartRecoveryRadiantEncore) return false;
		if (!InBurst && !IsDirtyStartRecoveryBurstWindow) return false;
		return RadiantEncorePvE.CanUse(out act, skipComboCheck: true);
	}

	private bool TryUseResonantArrow(out IAction? act)
	{
		act = null;
		if (!HasResonantArrow) return false;
		return ResonantArrowPvE.CanUse(out act, skipComboCheck: true);
	}

	#endregion

	#region Filler GCDs

	private static bool HasEnoughGcdAoETargets(IAction? act) =>
		act is IBaseAction baseAction
		&& BardAscendedDecisionPolicy.ShouldUseGcdAoE(baseAction.Target.AffectedTargets.Length);

	private static bool HasEnoughOgcdAoETargets(IAction? act) =>
		act is IBaseAction baseAction
		&& BardAscendedDecisionPolicy.ShouldUseOgcdAoE(baseAction.Target.AffectedTargets.Length);

	private bool TryUseEnhancedFiller(out IAction? act)
	{
		act = null;
		if (IsInSandbagMode || !CanUseEnhancedFiller || WouldUseDoTs) return false;

		if (TryUseEnhancedAoeFiller(out act)) return true;

		var procArrow = RefulgentArrowPvE.EnoughLevel ? RefulgentArrowPvE : StraightShotPvE;
		return procArrow.CanUse(out act, skipComboCheck: true);
	}

	private bool TryUseEnhancedAoeFiller(out IAction? act)
	{
		act = null;
		if (IsInSandbagMode || !CanUseEnhancedFiller) return false;

		var procAoE = ShadowbitePvE.EnoughLevel ? ShadowbitePvE : WideVolleyPvE;
		if (procAoE.CanUse(out var procAoEAct, skipAoeCheck: true, skipComboCheck: true) && HasEnoughGcdAoETargets(procAoEAct))
		{
			act = procAoEAct;
			return true;
		}

		return false;
	}

	private bool TryUseAoE(out IAction? act)
	{
		act = null;
		if (TryUseEnhancedAoeFiller(out act)) return true;

		var aoeAction = LadonsbitePvE.EnoughLevel ? LadonsbitePvE : QuickNockPvE;
		if (aoeAction.CanUse(out var aoeActionAct, skipAoeCheck: true) && HasEnoughGcdAoETargets(aoeActionAct))
		{
			act = aoeActionAct;
			return true;
		}

		return false;
	}

	private bool TryUseFiller(out IAction? act)
	{
		act = null;
		if (IsInSandbagMode) return false;
		if (TryUseAoE(out act)) return true;
		if (TryUseEnhancedFiller(out act)) return true;
		if (!BardAscendedDecisionPolicy.ShouldUseFiller(
				hasEnhancedFiller: false,
				hasResonantReady: HasResonantArrow))
		{
			return false;
		}

		return ActiveFiller.CanUse(out act, skipComboCheck: true);
	}

	#endregion

	#endregion

	#region oGCD Abilities

	#region Emergency Abilities

	private bool TryUseBarrage(out IAction? act)
	{
		act = null;
		if (IsInSandbagMode || (!InBurst && !IsDirtyStartRecoveryBurstWindow)) return false;

		if (HasHawksEye && !BurstEndGCD(3)) return false;

		return BarragePvE.CanUse(out act) && CanWeave;
	}

	private bool CanUseEmpyrealArrow
	{
		get
		{
			if (WeaponRemain <= DataCenter.CalculatedActionAhead + Math.Max(AnimationLock, 0.6f)) return false;

			return CanWeave && EmpyrealArrowPvE.Cooldown.HasOneCharge;
		}
	}

	private bool EmpyrealArrowTimingCheck
	{
		get
		{
			if (UsesStandardBurstPath)
			{
				return true;
			}

			if (!Is369)
			{
				return false;
			}

			if (InWanderers)
			{
				return InBurst || RagingStrikesPvE.Cooldown.IsCoolingDown;
			}
			if (InMages)
			{
				return IsFirstCycle ? EnoughWeaveTime : !SongEndAfter(MageRemainTime);
			}

			return InArmys && EnoughWeaveTime;
		}
	}

	private bool TryUseEmpyrealArrow(out IAction? act)
	{
		act = null;
		if (IsInSandbagMode) return false;

		if (NoSong) return false;

		if (EmpyrealArrowTimingCheck && CanUseEmpyrealArrow)
		{
			return EmpyrealArrowPvE.CanUse(out act);
		}

		return false;
	}

	#endregion

	#region Songs

	private bool ShouldSwapSong
	{
		get
		{
			if (NoSong) return true;
			if (InWanderers) return SongEndAfter(WandRemainTime - Math.Max(DataCenter.CalculatedActionAhead, AnimationLock));
			if (InMages) return SongEndAfter(MageRemainTime);
			return InArmys && SongEndAfter(ArmyRemainTime) && CanLateWeave;
		}
	}
	private static bool ShouldBlockSongSwap(ActionID prev1, ActionID prev2) =>
		!EnableSandbagMode && (IsLastAbility(prev1) || IsLastAbility(prev2));
	private bool TryUseSong(out IAction? act)
	{
		act = null;

		if (!HasSongActions)
			return false;

		if (NoSong
			&& IsFirstCycle
			&& !TheWanderersMinuetPvE.EnoughLevel
			&& TryUseFirstAvailableSong(out act))
		{
			return true;
		}

		if (!NoSong && !ShouldSwapSong)
			return false;

		return TheWanderersMinuetPvE.EnoughLevel && TryUseWanderersMinuet(out act)
			   || MagesBalladPvE.EnoughLevel && TryUseMagesBallad(out act)
			   || ArmysPaeonPvE.EnoughLevel && TryUseArmys(out act);
	}

	private bool TryUseFirstAvailableSong(out IAction? act)
	{
		act = null;
		if (MagesBalladPvE.EnoughLevel) return MagesBalladPvE.CanUse(out act);
		return ArmysPaeonPvE.EnoughLevel && ArmysPaeonPvE.CanUse(out act);
	}

	private bool CanUseWanderersMinuet
	{
		get
		{
			if (NoSong && IsFirstCycle)
			{
				if (IsStandardTiming) return true;
				if (IsCustom) return (WanderersWeave == BardAscendedWandererWeave.Early && CanEarlyWeave)
					|| (WanderersWeave == BardAscendedWandererWeave.Late && CanLateWeave);
				if (Is369) return CanLateWeave;
			}

			if (InArmys) return ShouldSwapSong;

			return NoSong && ArmysPaeonPvE.Cooldown.IsCoolingDown && MagesBalladPvE.Cooldown.IsCoolingDown;
		}
	}

	private bool TryUseWanderersMinuet(out IAction? act)
	{
		act = null;
		if (ShouldBlockSongSwap(ActionID.ArmysPaeonPvE, ActionID.MagesBalladPvE)) return false;

		return CanUseWanderersMinuet && TheWanderersMinuetPvE.CanUse(out act);
	}

	private bool CanUseMagesBallad
	{
		get
		{
			if (InWanderers && ShouldSwapSong)
			{
				return (Repertoire == 0
						|| IsLastAbility(ActionID.PitchPerfectPvE)
						|| !HasHostilesInMaxRange
						|| EnableSandbagMode) && CanLateWeave;
			}

			if (InArmys && ShouldSwapSong) return TheWanderersMinuetPvE.Cooldown.IsCoolingDown;

			return NoSong && (TheWanderersMinuetPvE.Cooldown.IsCoolingDown || ArmysPaeonPvE.Cooldown.IsCoolingDown);
		}
	}
	private bool TryUseMagesBallad(out IAction? act)
	{
		act = null;
		if (ShouldBlockSongSwap(ActionID.ArmysPaeonPvE, ActionID.TheWanderersMinuetPvE)) return false;

		return CanUseMagesBallad && MagesBalladPvE.CanUse(out act);
	}

	private bool CanUseArmysPaeon
	{
		get
		{
			if (EnableSandbagMode) return InMages && ShouldSwapSong;

			if (UsesStandardBurstPath)
			{
				if (InMages) return ShouldSwapSong;

				if (InWanderers) return ShouldSwapSong && MagesBalladPvE.Cooldown.IsCoolingDown;

				if (NoSong) return TheWanderersMinuetPvE.Cooldown.IsCoolingDown || MagesBalladPvE.Cooldown.IsCoolingDown;
			}

			if (!ShouldSwapSong) return false;
			if (!Is369) return false;

			if (IsFirstCycle)
			{
				return CanLateWeave
					   || IsLastAbility(ActionID.EmpyrealArrowPvE);
			}
			return EnoughWeaveTime;
		}
	}

	private bool TryUseArmys(out IAction? act)
	{
		act = null;
		if (ShouldBlockSongSwap(ActionID.TheWanderersMinuetPvE, ActionID.MagesBalladPvE)) return false;

		return CanUseArmysPaeon && ArmysPaeonPvE.CanUse(out act);
	}

	#endregion

	#region Buffs

	private static bool RecastIsLessThanGCD(IBaseAction action)
	{
		if (!action.Cooldown.IsCoolingDown) return true;

		return action.Cooldown.RecastTimeRemain < WeaponTotal;
	}

	private static bool ElapsedIsMoreThanGCD(IBaseAction action)
	{
		if (!action.Cooldown.IsCoolingDown) return false;

		return action.Cooldown.RecastTimeElapsedRaw > WeaponTotal;
	}

	private bool CanStartBurstWithRadiantFinale(out IAction? act)
	{
		act = null;
		if (!CanBurst) return false;
		if (!InWanderers && !IsDirtyStartRecoveryActive) return false;

		if (UsesStandardBurstPath)
		{
			var canStart = IsFirstCycle
				? HasBattleVoice
				: ElapsedIsMoreThanGCD(TheWanderersMinuetPvE) && RecastIsLessThanGCD(BattleVoicePvE);

			return canStart && RadiantFinalePvE.CanUse(out act);
		}

		if (!Is369) return false;

		var canStart369 = IsFirstCycle
			? !WouldUseDoTs
			: ElapsedIsMoreThanGCD(TheWanderersMinuetPvE) && RecastIsLessThanGCD(BattleVoicePvE);

		return canStart369 && RadiantFinalePvE.CanUse(out act);
	}

	private bool TryUseRadiantFinale(out IAction? act)
	{
		act = null;
		if (Is369 && (IsFirstCycle ? !CanLateWeave : !CanEarlyWeave)) return false;

		if (CanStartBurstWithRadiantFinale(out act))
		{
			MarkDirtyStartRecoveryBurstStarted();
			return true;
		}

		return false;
	}

	private bool CanStartBurstWithBattleVoice(out IAction? act)
	{
		act = null;
		if (!CanBurst) return false;
		if (!InWanderers && RadiantFinalePvE.EnoughLevel && !IsDirtyStartRecoveryActive) return false;
		var shouldWaitForRadiantFinale = BardAscendedDecisionPolicy.ShouldWaitForRadiantFinaleBeforeBattleVoice(
			RadiantFinalePvE.EnoughLevel,
			RadiantFinalePvE.CanUse(out _),
			HasRadiantFinale,
			IsLastAbility(ActionID.RadiantFinalePvE));

		if (UsesStandardBurstPath)
		{
			var canStart = IsFirstCycle
				? !HasRadiantFinale
				: !shouldWaitForRadiantFinale;

			return canStart && BattleVoicePvE.CanUse(out act);
		}

		if (!Is369) return false;

		var canStart369 = IsFirstCycle
			? !WouldUseDoTs && !shouldWaitForRadiantFinale
			: !shouldWaitForRadiantFinale;

		return canStart369 && BattleVoicePvE.CanUse(out act);
	}

	private bool TryUseBattleVoice(out IAction? act)
	{
		act = null;
		if (UsesStandardBurstPath && !CanLateWeave) return false;
		if (Is369 && (IsFirstCycle ? !CanEarlyWeave : !CanLateWeave)) return false;

		if (CanStartBurstWithBattleVoice(out act))
		{
			MarkDirtyStartRecoveryBurstStarted();
			return true;
		}

		return false;
	}

	private bool CanStartBurstWithRagingStrikes(out IAction? act)
	{
		act = null;
		if (!CanBurst) return false;

		var hasOtherBurst = false;
		var allOtherPresent = true;
		foreach (var status in BurstStatus)
		{
			if (status == StatusID.RagingStrikes) continue;
			hasOtherBurst = true;
			if (StatusHelper.PlayerHasStatus(true, status)) continue;
			allOtherPresent = false;
			break;
		}

		if (hasOtherBurst && !allOtherPresent) return false;

		return RagingStrikesPvE.CanUse(out act);
	}

	private bool TryUseRagingStrikes(out IAction? act)
	{
		act = null;
		if (!CanLateWeave) return false;

		if (CanStartBurstWithRagingStrikes(out act))
		{
			MarkDirtyStartRecoveryBurstStarted();
			return true;
		}

		return false;
	}

	#endregion

	#region Attack Abilities

	private bool TryUseBloodletterVariant(out IAction? act, bool usedUp)
	{
		if (RainOfDeathPvE.CanUse(out var rainOfDeathAct, usedUp: usedUp, skipAoeCheck: true) && HasEnoughOgcdAoETargets(rainOfDeathAct))
		{
			act = rainOfDeathAct;
			return true;
		}

		return ActiveBloodletterVariant.CanUse(out act, usedUp: usedUp);
	}

	private bool IsBloodletterBurstReservationActive()
	{
		return CanEnterBurstWindow || (InArmys && SongTime <= ArmyHeartbreakHoldThreshold);
	}

	private float GetBloodletterBurstEntryHorizon()
	{
		if (CanEnterBurstWindow) return 0f;
		return InArmys && SongTime <= ArmyHeartbreakHoldThreshold
			? Math.Max(0f, SongTime - ArmyRemainTime)
			: 0f;
	}

	private bool TryUseHeartBreakShot(out IAction? act)
	{
		act = null;
		if (IsInSandbagMode || !CanWeave || !EnoughWeaveTime) return false;

		var cooldown = ActiveBloodletterVariant.Cooldown;
		var willHaveMaxCharges = cooldown.WillHaveXCharges(
			BloodletterMax,
			HeartbreakChargeLookahead);
		var reservationActive = IsBloodletterBurstReservationActive();

		if (InBurst || !reservationActive)
		{
			return TryUseBloodletterVariant(out act, usedUp: true);
		}

		var canRecoverAfterSpend = BardAscendedDecisionPolicy.CanRecoverBloodletterChargesAfterSpend(new BardAscendedBloodletterRecoveryInput
		{
			CurrentCharges = cooldown.CurrentCharges,
			MaximumCharges = BloodletterMax,
			IsCooldownTicking = cooldown.IsCoolingDown,
			FirstChargeTimeRemaining = cooldown.RecastTimeRemainOneCharge,
			OneChargeRecastTime = cooldown.RecastTimeOneChargeRaw,
			RecoveryHorizon = GetBloodletterBurstEntryHorizon(),
		});
		return (canRecoverAfterSpend || willHaveMaxCharges) && TryUseBloodletterVariant(out act, usedUp: true);
	}

	private bool TryUseSideWinder(out IAction? act)
	{
		act = null;
		if (IsInSandbagMode) return false;
		if (!SidewinderPvE.Cooldown.WillHaveOneCharge(WeaponAhead)) return false;

		var rFWillHaveCharge = RadiantFinalePvE.Cooldown.IsCoolingDown
			&& RadiantFinalePvE.Cooldown.WillHaveOneCharge(SidewinderBuffLookahead);
		var bVWillHaveCharge = BattleVoicePvE.Cooldown.IsCoolingDown
			&& BattleVoicePvE.Cooldown.WillHaveOneCharge(SidewinderBuffLookahead);

		var noBurstIncoming = !rFWillHaveCharge && !bVWillHaveCharge && RagingStrikesPvE.Cooldown.IsCoolingDown;
		var rsExpiring = RagingStrikesPvE.Cooldown.IsCoolingDown && !HasRagingStrikes;
		if (!(InBurst || !RadiantFinalePvE.EnoughLevel || noBurstIncoming || rsExpiring)) return false;
		if (!EnoughWeaveTime) return false;
		return SidewinderPvE.CanUse(out act);
	}

	private bool TryUsePitchPerfect(out IAction? act)
	{
		act = null;
		if (IsInSandbagMode || Song != Song.WanderersMinuet) return false;

		if (!PitchPerfectPvE.CanUse(out act, skipAoeCheck: true, skipComboCheck: true)) return false;

		if (Repertoire == 3) return true;
		if (Repertoire == 2 && EmpyrealArrowPvE.Cooldown.WillHaveOneChargeGCD(1)) return true;

		return SongEndAfter(WandRemainTime - DataCenter.CalculatedActionAhead + AnimationLock) && WeaponRemain > LateWeaveWindow;
	}

	#endregion

	#endregion

	#region Miscellaneous

	private bool IsInSandbagMode =>
		EnableSandbagMode && (!InBurst || Song != Song.WanderersMinuet) &&
		((IsFirstCycle
		  && !RadiantFinalePvE.Cooldown.HasOneCharge
		  && !BattleVoicePvE.Cooldown.HasOneCharge
		  && !RagingStrikesPvE.Cooldown.HasOneCharge
		  && RadiantFinalePvE.Cooldown.IsCoolingDown
		  && BattleVoicePvE.Cooldown.IsCoolingDown
		  && RagingStrikesPvE.Cooldown.IsCoolingDown)
		 || (!IsFirstCycle
			 && !BattleVoicePvE.Cooldown.HasOneCharge
			 && !RagingStrikesPvE.Cooldown.HasOneCharge));

	private sealed class BardAscendedPotions : Potions
	{
		private BRD_Ascended? _rotation;

		public BardAscendedPotionTiming Timing { get; set; } = BardAscendedPotionTiming.Opener;

		public bool ShouldUsePotion(BRD_Ascended rotation, out IAction? act, bool clippingCheck = true)
		{
			_rotation = rotation;
			try
			{
				return base.ShouldUsePotion(rotation, out act, clippingCheck);
			}
			finally
			{
				_rotation = null;
			}
		}

		public override bool IsConditionMet()
		{
			if (IsFirstCycle)
			{
				return OpenerPotionTime > 0f || InWanderers;
			}

			if (_rotation?.InBurst == true) return true;
			return InOddMinuteWindow;
		}

		public override bool CanUseAtTime()
		{
			if (!Enabled) return false;

			foreach (var timing in BardAscendedDecisionPolicy.GetPotionTimings(Timing, CustomTimings.Timings))
			{
				if (IsTimingValid(timing)) return true;
			}

			return false;
		}

		protected override bool IsTimingValid(float timing)
		{
			if (timing > 0f
				&& DataCenter.CombatTimeRaw >= timing
				&& DataCenter.CombatTimeRaw - timing <= TimingWindowSeconds)
			{
				return true;
			}

			if (!IsOpenerPotion(timing)) return false;

			if (OpenerPotionTime == 0f)
			{
				return IsFirstCycle && InWanderers;
			}

			var countDown = Service.CountDownTime;
			return countDown > 0f && countDown <= OpenerPotionTime;
		}
	}

	#endregion

	#endregion

	#region Tracking Properties

	/// <inheritdoc />
	public override void DisplayRotationStatus()
	{
		ImGui.Text("===GCD Status===");
		ImGui.Text($"Weapon Remain: {WeaponRemain}");
		ImGui.Text($"Weapon Elapsed {WeaponElapsed}");
		ImGui.Text($"Calculated Action Ahead {DataCenter.CalculatedActionAhead}");
		ImGui.Text($"Can Weave {CanWeave}");
		ImGui.Text($"Enough Weave Time: {EnoughWeaveTime}");
		ImGui.Text($"Late Weave Window: {LateWeaveWindow}");
		ImGui.Text($"Can Late Weave: {CanLateWeave}");
		ImGui.Text($"Can Early Weave: {CanEarlyWeave}");
		ImGui.Text($"Empyreal Arrow Recast Remain: {EmpyrealArrowPvE.Cooldown.RecastTimeRemain} - {WeaponRemain} = {Math.Abs(EmpyrealArrowPvE.Cooldown.RecastTimeRemain - WeaponRemain)}");
		ImGui.Text($"Target Has Stormbite: {TargetHasDoT(Stormbite)}");
		ImGui.Text($"Target Has Caustic Bite: {TargetHasDoT(CausticBite)}");
		ImGui.Text($"In Burst: {InBurst}");
	}

	#endregion
}
