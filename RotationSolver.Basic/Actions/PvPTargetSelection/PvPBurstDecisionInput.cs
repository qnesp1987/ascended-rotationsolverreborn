namespace RotationSolver.Basic.Actions.PvPTargetSelection;

/// <summary>
/// Pure input snapshot for PvP burst conservation decisions.
/// </summary>
public readonly record struct PvPBurstDecisionInput(
	PvPBurstIntent Intent,
	double EffectiveHpRatio,
	double ActiveDamageReduction,
	ScoreBreakdown Score);
