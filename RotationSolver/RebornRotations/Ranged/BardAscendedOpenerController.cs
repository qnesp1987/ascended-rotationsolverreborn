namespace RotationSolver.RebornRotations.Ranged;

internal enum BardAscendedOpenerAction
{
	None,
	FlexibleFiller,
	Stormbite,
	CausticBite,
	RefulgentArrow,
	IronJaws,
	RadiantEncore,
	ResonantArrow,
	HeartbreakShot,
	TheWanderersMinuet,
	EmpyrealArrow,
	RadiantFinale,
	BattleVoice,
	RagingStrikes,
	Barrage,
	Sidewinder,
	PitchPerfect,
	Potion
}

internal enum BardAscendedOpenerRequestKind
{
	None,
	Gcd,
	Ability
}

internal enum BardAscendedOpenerResultKind
{
	NoAction,
	Skip,
	Continue,
	Complete,
	Break
}

internal enum BardAscendedWeaveSlot
{
	None,
	Prepull,
	Early,
	Late
}

internal readonly record struct BardAscendedOpenerState(
	BardAscendedSongTiming Timing,
	int Step,
	int NextGcdIndex,
	BardAscendedWeaveSlot NextWeaveSlot,
	bool IsTerminal)
{
	private const int PrepullStep = 0;
	private const int FirstGcdIndex = 1;

	internal static BardAscendedOpenerState Start(BardAscendedSongTiming timing)
	{
		return timing == BardAscendedSongTiming.AdjustedStandard
			? new BardAscendedOpenerState(timing, Step: PrepullStep, NextGcdIndex: FirstGcdIndex, NextWeaveSlot: BardAscendedWeaveSlot.Prepull, IsTerminal: false)
			: new BardAscendedOpenerState(timing, Step: FirstGcdIndex, NextGcdIndex: FirstGcdIndex, NextWeaveSlot: BardAscendedWeaveSlot.None, IsTerminal: false);
	}

	internal BardAscendedOpenerState Complete()
	{
		return this with { IsTerminal = true };
	}
}

internal readonly record struct BardAscendedOpenerInput(
	BardAscendedOpenerState State,
	BardAscendedOpenerRequestKind RequestKind,
	bool CanUseRequestedAction,
	int PitchPerfectStacks,
	bool WillGainPitchPerfectStackBeforeNextWeave,
	bool IsEmpyrealArrowNextScriptedAbility,
	bool WillBurstBuffEndBeforeNextGcd)
{
	internal static BardAscendedOpenerInput ForGcd(
		BardAscendedOpenerState state,
		bool canUseRequestedAction = true)
	{
		return new BardAscendedOpenerInput(
			state,
			BardAscendedOpenerRequestKind.Gcd,
			canUseRequestedAction,
			PitchPerfectStacks: 0,
			WillGainPitchPerfectStackBeforeNextWeave: false,
			IsEmpyrealArrowNextScriptedAbility: false,
			WillBurstBuffEndBeforeNextGcd: false);
	}

	internal static BardAscendedOpenerInput ForAbility(
		BardAscendedOpenerState state,
		bool canUseRequestedAction = true,
		int pitchPerfectStacks = 0,
		bool willGainPitchPerfectStackBeforeNextWeave = false,
		bool isEmpyrealArrowNextScriptedAbility = false,
		bool willBurstBuffEndBeforeNextGcd = false)
	{
		return new BardAscendedOpenerInput(
			state,
			BardAscendedOpenerRequestKind.Ability,
			canUseRequestedAction,
			pitchPerfectStacks,
			willGainPitchPerfectStackBeforeNextWeave,
			isEmpyrealArrowNextScriptedAbility,
			willBurstBuffEndBeforeNextGcd);
	}
}

internal readonly record struct BardAscendedOpenerResult(
	BardAscendedOpenerResultKind Kind,
	BardAscendedOpenerRequestKind RequestKind,
	BardAscendedOpenerAction Action,
	BardAscendedWeaveSlot WeaveSlot,
	BardAscendedOpenerState NextState);

internal static class BardAscendedOpenerController
{
	private const int PrepullStep = 0;
	private const int StormbiteStep = 1;
	private const int CausticBiteStep = 2;
	private const int FirstFlexibleFillerStep = 3;
	private const int SecondFlexibleFillerStep = 4;
	private const int StandardBarrageTargetStep = 5;
	private const int StandardRadiantEncoreStep = 6;
	private const int ResonantArrowStep = 7;
	private const int PostBurstFillerStep = 8;
	private const int PreRefreshFillerStep = 9;
	private const int IronJawsStep = 10;
	private const int PitchPerfectDumpStep = 11;
	private const int LastGcdIndex = PitchPerfectDumpStep;
	private const int Cycle369RadiantEncoreStep = 5;
	private const int Cycle369BarrageTargetStep = 6;
	private const int NoStacks = 0;
	private const float CountdownPrepullActionWindowSeconds = 1f;
	internal const float AdjustedStandardPrepullHeartbreakWindowSeconds = 0f;

	internal static BardAscendedOpenerResult GetNextRequest(BardAscendedOpenerInput input)
	{
		if (input.State.IsTerminal)
		{
			return Complete(input.State);
		}

		if (!input.CanUseRequestedAction)
		{
			return Break(input.State);
		}

		return input.RequestKind switch
		{
			BardAscendedOpenerRequestKind.Gcd => GetNextGcdRequest(input.State),
			BardAscendedOpenerRequestKind.Ability => GetNextAbilityRequest(input),
			_ => NoAction(input.State)
		};
	}

	internal static bool IsCountdownPrepullRequestReady(
		BardAscendedSongTiming timing,
		BardAscendedOpenerResult request,
		float remainTime)
	{
		if (!HasPendingCountdownPrepullRequest(request)) return false;

		return timing == BardAscendedSongTiming.AdjustedStandard
			   && request.Action == BardAscendedOpenerAction.HeartbreakShot
			? remainTime <= AdjustedStandardPrepullHeartbreakWindowSeconds
			: remainTime <= CountdownPrepullActionWindowSeconds;
	}

	internal static bool HasPendingCountdownPrepullRequest(BardAscendedOpenerResult request)
	{
		return request.Kind == BardAscendedOpenerResultKind.Continue
			   && request.RequestKind == BardAscendedOpenerRequestKind.Ability
			   && request.WeaveSlot == BardAscendedWeaveSlot.Prepull;
	}

	private static BardAscendedOpenerResult GetNextGcdRequest(BardAscendedOpenerState state)
	{
		if (state.NextWeaveSlot != BardAscendedWeaveSlot.None)
		{
			return Break(state);
		}

		if (state.NextGcdIndex > LastGcdIndex)
		{
			return Complete(state);
		}

		var action = GetGcdAction(state.Timing, state.NextGcdIndex);
		if (action == BardAscendedOpenerAction.None)
		{
			return Complete(state);
		}

		return Continue(
			BardAscendedOpenerRequestKind.Gcd,
			action,
			BardAscendedWeaveSlot.None,
			AdvanceAfterGcd(state));
	}

	private static BardAscendedOpenerResult GetNextAbilityRequest(BardAscendedOpenerInput input)
	{
		if (input.State.NextWeaveSlot == BardAscendedWeaveSlot.None)
		{
			return NoAction(input.State);
		}

		var action = GetAbilityAction(input.State.Timing, input.State.Step, input.State.NextWeaveSlot);

		if (action != BardAscendedOpenerAction.PitchPerfect && ShouldUsePitchPerfectSafety(input))
		{
			return Continue(
				BardAscendedOpenerRequestKind.Ability,
				BardAscendedOpenerAction.PitchPerfect,
				input.State.NextWeaveSlot,
				input.State);
		}

		if (action == BardAscendedOpenerAction.None)
		{
			return NoAction(input.State);
		}

		var nextState = AdvanceAfterAbility(input.State);
		if (action == BardAscendedOpenerAction.PitchPerfect && !ShouldUsePitchPerfectDump(input))
		{
			return new BardAscendedOpenerResult(
				BardAscendedOpenerResultKind.Skip,
				BardAscendedOpenerRequestKind.None,
				BardAscendedOpenerAction.None,
				BardAscendedWeaveSlot.None,
				nextState);
		}

		return Continue(
			BardAscendedOpenerRequestKind.Ability,
			action,
			input.State.NextWeaveSlot,
			nextState);
	}

	private static bool ShouldUsePitchPerfectSafety(BardAscendedOpenerInput input)
	{
		return input.PitchPerfectStacks switch
		{
			>= 3 => input.WillGainPitchPerfectStackBeforeNextWeave,
			2 => input.IsEmpyrealArrowNextScriptedAbility || input.WillGainPitchPerfectStackBeforeNextWeave,
			>= 1 => input.WillBurstBuffEndBeforeNextGcd,
			_ => false
		};
	}

	private static bool ShouldUsePitchPerfectDump(BardAscendedOpenerInput input)
	{
		return input.PitchPerfectStacks > NoStacks && input.WillBurstBuffEndBeforeNextGcd;
	}

	private static BardAscendedOpenerState AdvanceAfterGcd(BardAscendedOpenerState state)
	{
		var weaveSlot = GetFirstWeaveSlot(state.Timing, state.NextGcdIndex);
		var nextGcdIndex = state.NextGcdIndex + 1;
		if (weaveSlot == BardAscendedWeaveSlot.None)
		{
			return state with
			{
				Step = nextGcdIndex,
				NextGcdIndex = nextGcdIndex
			};
		}

		return state with
		{
			Step = state.NextGcdIndex,
			NextGcdIndex = nextGcdIndex,
			NextWeaveSlot = weaveSlot
		};
	}

	private static BardAscendedOpenerState AdvanceAfterAbility(BardAscendedOpenerState state)
	{
		if (HasAbilityAction(state.Timing, state.Step, GetNextWeaveSlot(state.NextWeaveSlot)))
		{
			return state with { NextWeaveSlot = GetNextWeaveSlot(state.NextWeaveSlot) };
		}

		return state with
		{
			Step = state.NextGcdIndex,
			NextWeaveSlot = BardAscendedWeaveSlot.None
		};
	}

	private static BardAscendedWeaveSlot GetNextWeaveSlot(BardAscendedWeaveSlot currentSlot)
	{
		return currentSlot switch
		{
			BardAscendedWeaveSlot.Prepull => BardAscendedWeaveSlot.None,
			BardAscendedWeaveSlot.Early => BardAscendedWeaveSlot.Late,
			_ => BardAscendedWeaveSlot.None
		};
	}

	private static BardAscendedWeaveSlot GetFirstWeaveSlot(BardAscendedSongTiming timing, int step)
	{
		return HasFirstWeaveSlot(timing, step)
			? BardAscendedWeaveSlot.Early
			: BardAscendedWeaveSlot.None;
	}

	private static bool HasFirstWeaveSlot(BardAscendedSongTiming timing, int step)
	{
		return timing switch
		{
			BardAscendedSongTiming.Cycle369 => step is StormbiteStep
				or CausticBiteStep
				or FirstFlexibleFillerStep
				or SecondFlexibleFillerStep
				or Cycle369RadiantEncoreStep
				or Cycle369BarrageTargetStep
				or IronJawsStep
				or PitchPerfectDumpStep,
			_ => step is StormbiteStep
				or CausticBiteStep
				or FirstFlexibleFillerStep
				or SecondFlexibleFillerStep
				or StandardBarrageTargetStep
				or PostBurstFillerStep
				or PitchPerfectDumpStep
		};
	}

	private static BardAscendedOpenerAction GetGcdAction(BardAscendedSongTiming timing, int gcdIndex)
	{
		return timing == BardAscendedSongTiming.Cycle369
			? GetCycle369GcdAction(gcdIndex)
			: GetStandardGcdAction(gcdIndex);
	}

	private static BardAscendedOpenerAction GetStandardGcdAction(int gcdIndex)
	{
		return gcdIndex switch
		{
			StormbiteStep => BardAscendedOpenerAction.Stormbite,
			CausticBiteStep => BardAscendedOpenerAction.CausticBite,
			FirstFlexibleFillerStep
				or SecondFlexibleFillerStep
				or PostBurstFillerStep
				or PreRefreshFillerStep
				or PitchPerfectDumpStep => BardAscendedOpenerAction.FlexibleFiller,
			StandardBarrageTargetStep => BardAscendedOpenerAction.RefulgentArrow,
			StandardRadiantEncoreStep => BardAscendedOpenerAction.RadiantEncore,
			ResonantArrowStep => BardAscendedOpenerAction.ResonantArrow,
			IronJawsStep => BardAscendedOpenerAction.IronJaws,
			_ => BardAscendedOpenerAction.None
		};
	}

	private static BardAscendedOpenerAction GetCycle369GcdAction(int gcdIndex)
	{
		return gcdIndex switch
		{
			StormbiteStep => BardAscendedOpenerAction.Stormbite,
			CausticBiteStep => BardAscendedOpenerAction.CausticBite,
			FirstFlexibleFillerStep
				or SecondFlexibleFillerStep
				or PostBurstFillerStep
				or PreRefreshFillerStep
				or PitchPerfectDumpStep => BardAscendedOpenerAction.FlexibleFiller,
			Cycle369RadiantEncoreStep => BardAscendedOpenerAction.RadiantEncore,
			Cycle369BarrageTargetStep => BardAscendedOpenerAction.RefulgentArrow,
			ResonantArrowStep => BardAscendedOpenerAction.ResonantArrow,
			IronJawsStep => BardAscendedOpenerAction.IronJaws,
			_ => BardAscendedOpenerAction.None
		};
	}

	private static bool HasAbilityAction(BardAscendedSongTiming timing, int step, BardAscendedWeaveSlot slot)
	{
		return GetAbilityAction(timing, step, slot) != BardAscendedOpenerAction.None;
	}

	private static BardAscendedOpenerAction GetAbilityAction(BardAscendedSongTiming timing, int step, BardAscendedWeaveSlot slot)
	{
		return timing switch
		{
			BardAscendedSongTiming.AdjustedStandard => GetAdjustedStandardAbilityAction(step, slot),
			BardAscendedSongTiming.Cycle369 => GetCycle369AbilityAction(step, slot),
			_ => GetStandardAbilityAction(step, slot)
		};
	}

	private static BardAscendedOpenerAction GetStandardAbilityAction(int step, BardAscendedWeaveSlot slot)
	{
		return (step, slot) switch
		{
			(StormbiteStep, BardAscendedWeaveSlot.Early) => BardAscendedOpenerAction.HeartbreakShot,
			(StormbiteStep, BardAscendedWeaveSlot.Late) => BardAscendedOpenerAction.TheWanderersMinuet,
			(CausticBiteStep, BardAscendedWeaveSlot.Early) => BardAscendedOpenerAction.EmpyrealArrow,
			(CausticBiteStep, BardAscendedWeaveSlot.Late) => BardAscendedOpenerAction.RadiantFinale,
			(FirstFlexibleFillerStep, BardAscendedWeaveSlot.Early) => BardAscendedOpenerAction.BattleVoice,
			(FirstFlexibleFillerStep, BardAscendedWeaveSlot.Late) => BardAscendedOpenerAction.RagingStrikes,
			(SecondFlexibleFillerStep, BardAscendedWeaveSlot.Early) => BardAscendedOpenerAction.Barrage,
			(StandardBarrageTargetStep, BardAscendedWeaveSlot.Early) => BardAscendedOpenerAction.Sidewinder,
			(PostBurstFillerStep, BardAscendedWeaveSlot.Early) => BardAscendedOpenerAction.EmpyrealArrow,
			(PitchPerfectDumpStep, BardAscendedWeaveSlot.Early) => BardAscendedOpenerAction.PitchPerfect,
			_ => BardAscendedOpenerAction.None
		};
	}

	private static BardAscendedOpenerAction GetAdjustedStandardAbilityAction(int step, BardAscendedWeaveSlot slot)
	{
		return (step, slot) switch
		{
			(PrepullStep, BardAscendedWeaveSlot.Prepull) => BardAscendedOpenerAction.HeartbreakShot,
			(StormbiteStep, BardAscendedWeaveSlot.Early) => BardAscendedOpenerAction.TheWanderersMinuet,
			(StormbiteStep, BardAscendedWeaveSlot.Late) => BardAscendedOpenerAction.EmpyrealArrow,
			(CausticBiteStep, BardAscendedWeaveSlot.Early) => BardAscendedOpenerAction.Potion,
			(CausticBiteStep, BardAscendedWeaveSlot.Late) => BardAscendedOpenerAction.BattleVoice,
			(FirstFlexibleFillerStep, BardAscendedWeaveSlot.Early) => BardAscendedOpenerAction.RadiantFinale,
			(FirstFlexibleFillerStep, BardAscendedWeaveSlot.Late) => BardAscendedOpenerAction.RagingStrikes,
			(SecondFlexibleFillerStep, BardAscendedWeaveSlot.Early) => BardAscendedOpenerAction.Barrage,
			(StandardBarrageTargetStep, BardAscendedWeaveSlot.Early) => BardAscendedOpenerAction.Sidewinder,
			(PostBurstFillerStep, BardAscendedWeaveSlot.Early) => BardAscendedOpenerAction.EmpyrealArrow,
			(PitchPerfectDumpStep, BardAscendedWeaveSlot.Early) => BardAscendedOpenerAction.PitchPerfect,
			_ => BardAscendedOpenerAction.None
		};
	}

	private static BardAscendedOpenerAction GetCycle369AbilityAction(int step, BardAscendedWeaveSlot slot)
	{
		return (step, slot) switch
		{
			(StormbiteStep, BardAscendedWeaveSlot.Early) => BardAscendedOpenerAction.HeartbreakShot,
			(StormbiteStep, BardAscendedWeaveSlot.Late) => BardAscendedOpenerAction.TheWanderersMinuet,
			(CausticBiteStep, BardAscendedWeaveSlot.Early) => BardAscendedOpenerAction.Potion,
			(CausticBiteStep, BardAscendedWeaveSlot.Late) => BardAscendedOpenerAction.RadiantFinale,
			(FirstFlexibleFillerStep, BardAscendedWeaveSlot.Early) => BardAscendedOpenerAction.BattleVoice,
			(SecondFlexibleFillerStep, BardAscendedWeaveSlot.Early) => BardAscendedOpenerAction.RagingStrikes,
			(SecondFlexibleFillerStep, BardAscendedWeaveSlot.Late) => BardAscendedOpenerAction.EmpyrealArrow,
			(Cycle369RadiantEncoreStep, BardAscendedWeaveSlot.Early) => BardAscendedOpenerAction.Barrage,
			(Cycle369BarrageTargetStep, BardAscendedWeaveSlot.Early) => BardAscendedOpenerAction.Sidewinder,
			(IronJawsStep, BardAscendedWeaveSlot.Early) => BardAscendedOpenerAction.EmpyrealArrow,
			(PitchPerfectDumpStep, BardAscendedWeaveSlot.Early) => BardAscendedOpenerAction.PitchPerfect,
			_ => BardAscendedOpenerAction.None
		};
	}

	private static BardAscendedOpenerResult NoAction(BardAscendedOpenerState state)
	{
		return new BardAscendedOpenerResult(
			BardAscendedOpenerResultKind.NoAction,
			BardAscendedOpenerRequestKind.None,
			BardAscendedOpenerAction.None,
			BardAscendedWeaveSlot.None,
			state);
	}

	private static BardAscendedOpenerResult Continue(
		BardAscendedOpenerRequestKind requestKind,
		BardAscendedOpenerAction action,
		BardAscendedWeaveSlot weaveSlot,
		BardAscendedOpenerState nextState)
	{
		return new BardAscendedOpenerResult(
			BardAscendedOpenerResultKind.Continue,
			requestKind,
			action,
			weaveSlot,
			nextState);
	}

	private static BardAscendedOpenerResult Complete(BardAscendedOpenerState state)
	{
		return new BardAscendedOpenerResult(
			BardAscendedOpenerResultKind.Complete,
			BardAscendedOpenerRequestKind.None,
			BardAscendedOpenerAction.None,
			BardAscendedWeaveSlot.None,
			state.Complete());
	}

	private static BardAscendedOpenerResult Break(BardAscendedOpenerState state)
	{
		return new BardAscendedOpenerResult(
			BardAscendedOpenerResultKind.Break,
			BardAscendedOpenerRequestKind.None,
			BardAscendedOpenerAction.None,
			BardAscendedWeaveSlot.None,
			state.Complete());
	}
}
