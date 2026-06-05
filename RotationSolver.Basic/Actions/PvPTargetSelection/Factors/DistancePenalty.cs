namespace RotationSolver.Basic.Actions.PvPTargetSelection.Factors;

/// <summary>
/// Linear penalty for distance beyond the player's effective range. Targets in range pay nothing.
/// Negative distances (shouldn't occur with <see cref="Helpers.ObjectHelper.DistanceToPlayer"/>
/// but defended against) are treated as zero.
/// </summary>
public static class DistancePenalty
{
	/// <summary>
	/// Return the linear out-of-range penalty: 0 when <paramref name="distance"/> is within
	/// <paramref name="effectiveRange"/>, otherwise the overshoot
	/// (<paramref name="distance"/> minus <paramref name="effectiveRange"/>).
	/// </summary>
	public static double Compute(float distance, float effectiveRange)
	{
		if (distance <= effectiveRange) return 0.0;
		return distance - effectiveRange;
	}
}
