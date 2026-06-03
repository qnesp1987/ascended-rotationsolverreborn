namespace RotationSolver.Basic.Actions.PvPTargetSelection;

/// <summary>
/// Pure kill-gate for PvP damage actions. It exists so rotations can distinguish
/// impossible targets from reduced-damage targets that are still lethal.
/// </summary>
public static class PvPDamageGate
{
	private const double HeavyDamageReduction = 0.30;

	/// <summary>
	/// Evaluates whether a PvP damage action should be spent into the target's
	/// current defensive state.
	/// </summary>
	public static PvPBurstRecommendation Evaluate(PvPDamageGateInput input)
	{
		if (input.HasInvulnerability || double.IsPositiveInfinity(input.EffectiveHpRatio))
		{
			return PvPBurstRecommendation.Hold;
		}

		if (input.ExpectedDamageRatio > 0.0 && input.EffectiveHpRatio <= input.ExpectedDamageRatio)
		{
			return PvPBurstRecommendation.Secure;
		}

		if (input.Intent == PvPBurstIntent.Secure)
		{
			return PvPBurstRecommendation.Hold;
		}

		if (input.ActiveDamageReduction >= HeavyDamageReduction && !input.HasPrioritySignal)
		{
			return PvPBurstRecommendation.Hold;
		}

		return PvPBurstRecommendation.Spend;
	}
}

/// <summary>
/// Value-only snapshot for deciding whether a PvP damage action is worth spending.
/// Ratios are expressed relative to the target's maximum HP.
/// </summary>
public readonly record struct PvPDamageGateInput(
	PvPBurstIntent Intent,
	double EffectiveHpRatio,
	double ExpectedDamageRatio,
	double ActiveDamageReduction,
	bool HasInvulnerability,
	bool HasPrioritySignal);
