using RotationSolver.Basic.Rotations.Openers;

namespace RotationSolver.Tests;

internal static partial class PvETestSuite
{
	private enum ToyOpenerAction
	{
		None,
		PrepullShot,
		GcdOne,
		GcdTwo,
		EarlyWeave,
		LateWeave,
		Substitute
	}

	private readonly record struct ToyOpenerContext(bool RequestInterjection, bool SuppressScripted);

	/// <summary>
	/// Minimal conforming script: GCDs at 1 and 2 with a gap afterwards (LastGcdIndex 5),
	/// weave window on step 1 (Early + Late), optional prepull ability with a 0f window.
	/// Interjects Substitute on request; withholds scripted abilities on request.
	/// </summary>
	private sealed class ToyOpenerScript(bool startsWithPrepullWeave) : IOpenerScript<ToyOpenerAction, ToyOpenerContext>
	{
		public int LastGcdIndex => 5;

		public bool StartsWithPrepullWeave => startsWithPrepullWeave;

		public bool TryGetGcdAction(int gcdIndex, out ToyOpenerAction action)
		{
			action = gcdIndex switch
			{
				1 => ToyOpenerAction.GcdOne,
				2 => ToyOpenerAction.GcdTwo,
				_ => ToyOpenerAction.None
			};
			return action != ToyOpenerAction.None;
		}

		public bool TryGetAbilityAction(int step, OpenerWeaveSlot slot, out ToyOpenerAction action)
		{
			action = (step, slot) switch
			{
				(0, OpenerWeaveSlot.Prepull) => ToyOpenerAction.PrepullShot,
				(1, OpenerWeaveSlot.Early) => ToyOpenerAction.EarlyWeave,
				(1, OpenerWeaveSlot.Late) => ToyOpenerAction.LateWeave,
				_ => ToyOpenerAction.None
			};
			return action != ToyOpenerAction.None;
		}

		public bool HasFirstWeaveSlot(int step)
		{
			return step == 1;
		}

		public float GetPrepullWindowSeconds(ToyOpenerAction action)
		{
			return action == ToyOpenerAction.PrepullShot
				? 0f
				: ScriptedOpenerController.CountdownPrepullActionWindowSeconds;
		}

		public bool TryInterject(in OpenerInput<ToyOpenerContext> input, ToyOpenerAction? scriptedAction, out ToyOpenerAction interjected)
		{
			interjected = ToyOpenerAction.Substitute;
			return scriptedAction != ToyOpenerAction.Substitute && input.Context.RequestInterjection;
		}

		public bool ShouldExecuteScripted(ToyOpenerAction scriptedAction, in OpenerInput<ToyOpenerContext> input)
		{
			return !input.Context.SuppressScripted;
		}
	}

	private static readonly ToyOpenerScript ToyScript = new(startsWithPrepullWeave: false);
	private static readonly ToyOpenerScript ToyPrepullScript = new(startsWithPrepullWeave: true);

	static void ScriptedOpenerTerminalStateAlwaysCompletes()
	{
		var terminal = OpenerState.Start(startsWithPrepullWeave: false).Complete();

		var result = ScriptedOpenerController.GetNextRequest(OpenerInput<ToyOpenerContext>.ForGcd(terminal), ToyScript);

		AssertEqual(OpenerResultKind.Complete, result.Kind, "terminal state should complete");
		AssertTrue(result.NextState.IsTerminal, "completed result should stay terminal");

		var terminalUnusable = ScriptedOpenerController.GetNextRequest(
			OpenerInput<ToyOpenerContext>.ForGcd(terminal, canUseRequestedAction: false), ToyScript);

		AssertEqual(OpenerResultKind.Complete, terminalUnusable.Kind, "terminal check should precede the usability check");
	}

	static void ScriptedOpenerUnusableActionBreaks()
	{
		var state = OpenerState.Start(startsWithPrepullWeave: false);

		var result = ScriptedOpenerController.GetNextRequest(
			OpenerInput<ToyOpenerContext>.ForGcd(state, canUseRequestedAction: false), ToyScript);

		AssertEqual(OpenerResultKind.Break, result.Kind, "unusable requested action should break");
		AssertTrue(result.NextState.IsTerminal, "break should mark the state terminal");
	}

	static void ScriptedOpenerGcdBreaksWhileWeavePending()
	{
		var state = new OpenerState(Step: 1, NextGcdIndex: 2, NextWeaveSlot: OpenerWeaveSlot.Early, IsTerminal: false);

		var result = ScriptedOpenerController.GetNextRequest(OpenerInput<ToyOpenerContext>.ForGcd(state), ToyScript);

		AssertEqual(OpenerResultKind.Break, result.Kind, "GCD request should break while a weave is pending");
		AssertTrue(result.NextState.IsTerminal, "pending-weave break should mark the state terminal");
	}

	static void ScriptedOpenerGcdCompletesPastLastIndexAndOnAbsentAction()
	{
		var pastEnd = new OpenerState(Step: 6, NextGcdIndex: 6, NextWeaveSlot: OpenerWeaveSlot.None, IsTerminal: false);
		var pastEndResult = ScriptedOpenerController.GetNextRequest(OpenerInput<ToyOpenerContext>.ForGcd(pastEnd), ToyScript);
		AssertEqual(OpenerResultKind.Complete, pastEndResult.Kind, "GCD past the last index should complete");

		var gap = new OpenerState(Step: 3, NextGcdIndex: 3, NextWeaveSlot: OpenerWeaveSlot.None, IsTerminal: false);
		var gapResult = ScriptedOpenerController.GetNextRequest(OpenerInput<ToyOpenerContext>.ForGcd(gap), ToyScript);
		AssertEqual(OpenerResultKind.Complete, gapResult.Kind, "absent scripted GCD should complete (terminal asymmetry)");
		AssertTrue(gapResult.NextState.IsTerminal, "absent scripted GCD should mark the state terminal");
	}

	static void ScriptedOpenerGcdAdvanceOpensWeaveWindow()
	{
		var state = OpenerState.Start(startsWithPrepullWeave: false);

		var result = ScriptedOpenerController.GetNextRequest(OpenerInput<ToyOpenerContext>.ForGcd(state), ToyScript);

		AssertEqual(OpenerResultKind.Continue, result.Kind, "scripted GCD should continue");
		AssertEqual(ToyOpenerAction.GcdOne, result.Action, "scripted GCD should request the table action");
		AssertEqual(1, result.NextState.Step, "weave-opening GCD should keep Step at the owning index");
		AssertEqual(2, result.NextState.NextGcdIndex, "GCD advance should increment the next index");
		AssertEqual(OpenerWeaveSlot.Early, result.NextState.NextWeaveSlot, "step 1 should open the Early weave slot");
	}

	static void ScriptedOpenerGcdAdvanceWithoutWeaveJumpsStep()
	{
		var state = new OpenerState(Step: 2, NextGcdIndex: 2, NextWeaveSlot: OpenerWeaveSlot.None, IsTerminal: false);

		var result = ScriptedOpenerController.GetNextRequest(OpenerInput<ToyOpenerContext>.ForGcd(state), ToyScript);

		AssertEqual(ToyOpenerAction.GcdTwo, result.Action, "step 2 should request its scripted GCD");
		AssertEqual(3, result.NextState.Step, "non-weave GCD should jump Step to the next index");
		AssertEqual(OpenerWeaveSlot.None, result.NextState.NextWeaveSlot, "step 2 should not open a weave slot");
	}

	static void ScriptedOpenerAbilityWithoutPendingSlotReturnsNoAction()
	{
		var state = new OpenerState(Step: 2, NextGcdIndex: 2, NextWeaveSlot: OpenerWeaveSlot.None, IsTerminal: false);

		var result = ScriptedOpenerController.GetNextRequest(
			OpenerInput<ToyOpenerContext>.ForAbility(state, default), ToyScript);

		AssertEqual(OpenerResultKind.NoAction, result.Kind, "ability request without a pending slot should be NoAction");
		AssertEqual(state, result.NextState, "NoAction should not change state");
	}

	static void ScriptedOpenerAbilityWalksEarlyThenLateThenClears()
	{
		var state = new OpenerState(Step: 1, NextGcdIndex: 2, NextWeaveSlot: OpenerWeaveSlot.Early, IsTerminal: false);

		var early = ScriptedOpenerController.GetNextRequest(OpenerInput<ToyOpenerContext>.ForAbility(state, default), ToyScript);
		AssertEqual(ToyOpenerAction.EarlyWeave, early.Action, "early slot should request the early ability");
		AssertEqual(OpenerWeaveSlot.Late, early.NextState.NextWeaveSlot, "early advance should move to the late slot when it has content");
		AssertEqual(1, early.NextState.Step, "ability advance should keep Step at the owning GCD");

		var late = ScriptedOpenerController.GetNextRequest(OpenerInput<ToyOpenerContext>.ForAbility(early.NextState, default), ToyScript);
		AssertEqual(ToyOpenerAction.LateWeave, late.Action, "late slot should request the late ability");
		AssertEqual(OpenerWeaveSlot.None, late.NextState.NextWeaveSlot, "late advance should clear the weave slot");
		AssertEqual(2, late.NextState.Step, "cleared weave should move Step to the next GCD index");
	}

	static void ScriptedOpenerInterjectionSubstitutesWithoutAdvancing()
	{
		var state = new OpenerState(Step: 1, NextGcdIndex: 2, NextWeaveSlot: OpenerWeaveSlot.Early, IsTerminal: false);

		var result = ScriptedOpenerController.GetNextRequest(
			OpenerInput<ToyOpenerContext>.ForAbility(state, new ToyOpenerContext(RequestInterjection: true, SuppressScripted: false)),
			ToyScript);

		AssertEqual(OpenerResultKind.Continue, result.Kind, "interjection should continue");
		AssertEqual(ToyOpenerAction.Substitute, result.Action, "interjection should substitute the script action");
		AssertEqual(state, result.NextState, "interjection should not advance opener state");
	}

	static void ScriptedOpenerInterjectionRunsWhenScriptedActionAbsent()
	{
		var state = new OpenerState(Step: 4, NextGcdIndex: 5, NextWeaveSlot: OpenerWeaveSlot.Early, IsTerminal: false);

		var interjected = ScriptedOpenerController.GetNextRequest(
			OpenerInput<ToyOpenerContext>.ForAbility(state, new ToyOpenerContext(RequestInterjection: true, SuppressScripted: false)),
			ToyScript);
		AssertEqual(OpenerResultKind.Continue, interjected.Kind, "interjection should fire even when the slot has no scripted action");
		AssertEqual(ToyOpenerAction.Substitute, interjected.Action, "absent-slot interjection should request the substitute");
		AssertEqual(state, interjected.NextState, "absent-slot interjection should not advance opener state");

		var absent = ScriptedOpenerController.GetNextRequest(
			OpenerInput<ToyOpenerContext>.ForAbility(state, default), ToyScript);
		AssertEqual(OpenerResultKind.NoAction, absent.Kind, "absent ability without interjection should be NoAction (terminal asymmetry)");
		AssertEqual(state, absent.NextState, "absent ability should not change state");
	}

	static void ScriptedOpenerSuppressedScriptedAbilitySkipsWithAdvancedState()
	{
		var state = new OpenerState(Step: 1, NextGcdIndex: 2, NextWeaveSlot: OpenerWeaveSlot.Early, IsTerminal: false);

		var result = ScriptedOpenerController.GetNextRequest(
			OpenerInput<ToyOpenerContext>.ForAbility(state, new ToyOpenerContext(RequestInterjection: false, SuppressScripted: true)),
			ToyScript);

		AssertEqual(OpenerResultKind.Skip, result.Kind, "withheld scripted ability should skip");
		AssertEqual(OpenerRequestKind.None, result.RequestKind, "skip should not request an action kind");
		AssertEqual(default(ToyOpenerAction), result.Action, "skip should carry the default action");
		AssertEqual(OpenerWeaveSlot.Late, result.NextState.NextWeaveSlot, "skip should carry the advanced weave slot");
		AssertFalse(result.NextState.IsTerminal, "skip should keep the opener active");
	}

	static void ScriptedOpenerPrepullReadinessUsesScriptWindow()
	{
		var start = OpenerState.Start(ToyPrepullScript.StartsWithPrepullWeave);
		AssertEqual(0, start.Step, "prepull start should begin at step zero");
		AssertEqual(OpenerWeaveSlot.Prepull, start.NextWeaveSlot, "prepull start should pend the prepull slot");

		var request = ScriptedOpenerController.GetNextRequest(
			OpenerInput<ToyOpenerContext>.ForAbility(start, default), ToyPrepullScript);
		AssertEqual(ToyOpenerAction.PrepullShot, request.Action, "prepull slot should request the prepull ability");
		AssertTrue(
			ScriptedOpenerController.HasPendingCountdownPrepullRequest(in request),
			"prepull continue should report a pending countdown request");

		AssertFalse(
			ScriptedOpenerController.IsCountdownPrepullRequestReady(ToyPrepullScript, in request, 0.5f),
			"zero-window prepull should not be ready before countdown zero");
		AssertTrue(
			ScriptedOpenerController.IsCountdownPrepullRequestReady(ToyPrepullScript, in request, 0f),
			"zero-window prepull should be ready at countdown zero");

		var nonPrepullStart = OpenerState.Start(startsWithPrepullWeave: false);
		AssertEqual(1, nonPrepullStart.Step, "non-prepull start should begin at the first GCD");
		AssertEqual(OpenerWeaveSlot.None, nonPrepullStart.NextWeaveSlot, "non-prepull start should pend no slot");
	}
}
