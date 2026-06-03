using RotationSolver.Basic.Actions;

namespace RotationSolver.Basic.Actions.PvPTargetSelection;

/// <summary>
/// Options for exact target PvP action checks. Defaults mirror <see cref="IBaseAction.CanUse"/>.
/// </summary>
/// <param name="SkipStatusProvideCheck">Preserves the caller's status provide policy for the exact target check.</param>
/// <param name="SkipStatusNeed">Preserves the caller's self status requirement policy for the exact target check.</param>
/// <param name="SkipTargetStatusNeedCheck">Preserves the caller's target status requirement policy for the exact target check.</param>
/// <param name="SkipComboCheck">Preserves the caller's combo validation policy for the exact target check.</param>
/// <param name="SkipCastingCheck">Preserves the caller's casting validation policy for the exact target check.</param>
/// <param name="UsedUp">Preserves the caller's stack spending policy for the exact target check.</param>
/// <param name="SkipAoeCheck">Preserves the caller's area target count policy for the exact target check.</param>
/// <param name="SkipTtkCheck">Preserves the caller's time to kill policy for the exact target check.</param>
/// <param name="GcdCountForAbility">Preserves the caller's GCD timing context for ability checks.</param>
/// <param name="CheckActionManager">Preserves whether the caller requires the game action manager check.</param>
/// <param name="TargetOverride">Preserves the caller's target selection override while the exact target predicate is active.</param>
public readonly record struct PvPSingleTargetActionOptions(
	bool SkipStatusProvideCheck = false,
	bool SkipStatusNeed = false,
	bool SkipTargetStatusNeedCheck = false,
	bool SkipComboCheck = false,
	bool SkipCastingCheck = false,
	bool UsedUp = false,
	bool SkipAoeCheck = false,
	bool SkipTtkCheck = false,
	byte GcdCountForAbility = 0,
	bool CheckActionManager = false,
	TargetType TargetOverride = default);
