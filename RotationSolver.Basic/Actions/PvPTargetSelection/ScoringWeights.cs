namespace RotationSolver.Basic.Actions.PvPTargetSelection;

/// <summary>
/// Tunable weights for <see cref="PvPTargetScorer"/>. All factor weights are present;
/// the Casual and Ranked presets carry empirically-seeded starting values.
/// </summary>
public readonly record struct ScoringWeights(
	// Phase 1
	double RoleWeight,
	double FinishWeight,
	double MitigationPenaltyWeight,
	double DistancePenaltyWeight,
	double StickyBonus,
	// Phase 2
	double CarrierWeight,
	double LBWeight,
	// Phase 3
	double IsolationWeight,
	double ThreatWeight,
	// Ranked CC control pressure
	double MpPressureWeight,
	double ResiliencePenaltyWeight,
	double ObjectiveWeight)
{
	/// <summary>
	/// Ranked is the default because PvPSmart target selection is tuned around
	/// Crystalline Conflict decisions where mitigation, MP, and control windows matter.
	/// </summary>
	public static ScoringPreset DefaultPreset => ScoringPreset.Ranked;

	/// <summary>
	/// Default weight seed used by config initialization so reset configs and tests
	/// read from the same preset source.
	/// </summary>
	public static ScoringWeights DefaultWeights => ForPreset(DefaultPreset);

	/// <summary>
	/// Detect the persisted Casual default written before the Ranked CC control factors
	/// existed. Used only during config migration.
	/// </summary>
	public static bool IsLegacyCasualDefault(ScoringWeights weights)
	{
		return weights == new ScoringWeights(
			RoleWeight: 1.00,
			FinishWeight: 1.00,
			MitigationPenaltyWeight: 1.00,
			DistancePenaltyWeight: 0.10,
			StickyBonus: 0.05,
			CarrierWeight: 0.50,
			LBWeight: 1.00,
			IsolationWeight: 0.25,
			ThreatWeight: 0.40,
			MpPressureWeight: 0.0,
			ResiliencePenaltyWeight: 0.0,
			ObjectiveWeight: 0.0);
	}

	/// <summary>
	/// Move the pre-control default seed to the current default so users promoted from
	/// Casual to Ranked do not retain zero control-pressure weights underneath.
	/// </summary>
	public static ScoringWeights MigrateLegacyDefaultWeights(ScoringWeights weights)
	{
		return IsLegacyCasualDefault(weights) ? DefaultWeights : weights;
	}

	/// <summary>
	/// Migrate the version 12 PvP scoring preset and weight pair to the current Bard
	/// control-support defaults without requiring the full plugin config object.
	/// </summary>
	public static (ScoringPreset Preset, ScoringWeights Weights) MigrateLegacyConfig(
		ScoringPreset preset,
		ScoringWeights weights)
	{
		if (preset == ScoringPreset.Casual
			&& (IsLegacyCasualDefault(weights) || weights == DefaultWeights))
		{
			return (DefaultPreset, MigrateLegacyDefaultWeights(weights));
		}

		if (preset == ScoringPreset.Custom)
		{
			return (preset, MigrateLegacyCustomWeights(weights));
		}

		return (preset, weights);
	}

	/// <summary>
	/// Fill control-pressure weights that older Custom configs could not persist while
	/// preserving every weight the user could previously tune.
	/// </summary>
	public static ScoringWeights MigrateLegacyCustomWeights(ScoringWeights weights)
	{
		var casualSeed = ForPreset(ScoringPreset.Casual);
		return new ScoringWeights(
			RoleWeight: weights.RoleWeight,
			FinishWeight: weights.FinishWeight,
			MitigationPenaltyWeight: weights.MitigationPenaltyWeight,
			DistancePenaltyWeight: weights.DistancePenaltyWeight,
			StickyBonus: weights.StickyBonus,
			CarrierWeight: weights.CarrierWeight,
			LBWeight: weights.LBWeight,
			IsolationWeight: weights.IsolationWeight,
			ThreatWeight: weights.ThreatWeight,
			MpPressureWeight: weights.MpPressureWeight == 0.0 ? casualSeed.MpPressureWeight : weights.MpPressureWeight,
			ResiliencePenaltyWeight: weights.ResiliencePenaltyWeight == 0.0 ? casualSeed.ResiliencePenaltyWeight : weights.ResiliencePenaltyWeight,
			ObjectiveWeight: weights.ObjectiveWeight == 0.0 ? casualSeed.ObjectiveWeight : weights.ObjectiveWeight);
	}

	/// <summary>
	/// Look up the preset values. <see cref="ScoringPreset.Custom"/> returns the Casual seed;
	/// callers replace via the user's <c>PvPScoringWeights</c> config when in Custom mode.
	/// </summary>
	public static ScoringWeights ForPreset(ScoringPreset preset) => preset switch
	{
		ScoringPreset.Ranked => new ScoringWeights(
			RoleWeight: 0.60,
			FinishWeight: 1.50,
			MitigationPenaltyWeight: 2.00,
			DistancePenaltyWeight: 0.10,
			StickyBonus: 0.03,
			CarrierWeight: 2.00,
			LBWeight: 1.50,
			IsolationWeight: 0.40,
			ThreatWeight: 0.70,
			MpPressureWeight: 0.80,
			ResiliencePenaltyWeight: 0.90,
			ObjectiveWeight: 1.00),

		// Casual is also the seed for Custom: user config overlays this if preset is Custom.
		_ => new ScoringWeights(
			RoleWeight: 1.00,
			FinishWeight: 1.00,
			MitigationPenaltyWeight: 1.00,
			DistancePenaltyWeight: 0.10,
			StickyBonus: 0.05,
			CarrierWeight: 0.50,
			LBWeight: 1.00,
			IsolationWeight: 0.25,
			ThreatWeight: 0.40,
			MpPressureWeight: 0.40,
			ResiliencePenaltyWeight: 0.50,
			ObjectiveWeight: 0.50),
	};
}
