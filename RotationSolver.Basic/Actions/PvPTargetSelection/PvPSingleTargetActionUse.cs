using RotationSolver.Basic.Actions;

namespace RotationSolver.Basic.Actions.PvPTargetSelection;

/// <summary>
/// Runs existing PvP action targeting against one chosen object id while restoring the action predicate afterward.
/// </summary>
public static class PvPSingleTargetActionUse
{
	/// <summary>
	/// Restricts <paramref name="action"/> to <paramref name="targetId"/> for one <see cref="IBaseAction.CanUse"/> check.
	/// </summary>
	public static bool TryUseOn(
		IBaseAction action,
		ulong targetId,
		PvPSingleTargetActionOptions options,
		out IAction? result)
	{
		result = null;
		ArgumentNullException.ThrowIfNull(action);

		if (targetId == 0)
		{
			return false;
		}

		var originalCanTarget = action.Setting.CanTarget;
		action.Setting.CanTarget = candidate =>
			originalCanTarget(candidate) && candidate.GameObjectId == targetId;

		try
		{
			return action.CanUse(
				out result,
				skipStatusProvideCheck: options.SkipStatusProvideCheck,
				skipStatusNeed: options.SkipStatusNeed,
				skipTargetStatusNeedCheck: options.SkipTargetStatusNeedCheck,
				skipComboCheck: options.SkipComboCheck,
				skipCastingCheck: options.SkipCastingCheck,
				usedUp: options.UsedUp,
				skipAoeCheck: options.SkipAoeCheck,
				skipTTKCheck: options.SkipTtkCheck,
				gcdCountForAbility: options.GcdCountForAbility,
				checkActionManager: options.CheckActionManager,
				targetOverride: options.TargetOverride);
		}
		finally
		{
			action.Setting.CanTarget = originalCanTarget;
		}
	}
}
