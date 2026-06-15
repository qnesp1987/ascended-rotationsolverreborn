using RotationSolver.Basic.Actions.PvPTargetSelection;

namespace RotationSolver.Tests;

internal static partial class PvPTestSuite
{
	private readonly record struct RankProbe(int Id, double Score);

	private static double ProbeScore(RankProbe probe)
	{
		return probe.Score;
	}

	private static int CompareProbesByScoreThenId((RankProbe Target, double Score) left, (RankProbe Target, double Score) right)
	{
		var scoreComparison = right.Score.CompareTo(left.Score);
		return scoreComparison != 0
			? scoreComparison
			: left.Target.Id.CompareTo(right.Target.Id);
	}

	static void PvPTargetRankingFiltersNegativeInfinityScores()
	{
		var ranked = PvPTargetRanking.Rank(
			[new RankProbe(1, 2.0), new RankProbe(2, double.NegativeInfinity), new RankProbe(3, 1.0)],
			ProbeScore,
			CompareProbesByScoreThenId);

		AssertEqual(2, ranked.Count, "negative infinity scores should be dropped");
		AssertEqual(1, ranked[0].Id, "highest finite score should rank first");
		AssertEqual(3, ranked[1].Id, "lower finite score should rank second");
	}

	static void PvPTargetRankingOrdersBySuppliedComparison()
	{
		var ranked = PvPTargetRanking.Rank(
			[new RankProbe(1, 1.0), new RankProbe(2, 3.0), new RankProbe(3, 2.0)],
			ProbeScore,
			CompareProbesByScoreThenId);

		AssertEqual(2, ranked[0].Id, "comparison should order by score descending");
		AssertEqual(3, ranked[1].Id, "comparison should place the middle score second");
		AssertEqual(1, ranked[2].Id, "comparison should place the lowest score last");
	}

	static void PvPTargetRankingDelegatesTiebreaksToComparison()
	{
		var ranked = PvPTargetRanking.Rank(
			[new RankProbe(7, 2.0), new RankProbe(3, 2.0), new RankProbe(5, 2.0)],
			ProbeScore,
			CompareProbesByScoreThenId);

		AssertEqual(3, ranked[0].Id, "equal scores should fall back to the supplied tiebreak");
		AssertEqual(5, ranked[1].Id, "tiebreak should order by id ascending");
		AssertEqual(7, ranked[2].Id, "tiebreak should place the highest id last");
	}

	static void PvPTargetRankingSelectBestReturnsFirstRankedOrNull()
	{
		var best = PvPTargetRanking.SelectBest(
			[new RankProbe(1, 1.0), new RankProbe(2, 3.0)],
			ProbeScore,
			CompareProbesByScoreThenId);

		AssertEqual(2, best?.Id, "select best should return the first ranked snapshot");

		var none = PvPTargetRanking.SelectBest(
			[new RankProbe(1, double.NegativeInfinity)],
			ProbeScore,
			CompareProbesByScoreThenId);

		AssertTrue(none is null, "select best should return null when no snapshot has a finite score");

		var empty = PvPTargetRanking.SelectBest<RankProbe>([], ProbeScore, CompareProbesByScoreThenId);

		AssertTrue(empty is null, "select best should return null for empty input");
	}
}
