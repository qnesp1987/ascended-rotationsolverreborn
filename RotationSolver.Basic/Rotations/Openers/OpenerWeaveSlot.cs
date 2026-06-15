namespace RotationSolver.Basic.Rotations.Openers;

/// <summary>
/// Identifies which weave window an opener ability occupies, because scripts map
/// abilities by (step, slot) and the controller owns the Prepull/Early/Late progression.
/// </summary>
public enum OpenerWeaveSlot
{
	None,
	Prepull,
	Early,
	Late
}
