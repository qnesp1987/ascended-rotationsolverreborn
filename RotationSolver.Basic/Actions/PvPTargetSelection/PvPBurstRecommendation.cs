namespace RotationSolver.Basic.Actions.PvPTargetSelection;

/// <summary>
/// Result of evaluating whether a PvP burst action should be spent now.
/// </summary>
public enum PvPBurstRecommendation
{
	/// <summary>
	/// Conserve the burst action for a better target or a clearer kill window.
	/// </summary>
	Hold,

	/// <summary>
	/// Spend the burst action because the target is valuable enough to pressure.
	/// </summary>
	Spend,

	/// <summary>
	/// Spend the burst action because the target is within effective kill range.
	/// </summary>
	Secure,
}
