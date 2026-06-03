namespace RotationSolver.Basic.Actions.PvPTargetSelection;

/// <summary>
/// Describes how selective PvP burst conservation should be for a candidate action.
/// </summary>
public enum PvPBurstIntent
{
	/// <summary>
	/// Use the action for either a valuable pressure window or a kill-secure window.
	/// </summary>
	Burst,

	/// <summary>
	/// Use the action only when the selected target is already within secure-kill range.
	/// </summary>
	Secure,
}
