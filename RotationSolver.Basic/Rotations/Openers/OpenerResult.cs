namespace RotationSolver.Basic.Rotations.Openers;

/// <summary>
/// The controller's answer to one opener request. <see cref="Action"/> is meaningful
/// only when <see cref="Kind"/> is <see cref="OpenerResultKind.Continue"/>; consumers
/// must branch on <see cref="Kind"/> first (every other kind carries
/// <see langword="default"/> as the action).
/// </summary>
public readonly record struct OpenerResult<TAction>(
	OpenerResultKind Kind,
	OpenerRequestKind RequestKind,
	TAction Action,
	OpenerWeaveSlot WeaveSlot,
	OpenerState NextState)
	where TAction : struct, Enum;
