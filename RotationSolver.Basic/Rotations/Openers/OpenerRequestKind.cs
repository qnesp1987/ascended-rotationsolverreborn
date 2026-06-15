namespace RotationSolver.Basic.Rotations.Openers;

/// <summary>
/// Distinguishes whether an opener request is for the next GCD or the next weave ability,
/// because the controller advances state differently for each.
/// </summary>
public enum OpenerRequestKind
{
	None,
	Gcd,
	Ability
}
