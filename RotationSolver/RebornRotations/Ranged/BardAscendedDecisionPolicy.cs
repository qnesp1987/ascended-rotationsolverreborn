using System.ComponentModel;

namespace RotationSolver.RebornRotations.Ranged;

internal enum BardAscendedSongTiming
{
	[Description("Standard 3-3-12 song timing")]
	Standard,

	[Description("Adjusted standard song timing")]
	AdjustedStandard,

	[Description("3-6-9 song timing")]
	Cycle369,

	[Description("Custom song timing")]
	Custom
}

internal enum BardAscendedWandererWeave
{
	[Description("Early Wanderer's Minuet weave")]
	Early,

	[Description("Late Wanderer's Minuet weave")]
	Late
}

internal enum BardAscendedSongPhase
{
	None,
	WanderersMinuet,
	MagesBallad,
	ArmysPaeon
}

internal enum BardAscendedPotionTiming
{
	[Description("Use potion in the opener")]
	Opener,

	[Description("Use potions at 2 and 8 minutes")]
	TwoEight,

	[Description("Use potions in the opener and at 6 minutes")]
	ZeroSix,

	[Description("Use potions in the opener, at 5 minutes, and at 10 minutes")]
	ZeroFiveTen,

	[Description("Use custom potion timings")]
	Custom
}

internal readonly record struct BardAscendedSongDurations(float Wanderers, float Mages, float Armys);

internal readonly record struct BardAscendedApexDecisionInput(
	BardAscendedSongPhase SongPhase,
	byte SoulVoice,
	bool IsInBurst,
	bool WouldUseIronJaws,
	float SongSecondsRemaining,
	float TargetSecondsRemaining,
	float WeaponTotalSeconds,
	bool WouldUseEnhancedFiller,
	bool NoFutureBlastPossible);

internal readonly record struct BardAscendedFreshDotAoeInput(
	bool HasResolvedNormalAoeCandidate,
	int NormalAoeAffectedTargets,
	float TargetSecondsRemaining,
	bool IsBossTarget);

internal readonly record struct BardAscendedBloodletterRecoveryInput
{
	internal int CurrentCharges { get; init; }
	internal int MaximumCharges { get; init; }
	internal bool IsCooldownTicking { get; init; }
	internal float FirstChargeTimeRemaining { get; init; }
	internal float OneChargeRecastTime { get; init; }
	internal float RecoveryHorizon { get; init; }
}

internal static class BardAscendedDecisionPolicy
{
	internal const float SongMaxDuration = 45f;
	internal const float StandardWanderersDuration = 42f;
	internal const float StandardMagesDuration = 42f;
	internal const float StandardArmysDuration = 33f;
	internal const float Cycle369WanderersDuration = 42f;
	internal const float Cycle369MagesDuration = 39f;
	internal const float Cycle369ArmysDuration = 36f;
	internal const float InitialDotMinimumTargetSeconds = 15f;
	internal const float InitialDotReplacingEnhancedFillerMinimumTargetSeconds = 18f;
	internal const float IronJawsMinimumTargetSeconds = 9f;
	internal const float IronJawsReplacingEnhancedFillerMinimumTargetSeconds = 12f;
	internal const float CausticOnlyMinimumTargetSeconds = 12f;
	internal const float StormbiteOnlyMinimumTargetSeconds = 15f;
	internal const float MageBalladApexEarliestSecondsRemaining = 18f;
	internal const float MageBalladApexLatestSecondsRemaining = 21f;
	internal const int ApexBlastReadySoulVoice = 80;
	internal const int SoulVoiceCap = 100;
	internal const int ApexBeatsBurstShotSoulVoice = 32;
	internal const int ApexBeatsEnhancedFillerSoulVoice = 40;
	internal const int GcdAoETargets = 2;
	internal const int OgcdAoETargets = 2;
	internal const int NormalAoeFreshDotOverrideTargets = 3;
	internal const float FreshDotHighHpMinimumTargetSeconds = 30f;

	private const float OpenerPotionSeconds = 0f;
	private const float TwoMinutePotionSeconds = 120f;
	private const float FiveMinutePotionSeconds = 300f;
	private const float SixMinutePotionSeconds = 360f;
	private const float EightMinutePotionSeconds = 480f;
	private const float TenMinutePotionSeconds = 600f;
	private const int OneGcdRemaining = 1;
	private const int TwoGcdsRemaining = 2;

	internal static BardAscendedSongDurations GetSongDurations(BardAscendedSongTiming timing, BardAscendedSongDurations customDurations)
	{
		return timing switch
		{
			BardAscendedSongTiming.Cycle369 => new BardAscendedSongDurations(
				Cycle369WanderersDuration,
				Cycle369MagesDuration,
				Cycle369ArmysDuration),
			BardAscendedSongTiming.Custom => customDurations,
			_ => new BardAscendedSongDurations(
				StandardWanderersDuration,
				StandardMagesDuration,
				StandardArmysDuration)
		};
	}

	internal static bool UsesStandardBurstPath(BardAscendedSongTiming timing)
	{
		return timing != BardAscendedSongTiming.Cycle369;
	}

	internal static bool ShouldWaitForRadiantFinaleBeforeBattleVoice(
		bool radiantFinaleEnoughLevel,
		bool radiantFinaleCanUse,
		bool hasRadiantFinale,
		bool wasRadiantFinaleLastAction)
	{
		return radiantFinaleEnoughLevel
			&& radiantFinaleCanUse
			&& !hasRadiantFinale
			&& !wasRadiantFinaleLastAction;
	}

	internal static bool ShouldApplyBothDots(float targetTimeToKill, bool isBossTarget, bool replacesEnhancedFiller)
	{
		var minimumTargetSeconds = replacesEnhancedFiller
			? InitialDotReplacingEnhancedFillerMinimumTargetSeconds
			: InitialDotMinimumTargetSeconds;

		return TargetMeetsThreshold(targetTimeToKill, isBossTarget, minimumTargetSeconds);
	}

	internal static bool ShouldRefreshIronJaws(float targetTimeToKill, bool isBossTarget, bool replacesEnhancedFiller)
	{
		var minimumTargetSeconds = replacesEnhancedFiller
			? IronJawsReplacingEnhancedFillerMinimumTargetSeconds
			: IronJawsMinimumTargetSeconds;

		return TargetMeetsThreshold(targetTimeToKill, isBossTarget, minimumTargetSeconds);
	}

	internal static bool ShouldApplyCausticOnly(float targetTimeToKill, bool isBossTarget)
	{
		return TargetMeetsThreshold(targetTimeToKill, isBossTarget, CausticOnlyMinimumTargetSeconds);
	}

	internal static bool ShouldApplyStormbiteOnly(float targetTimeToKill, bool isBossTarget)
	{
		return TargetMeetsThreshold(targetTimeToKill, isBossTarget, StormbiteOnlyMinimumTargetSeconds);
	}

	internal static bool ShouldSpendApex(BardAscendedApexDecisionInput input)
	{
		if (ShouldDumpApexBeforeFightEnds(
			input.SoulVoice,
			input.TargetSecondsRemaining,
			input.WeaponTotalSeconds,
			input.WouldUseEnhancedFiller,
			input.NoFutureBlastPossible))
		{
			return true;
		}

		if (input.WouldUseIronJaws)
		{
			return false;
		}

		if (input.IsInBurst)
		{
			return input.SoulVoice >= ApexBlastReadySoulVoice;
		}

		if (input.SongPhase != BardAscendedSongPhase.MagesBallad)
		{
			return false;
		}

		return input.SoulVoice >= SoulVoiceCap
			|| ShouldSpendApexInMageBalladWindow(input.SoulVoice, input.SongSecondsRemaining);
	}

	internal static bool ShouldUseBlastArrow(bool hasBlastReady, bool wouldUseDots, bool wouldUseIronJaws)
	{
		return hasBlastReady && !wouldUseDots && !wouldUseIronJaws;
	}

	internal static bool ShouldUseFiller(bool hasEnhancedFiller, bool hasResonantReady)
	{
		return !hasEnhancedFiller && !hasResonantReady;
	}

	internal static bool ShouldUseGcdAoE(int affectedTargets)
	{
		return affectedTargets >= GcdAoETargets;
	}

	internal static bool ShouldUseOgcdAoE(int affectedTargets)
	{
		return affectedTargets >= OgcdAoETargets;
	}

	internal static bool ShouldFreshDotYieldToNormalAoe(BardAscendedFreshDotAoeInput input)
	{
		if (!input.HasResolvedNormalAoeCandidate) return false;
		if (input.IsBossTarget) return false;
		if (float.IsNaN(input.TargetSecondsRemaining)) return false;
		if (TargetMeetsThreshold(
				input.TargetSecondsRemaining,
				isBossTarget: false,
				FreshDotHighHpMinimumTargetSeconds))
		{
			return false;
		}

		return input.NormalAoeAffectedTargets >= NormalAoeFreshDotOverrideTargets;
	}

	internal static bool CanRecoverBloodletterChargesAfterSpend(BardAscendedBloodletterRecoveryInput input)
	{
		if (input.CurrentCharges <= 0) return false;

		var chargesAfterSpend = Math.Max(input.CurrentCharges - 1, 0);
		var chargesNeeded = input.MaximumCharges - chargesAfterSpend;
		if (chargesNeeded <= 0) return true;
		if (input.RecoveryHorizon <= 0f) return false;

		var firstChargeRecoveryTime = input.IsCooldownTicking && input.CurrentCharges < input.MaximumCharges
			? Math.Max(0f, input.FirstChargeTimeRemaining)
			: input.OneChargeRecastTime;
		var fullRecoveryTime = firstChargeRecoveryTime + (chargesNeeded - 1) * input.OneChargeRecastTime;
		return fullRecoveryTime <= input.RecoveryHorizon;
	}

	internal static bool ShouldStartFirstCycle(
		bool isInCombat,
		bool hasCombatState,
		float currentCombatTime,
		float previousCombatTime)
	{
		return isInCombat && (!hasCombatState || currentCombatTime < previousCombatTime);
	}

	internal static bool ShouldUseDirtyStartRecovery(
		bool enablePlannedFightMode,
		bool isFirstCycle,
		BardAscendedSongPhase songPhase)
	{
		if (enablePlannedFightMode) return false;
		if (!isFirstCycle) return false;

		return songPhase is BardAscendedSongPhase.MagesBallad
			or BardAscendedSongPhase.ArmysPaeon;
	}

	internal static ReadOnlySpan<float> GetPotionTimings(BardAscendedPotionTiming timing, float[]? customTimings)
	{
		return timing switch
		{
			BardAscendedPotionTiming.Opener => [OpenerPotionSeconds],
			BardAscendedPotionTiming.TwoEight => [TwoMinutePotionSeconds, EightMinutePotionSeconds],
			BardAscendedPotionTiming.ZeroSix => [OpenerPotionSeconds, SixMinutePotionSeconds],
			BardAscendedPotionTiming.ZeroFiveTen => [OpenerPotionSeconds, FiveMinutePotionSeconds, TenMinutePotionSeconds],
			BardAscendedPotionTiming.Custom => GetCustomPotionTimings(customTimings),
			_ => []
		};
	}

	private static bool TargetMeetsThreshold(float targetTimeToKill, bool isBossTarget, float minimumTargetSeconds)
	{
		if (float.IsNaN(targetTimeToKill))
		{
			return isBossTarget;
		}

		return targetTimeToKill >= minimumTargetSeconds;
	}

	private static bool ShouldSpendApexInMageBalladWindow(byte soulVoice, float songSecondsRemaining)
	{
		return soulVoice >= ApexBlastReadySoulVoice
			&& songSecondsRemaining >= MageBalladApexEarliestSecondsRemaining
			&& songSecondsRemaining <= MageBalladApexLatestSecondsRemaining;
	}

	private static bool ShouldDumpApexBeforeFightEnds(
		byte soulVoice,
		float targetSecondsRemaining,
		float weaponTotalSeconds,
		bool wouldUseEnhancedFiller,
		bool noFutureBlastPossible)
	{
		if (!HasPlannedKillTime(targetSecondsRemaining, weaponTotalSeconds))
		{
			return false;
		}

		if (HasGcdsRemaining(targetSecondsRemaining, weaponTotalSeconds, OneGcdRemaining) && noFutureBlastPossible)
		{
			var minimumSoulVoice = wouldUseEnhancedFiller
				? ApexBeatsEnhancedFillerSoulVoice
				: ApexBeatsBurstShotSoulVoice;

			return soulVoice >= minimumSoulVoice;
		}

		return HasGcdsRemaining(targetSecondsRemaining, weaponTotalSeconds, TwoGcdsRemaining)
			&& soulVoice >= ApexBlastReadySoulVoice;
	}

	private static bool HasPlannedKillTime(float targetSecondsRemaining, float weaponTotalSeconds)
	{
		return float.IsFinite(targetSecondsRemaining)
			&& targetSecondsRemaining > 0f
			&& float.IsFinite(weaponTotalSeconds)
			&& weaponTotalSeconds > 0f;
	}

	private static bool HasGcdsRemaining(float targetSecondsRemaining, float weaponTotalSeconds, int gcdCount)
	{
		return targetSecondsRemaining <= weaponTotalSeconds * gcdCount;
	}

	private static ReadOnlySpan<float> GetCustomPotionTimings(float[]? customTimings)
	{
		if (customTimings is null || customTimings.Length == 0 || ContainsOnlyOpenerPotionTimings(customTimings))
		{
			return [];
		}

		var positiveTimingCount = CountPositivePotionTimings(customTimings);
		if (positiveTimingCount == customTimings.Length) return customTimings;

		var positiveTimings = new float[positiveTimingCount];
		var nextPositiveTimingIndex = 0;
		for (var index = 0; index < customTimings.Length; index++)
		{
			if (customTimings[index] <= OpenerPotionSeconds) continue;
			positiveTimings[nextPositiveTimingIndex] = customTimings[index];
			nextPositiveTimingIndex++;
		}

		return positiveTimings;
	}

	private static bool ContainsOnlyOpenerPotionTimings(ReadOnlySpan<float> timings)
	{
		for (var index = 0; index < timings.Length; index++)
		{
			if (timings[index] != OpenerPotionSeconds)
			{
				return false;
			}
		}

		return true;
	}

	private static int CountPositivePotionTimings(ReadOnlySpan<float> timings)
	{
		var count = 0;
		for (var index = 0; index < timings.Length; index++)
		{
			if (timings[index] > OpenerPotionSeconds)
			{
				count++;
			}
		}

		return count;
	}
}
