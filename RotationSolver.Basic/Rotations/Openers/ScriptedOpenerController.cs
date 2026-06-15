namespace RotationSolver.Basic.Rotations.Openers;

/// <summary>
/// Drives scripted opener sequencing over an <see cref="IOpenerScript{TAction, TContext}"/>
/// so every job opener shares one state machine: GCD/weave progression, terminal
/// handling, interjection ordering, and countdown prepull readiness. Owns no job
/// knowledge — scripts supply content and decisions.
/// </summary>
public static class ScriptedOpenerController
{
	/// <summary>
	/// Default countdown window for pre-pull opener actions; scripts override per action
	/// via <see cref="IOpenerScript{TAction, TContext}.GetPrepullWindowSeconds"/>.
	/// </summary>
	public const float CountdownPrepullActionWindowSeconds = 1f;

	/// <summary>
	/// Resolves the next opener request. Terminal states complete; unusable requested
	/// actions break; otherwise the request kind selects the GCD or ability path.
	/// </summary>
	public static OpenerResult<TAction> GetNextRequest<TAction, TContext>(
		in OpenerInput<TContext> input,
		IOpenerScript<TAction, TContext> script)
		where TAction : struct, Enum
		where TContext : struct
	{
		if (input.State.IsTerminal)
		{
			return Complete<TAction>(input.State);
		}

		if (!input.CanUseRequestedAction)
		{
			return Break<TAction>(input.State);
		}

		return input.RequestKind switch
		{
			OpenerRequestKind.Gcd => GetNextGcdRequest(input.State, script),
			OpenerRequestKind.Ability => GetNextAbilityRequest(in input, script),
			_ => NoAction<TAction>(input.State)
		};
	}

	/// <summary>
	/// Whether a pending countdown pre-pull ability may fire yet, using the script's
	/// per-action window.
	/// </summary>
	public static bool IsCountdownPrepullRequestReady<TAction, TContext>(
		IOpenerScript<TAction, TContext> script,
		in OpenerResult<TAction> request,
		float remainTime)
		where TAction : struct, Enum
		where TContext : struct
	{
		if (!HasPendingCountdownPrepullRequest(in request))
		{
			return false;
		}

		return remainTime <= script.GetPrepullWindowSeconds(request.Action);
	}

	/// <summary>
	/// Whether the result is an actionable pre-pull ability request, so rotations can
	/// hold their pull GCD while one is pending.
	/// </summary>
	public static bool HasPendingCountdownPrepullRequest<TAction>(in OpenerResult<TAction> request)
		where TAction : struct, Enum
	{
		return request.Kind == OpenerResultKind.Continue
			   && request.RequestKind == OpenerRequestKind.Ability
			   && request.WeaveSlot == OpenerWeaveSlot.Prepull;
	}

	private static OpenerResult<TAction> GetNextGcdRequest<TAction, TContext>(
		OpenerState state,
		IOpenerScript<TAction, TContext> script)
		where TAction : struct, Enum
		where TContext : struct
	{
		if (state.NextWeaveSlot != OpenerWeaveSlot.None)
		{
			return Break<TAction>(state);
		}

		if (state.NextGcdIndex > script.LastGcdIndex)
		{
			return Complete<TAction>(state);
		}

		if (!script.TryGetGcdAction(state.NextGcdIndex, out var action))
		{
			return Complete<TAction>(state);
		}

		return Continue(OpenerRequestKind.Gcd, action, OpenerWeaveSlot.None, AdvanceAfterGcd(state, script));
	}

	private static OpenerResult<TAction> GetNextAbilityRequest<TAction, TContext>(
		in OpenerInput<TContext> input,
		IOpenerScript<TAction, TContext> script)
		where TAction : struct, Enum
		where TContext : struct
	{
		if (input.State.NextWeaveSlot == OpenerWeaveSlot.None)
		{
			return NoAction<TAction>(input.State);
		}

		TAction? scriptedAction = script.TryGetAbilityAction(input.State.Step, input.State.NextWeaveSlot, out var resolved)
			? resolved
			: null;

		if (script.TryInterject(in input, scriptedAction, out var interjected))
		{
			return Continue(OpenerRequestKind.Ability, interjected, input.State.NextWeaveSlot, input.State);
		}

		if (scriptedAction is not { } action)
		{
			return NoAction<TAction>(input.State);
		}

		var nextState = AdvanceAfterAbility(input.State, script);
		if (!script.ShouldExecuteScripted(action, in input))
		{
			return Skip<TAction>(nextState);
		}

		return Continue(OpenerRequestKind.Ability, action, input.State.NextWeaveSlot, nextState);
	}

	private static OpenerState AdvanceAfterGcd<TAction, TContext>(
		OpenerState state,
		IOpenerScript<TAction, TContext> script)
		where TAction : struct, Enum
		where TContext : struct
	{
		var weaveSlot = script.HasFirstWeaveSlot(state.NextGcdIndex)
			? OpenerWeaveSlot.Early
			: OpenerWeaveSlot.None;
		var nextGcdIndex = state.NextGcdIndex + 1;
		if (weaveSlot == OpenerWeaveSlot.None)
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

	private static OpenerState AdvanceAfterAbility<TAction, TContext>(
		OpenerState state,
		IOpenerScript<TAction, TContext> script)
		where TAction : struct, Enum
		where TContext : struct
	{
		var nextSlot = GetNextWeaveSlot(state.NextWeaveSlot);
		if (script.TryGetAbilityAction(state.Step, nextSlot, out _))
		{
			return state with { NextWeaveSlot = nextSlot };
		}

		return state with
		{
			Step = state.NextGcdIndex,
			NextWeaveSlot = OpenerWeaveSlot.None
		};
	}

	private static OpenerWeaveSlot GetNextWeaveSlot(OpenerWeaveSlot currentSlot)
	{
		return currentSlot switch
		{
			OpenerWeaveSlot.Prepull => OpenerWeaveSlot.None,
			OpenerWeaveSlot.Early => OpenerWeaveSlot.Late,
			_ => OpenerWeaveSlot.None
		};
	}

	private static OpenerResult<TAction> NoAction<TAction>(OpenerState state)
		where TAction : struct, Enum
	{
		return new OpenerResult<TAction>(OpenerResultKind.NoAction, OpenerRequestKind.None, default, OpenerWeaveSlot.None, state);
	}

	private static OpenerResult<TAction> Continue<TAction>(
		OpenerRequestKind requestKind,
		TAction action,
		OpenerWeaveSlot weaveSlot,
		OpenerState nextState)
		where TAction : struct, Enum
	{
		return new OpenerResult<TAction>(OpenerResultKind.Continue, requestKind, action, weaveSlot, nextState);
	}

	private static OpenerResult<TAction> Complete<TAction>(OpenerState state)
		where TAction : struct, Enum
	{
		return new OpenerResult<TAction>(OpenerResultKind.Complete, OpenerRequestKind.None, default, OpenerWeaveSlot.None, state.Complete());
	}

	private static OpenerResult<TAction> Break<TAction>(OpenerState state)
		where TAction : struct, Enum
	{
		return new OpenerResult<TAction>(OpenerResultKind.Break, OpenerRequestKind.None, default, OpenerWeaveSlot.None, state.Complete());
	}

	/// <summary>
	/// Advances the opener over a withheld scripted ability: carries the already-advanced
	/// <paramref name="nextState"/> with no action, so callers skip this slot without leaving the opener.
	/// </summary>
	private static OpenerResult<TAction> Skip<TAction>(OpenerState nextState)
		where TAction : struct, Enum
	{
		return new OpenerResult<TAction>(OpenerResultKind.Skip, OpenerRequestKind.None, default, OpenerWeaveSlot.None, nextState);
	}
}
