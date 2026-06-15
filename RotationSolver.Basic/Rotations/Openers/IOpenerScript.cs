namespace RotationSolver.Basic.Rotations.Openers;

/// <summary>
/// One opener variant's content and in-opener decision hooks. The controller owns
/// sequencing; implementations own which action belongs at each (step, slot), which
/// steps open weave windows, per-action countdown windows, and when to substitute or
/// withhold a scripted ability. Implementations must not match
/// <see cref="OpenerWeaveSlot.None"/> in <see cref="TryGetAbilityAction"/>.
/// </summary>
public interface IOpenerScript<TAction, TContext>
	where TAction : struct, Enum
	where TContext : struct
{
	/// <summary>Index of the last scripted GCD; later indexes complete the opener.</summary>
	int LastGcdIndex { get; }

	/// <summary>Whether this opener schedules a pre-pull ability before the first GCD.</summary>
	bool StartsWithPrepullWeave { get; }

	/// <summary>Resolves the scripted GCD for an index; false when the script has none.</summary>
	bool TryGetGcdAction(int gcdIndex, out TAction action);

	/// <summary>Resolves the scripted ability for a (step, slot); false when the script has none.</summary>
	bool TryGetAbilityAction(int step, OpenerWeaveSlot slot, out TAction action);

	/// <summary>Whether the GCD at this step opens a weave window.</summary>
	bool HasFirstWeaveSlot(int step);

	/// <summary>
	/// Countdown window for a pre-pull action; return
	/// <see cref="ScriptedOpenerController.CountdownPrepullActionWindowSeconds"/> unless
	/// the action must land at a different point before the pull.
	/// </summary>
	float GetPrepullWindowSeconds(TAction action);

	/// <summary>
	/// Substitution hook, consulted before the absent-check so a safety action can fire
	/// even when the slot has no scripted entry. Returning true emits
	/// <paramref name="interjected"/> WITHOUT advancing the opener.
	/// </summary>
	bool TryInterject(in OpenerInput<TContext> input, TAction? scriptedAction, out TAction interjected);

	/// <summary>
	/// Withhold hook, consulted after the advanced state is computed. Returning false
	/// converts the scripted ability into <see cref="OpenerResultKind.Skip"/>.
	/// </summary>
	bool ShouldExecuteScripted(TAction scriptedAction, in OpenerInput<TContext> input);
}
