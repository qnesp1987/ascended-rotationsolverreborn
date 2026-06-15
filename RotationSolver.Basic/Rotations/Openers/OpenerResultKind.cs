namespace RotationSolver.Basic.Rotations.Openers;

/// <summary>
/// Tells the consuming rotation how to react to an opener request so it never has to
/// inspect controller internals: act (<see cref="Continue"/>), advance without acting
/// (<see cref="Skip"/>), stand down this slot (<see cref="NoAction"/>), or leave the
/// opener (<see cref="Complete"/> when the script is exhausted, <see cref="Break"/>
/// when a required action is unusable).
/// </summary>
public enum OpenerResultKind
{
	NoAction,
	Skip,
	Continue,
	Complete,
	Break
}
