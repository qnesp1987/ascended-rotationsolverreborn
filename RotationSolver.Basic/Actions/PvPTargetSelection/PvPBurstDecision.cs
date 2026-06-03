namespace RotationSolver.Basic.Actions.PvPTargetSelection;

/// <summary>
/// Pure PvP burst conservation policy. It decides whether a burst action should be
/// held, spent for pressure, or spent to secure a kill.
/// </summary>
public static class PvPBurstDecision
{
	private const double SecureEffectiveHpRatio = 0.30;
	private const double PressureEffectiveHpRatio = 0.55;
	private const double HeavyDamageReduction = 0.30;
	private const double ValuableTargetScore = 1.50;
	private const double HighRoleContribution = 0.80;
	private const double PrioritySignalContribution = 0.10;

	/// <summary>
	/// Evaluate the current target and return the recommended burst action.
	/// </summary>
	public static PvPBurstRecommendation Evaluate(PvPBurstDecisionInput input)
	{
		if (input.Score.Invuln || double.IsPositiveInfinity(input.EffectiveHpRatio))
		{
			return PvPBurstRecommendation.Hold;
		}

		if (input.EffectiveHpRatio <= SecureEffectiveHpRatio)
		{
			return PvPBurstRecommendation.Secure;
		}

		if (input.Intent == PvPBurstIntent.Secure)
		{
			return PvPBurstRecommendation.Hold;
		}

		var hasPrioritySignal = HasPrioritySignal(input.Score);
		if (input.ActiveDamageReduction >= HeavyDamageReduction && !hasPrioritySignal)
		{
			return PvPBurstRecommendation.Hold;
		}

		if (hasPrioritySignal)
		{
			return PvPBurstRecommendation.Spend;
		}

		if (input.EffectiveHpRatio <= PressureEffectiveHpRatio && input.Score.Total >= ValuableTargetScore)
		{
			return PvPBurstRecommendation.Spend;
		}

		return PvPBurstRecommendation.Hold;
	}

	private static bool HasPrioritySignal(ScoreBreakdown score)
	{
		if (score.Carrier > PrioritySignalContribution) return true;
		if (score.LB > PrioritySignalContribution) return true;
		if (score.Threat > PrioritySignalContribution) return true;
		if (score.Objective > PrioritySignalContribution) return true;
		return score.Role >= HighRoleContribution && score.Total >= ValuableTargetScore;
	}
}
