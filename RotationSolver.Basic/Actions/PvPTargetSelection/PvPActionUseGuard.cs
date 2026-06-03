namespace RotationSolver.Basic.Actions.PvPTargetSelection;

/// <summary>
/// Value-only snapshot for the final PvP Guard check before an action is sent.
/// </summary>
public readonly record struct PvPActionUseGuardInput(
	bool IsPvP,
	bool IsHostileAction,
	bool IgnoresGuard,
	bool TargetHasGuard,
	bool GuardWillExpireBeforeAction);

/// <summary>
/// Decides whether a PvP action must be blocked because the selected target gained Guard.
/// </summary>
public static class PvPActionUseGuard
{
	/// <summary>
	/// Returns <see langword="true"/> when final action use should be blocked by Guard.
	/// </summary>
	public static bool ShouldBlock(PvPActionUseGuardInput input)
	{
		return input.IsPvP
			&& input.IsHostileAction
			&& input.TargetHasGuard
			&& !input.IgnoresGuard
			&& !input.GuardWillExpireBeforeAction;
	}
}
