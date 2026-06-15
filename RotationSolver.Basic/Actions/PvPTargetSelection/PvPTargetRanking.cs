namespace RotationSolver.Basic.Actions.PvPTargetSelection;

/// <summary>
/// Orders PvP target snapshots by a supplied score so per-job policies share one
/// filter–score–sort–project pipeline instead of duplicating it: snapshots scoring
/// <see cref="double.NegativeInfinity"/> are dropped (job policies use it as their
/// hard-exclusion sentinel), the supplied comparison owns ordering and tiebreaks.
/// </summary>
public static class PvPTargetRanking
{
	/// <summary>
	/// Ranks snapshots best-first. The comparison receives (snapshot, score) pairs so
	/// policies keep their own tiebreak rules (or none) without re-scoring.
	/// </summary>
	public static List<TSnapshot> Rank<TSnapshot>(
		IReadOnlyList<TSnapshot> targets,
		Func<TSnapshot, double> score,
		Comparison<(TSnapshot Target, double Score)> comparison)
	{
		List<(TSnapshot Target, double Score)> scoredTargets = [];

		foreach (var target in targets)
		{
			var targetScore = score(target);
			if (double.IsNegativeInfinity(targetScore))
			{
				continue;
			}

			scoredTargets.Add((target, targetScore));
		}

		scoredTargets.Sort(comparison);

		List<TSnapshot> rankedTargets = [];
		foreach (var scoredTarget in scoredTargets)
		{
			rankedTargets.Add(scoredTarget.Target);
		}

		return rankedTargets;
	}

	/// <summary>
	/// Returns the best-ranked snapshot, or null when every snapshot is excluded or the
	/// input is empty.
	/// </summary>
	public static TSnapshot? SelectBest<TSnapshot>(
		IReadOnlyList<TSnapshot> targets,
		Func<TSnapshot, double> score,
		Comparison<(TSnapshot Target, double Score)> comparison)
		where TSnapshot : struct
	{
		var rankedTargets = Rank(targets, score, comparison);
		return rankedTargets.Count == 0 ? null : rankedTargets[0];
	}
}
