using RotationSolver.Basic.Rotations.Openers;

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

internal readonly record struct BardAscendedOpenerContext(
	int PitchPerfectStacks,
	bool WillGainPitchPerfectStackBeforeNextWeave,
	bool IsEmpyrealArrowNextScriptedAbility,
	bool WillBurstBuffEndBeforeNextGcd);

/// <summary>
/// Shared BRD opener content: the step layout common to all three timings plus the
/// Pitch Perfect safety/dump interjections, which are timing-independent.
/// </summary>
internal abstract class BardAscendedOpenerScript : IOpenerScript<BardAscendedOpenerAction, BardAscendedOpenerContext>
{
	protected const int PrepullStep = 0;
	protected const int StormbiteStep = 1;
	protected const int CausticBiteStep = 2;
	protected const int FirstFlexibleFillerStep = 3;
	protected const int SecondFlexibleFillerStep = 4;
	protected const int StandardBarrageTargetStep = 5;
	protected const int StandardRadiantEncoreStep = 6;
	protected const int ResonantArrowStep = 7;
	protected const int PostBurstFillerStep = 8;
	protected const int PreRefreshFillerStep = 9;
	protected const int IronJawsStep = 10;
	protected const int PitchPerfectDumpStep = 11;
	protected const int Cycle369RadiantEncoreStep = 5;
	protected const int Cycle369BarrageTargetStep = 6;
	private const int NoStacks = 0;

	public int LastGcdIndex => PitchPerfectDumpStep;

	public virtual bool StartsWithPrepullWeave => false;

	public abstract bool TryGetGcdAction(int gcdIndex, out BardAscendedOpenerAction action);

	public abstract bool TryGetAbilityAction(int step, OpenerWeaveSlot slot, out BardAscendedOpenerAction action);

	public abstract bool HasFirstWeaveSlot(int step);

	public virtual float GetPrepullWindowSeconds(BardAscendedOpenerAction action)
	{
		return ScriptedOpenerController.CountdownPrepullActionWindowSeconds;
	}

	public bool TryInterject(in OpenerInput<BardAscendedOpenerContext> input, BardAscendedOpenerAction? scriptedAction, out BardAscendedOpenerAction interjected)
	{
		interjected = BardAscendedOpenerAction.PitchPerfect;
		return scriptedAction != BardAscendedOpenerAction.PitchPerfect
			&& ShouldUsePitchPerfectSafety(input.Context);
	}

	public bool ShouldExecuteScripted(BardAscendedOpenerAction scriptedAction, in OpenerInput<BardAscendedOpenerContext> input)
	{
		return scriptedAction != BardAscendedOpenerAction.PitchPerfect
			|| ShouldUsePitchPerfectDump(input.Context);
	}

	private static bool ShouldUsePitchPerfectSafety(in BardAscendedOpenerContext context)
	{
		return context.PitchPerfectStacks switch
		{
			>= 3 => context.WillGainPitchPerfectStackBeforeNextWeave,
			2 => context.IsEmpyrealArrowNextScriptedAbility || context.WillGainPitchPerfectStackBeforeNextWeave,
			>= 1 => context.WillBurstBuffEndBeforeNextGcd,
			_ => false
		};
	}

	private static bool ShouldUsePitchPerfectDump(in BardAscendedOpenerContext context)
	{
		return context.PitchPerfectStacks > NoStacks && context.WillBurstBuffEndBeforeNextGcd;
	}
}

internal sealed class BardAscendedStandardOpenerScript : BardAscendedOpenerScript
{
	public override bool TryGetGcdAction(int gcdIndex, out BardAscendedOpenerAction action)
	{
		action = gcdIndex switch
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
		return action != BardAscendedOpenerAction.None;
	}

	public override bool TryGetAbilityAction(int step, OpenerWeaveSlot slot, out BardAscendedOpenerAction action)
	{
		action = (step, slot) switch
		{
			(StormbiteStep, OpenerWeaveSlot.Early) => BardAscendedOpenerAction.HeartbreakShot,
			(StormbiteStep, OpenerWeaveSlot.Late) => BardAscendedOpenerAction.TheWanderersMinuet,
			(CausticBiteStep, OpenerWeaveSlot.Early) => BardAscendedOpenerAction.EmpyrealArrow,
			(CausticBiteStep, OpenerWeaveSlot.Late) => BardAscendedOpenerAction.RadiantFinale,
			(FirstFlexibleFillerStep, OpenerWeaveSlot.Early) => BardAscendedOpenerAction.BattleVoice,
			(FirstFlexibleFillerStep, OpenerWeaveSlot.Late) => BardAscendedOpenerAction.RagingStrikes,
			(SecondFlexibleFillerStep, OpenerWeaveSlot.Early) => BardAscendedOpenerAction.Barrage,
			(StandardBarrageTargetStep, OpenerWeaveSlot.Early) => BardAscendedOpenerAction.Sidewinder,
			(PostBurstFillerStep, OpenerWeaveSlot.Early) => BardAscendedOpenerAction.EmpyrealArrow,
			(PitchPerfectDumpStep, OpenerWeaveSlot.Early) => BardAscendedOpenerAction.PitchPerfect,
			_ => BardAscendedOpenerAction.None
		};
		return action != BardAscendedOpenerAction.None;
	}

	public override bool HasFirstWeaveSlot(int step)
	{
		return step is StormbiteStep
			or CausticBiteStep
			or FirstFlexibleFillerStep
			or SecondFlexibleFillerStep
			or StandardBarrageTargetStep
			or PostBurstFillerStep
			or PitchPerfectDumpStep;
	}
}

internal sealed class BardAscendedAdjustedStandardOpenerScript : BardAscendedOpenerScript
{
	public override bool StartsWithPrepullWeave => true;

	public override float GetPrepullWindowSeconds(BardAscendedOpenerAction action)
	{
		return action == BardAscendedOpenerAction.HeartbreakShot
			? BardAscendedOpenerScripts.AdjustedStandardPrepullHeartbreakWindowSeconds
			: base.GetPrepullWindowSeconds(action);
	}

	public override bool TryGetGcdAction(int gcdIndex, out BardAscendedOpenerAction action)
	{
		// Adjusted standard shares the standard GCD layout; only abilities differ.
		action = gcdIndex switch
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
		return action != BardAscendedOpenerAction.None;
	}

	public override bool TryGetAbilityAction(int step, OpenerWeaveSlot slot, out BardAscendedOpenerAction action)
	{
		action = (step, slot) switch
		{
			(PrepullStep, OpenerWeaveSlot.Prepull) => BardAscendedOpenerAction.HeartbreakShot,
			(StormbiteStep, OpenerWeaveSlot.Early) => BardAscendedOpenerAction.TheWanderersMinuet,
			(StormbiteStep, OpenerWeaveSlot.Late) => BardAscendedOpenerAction.EmpyrealArrow,
			(CausticBiteStep, OpenerWeaveSlot.Early) => BardAscendedOpenerAction.Potion,
			(CausticBiteStep, OpenerWeaveSlot.Late) => BardAscendedOpenerAction.BattleVoice,
			(FirstFlexibleFillerStep, OpenerWeaveSlot.Early) => BardAscendedOpenerAction.RadiantFinale,
			(FirstFlexibleFillerStep, OpenerWeaveSlot.Late) => BardAscendedOpenerAction.RagingStrikes,
			(SecondFlexibleFillerStep, OpenerWeaveSlot.Early) => BardAscendedOpenerAction.Barrage,
			(StandardBarrageTargetStep, OpenerWeaveSlot.Early) => BardAscendedOpenerAction.Sidewinder,
			(PostBurstFillerStep, OpenerWeaveSlot.Early) => BardAscendedOpenerAction.EmpyrealArrow,
			(PitchPerfectDumpStep, OpenerWeaveSlot.Early) => BardAscendedOpenerAction.PitchPerfect,
			_ => BardAscendedOpenerAction.None
		};
		return action != BardAscendedOpenerAction.None;
	}

	public override bool HasFirstWeaveSlot(int step)
	{
		return step is StormbiteStep
			or CausticBiteStep
			or FirstFlexibleFillerStep
			or SecondFlexibleFillerStep
			or StandardBarrageTargetStep
			or PostBurstFillerStep
			or PitchPerfectDumpStep;
	}
}

internal sealed class BardAscendedCycle369OpenerScript : BardAscendedOpenerScript
{
	public override bool TryGetGcdAction(int gcdIndex, out BardAscendedOpenerAction action)
	{
		action = gcdIndex switch
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
		return action != BardAscendedOpenerAction.None;
	}

	public override bool TryGetAbilityAction(int step, OpenerWeaveSlot slot, out BardAscendedOpenerAction action)
	{
		action = (step, slot) switch
		{
			(StormbiteStep, OpenerWeaveSlot.Early) => BardAscendedOpenerAction.HeartbreakShot,
			(StormbiteStep, OpenerWeaveSlot.Late) => BardAscendedOpenerAction.TheWanderersMinuet,
			(CausticBiteStep, OpenerWeaveSlot.Early) => BardAscendedOpenerAction.Potion,
			(CausticBiteStep, OpenerWeaveSlot.Late) => BardAscendedOpenerAction.RadiantFinale,
			(FirstFlexibleFillerStep, OpenerWeaveSlot.Early) => BardAscendedOpenerAction.BattleVoice,
			(SecondFlexibleFillerStep, OpenerWeaveSlot.Early) => BardAscendedOpenerAction.RagingStrikes,
			(SecondFlexibleFillerStep, OpenerWeaveSlot.Late) => BardAscendedOpenerAction.EmpyrealArrow,
			(Cycle369RadiantEncoreStep, OpenerWeaveSlot.Early) => BardAscendedOpenerAction.Barrage,
			(Cycle369BarrageTargetStep, OpenerWeaveSlot.Early) => BardAscendedOpenerAction.Sidewinder,
			(IronJawsStep, OpenerWeaveSlot.Early) => BardAscendedOpenerAction.EmpyrealArrow,
			(PitchPerfectDumpStep, OpenerWeaveSlot.Early) => BardAscendedOpenerAction.PitchPerfect,
			_ => BardAscendedOpenerAction.None
		};
		return action != BardAscendedOpenerAction.None;
	}

	public override bool HasFirstWeaveSlot(int step)
	{
		return step is StormbiteStep
			or CausticBiteStep
			or FirstFlexibleFillerStep
			or SecondFlexibleFillerStep
			or Cycle369RadiantEncoreStep
			or Cycle369BarrageTargetStep
			or IronJawsStep
			or PitchPerfectDumpStep;
	}
}

/// <summary>
/// Selects the BRD opener script for a song timing. Total over the enum:
/// Custom follows the Standard tables, mirroring the pre-extraction default switch arms.
/// </summary>
internal static class BardAscendedOpenerScripts
{
	internal const float AdjustedStandardPrepullHeartbreakWindowSeconds = 0f;

	private static readonly BardAscendedStandardOpenerScript StandardScript = new();
	private static readonly BardAscendedAdjustedStandardOpenerScript AdjustedStandardScript = new();
	private static readonly BardAscendedCycle369OpenerScript Cycle369Script = new();

	internal static BardAscendedOpenerScript For(BardAscendedSongTiming timing)
	{
		return timing switch
		{
			BardAscendedSongTiming.AdjustedStandard => AdjustedStandardScript,
			BardAscendedSongTiming.Cycle369 => Cycle369Script,
			_ => StandardScript
		};
	}

	internal static OpenerState StartFor(BardAscendedSongTiming timing)
	{
		return OpenerState.Start(For(timing).StartsWithPrepullWeave);
	}
}
