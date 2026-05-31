using RotationSolver.RebornRotations.PVPRotations.Melee;

namespace RotationSolver.Tests;

internal static partial class PvPTestSuite
{
	static NinjaPvPLimitBreakTargetSnapshot NinjaLimitBreakTarget(
		ulong targetId,
		float healthRatio,
		float distanceToPlayer = 0f,
		bool hasRespectedInvulnerability = false)
	{
		return new NinjaPvPLimitBreakTargetSnapshot(
			TargetId: targetId,
			HealthRatio: healthRatio,
			DistanceToPlayer: distanceToPlayer,
			HasRespectedInvulnerability: hasRespectedInvulnerability);
	}

	static void NinjaSeitonTenchuExecutesLowHealthInRangeTarget()
	{
		var target = NinjaLimitBreakTarget(1, healthRatio: 0.30f, distanceToPlayer: 5f);

		AssertTrue(NinjaPvPLimitBreakPolicy.ShouldExecute(target), "Seiton Tenchu should execute a sub-threshold target inside 10y");
		AssertEqual(1UL, NinjaPvPLimitBreakPolicy.SelectBest([target])?.TargetId, "Seiton Tenchu should select the eligible execute target");
	}

	static void NinjaSeitonTenchuRejectsTargetAtOrAboveThreshold()
	{
		var atThreshold = NinjaLimitBreakTarget(1, healthRatio: 0.35f, distanceToPlayer: 5f);
		var aboveThreshold = NinjaLimitBreakTarget(2, healthRatio: 0.50f, distanceToPlayer: 5f);

		AssertFalse(NinjaPvPLimitBreakPolicy.ShouldExecute(atThreshold), "Seiton Tenchu should treat exactly 35% as not yet a guaranteed execute");
		AssertFalse(NinjaPvPLimitBreakPolicy.ShouldExecute(aboveThreshold), "Seiton Tenchu should not fire above the execute threshold");
	}

	static void NinjaSeitonTenchuRejectsRespectedInvulnerability()
	{
		var target = NinjaLimitBreakTarget(1, healthRatio: 0.10f, distanceToPlayer: 5f, hasRespectedInvulnerability: true);

		AssertFalse(NinjaPvPLimitBreakPolicy.ShouldExecute(target), "Seiton Tenchu should not fire into Hallowed Ground or Undead Redemption");
	}

	static void NinjaSeitonTenchuRejectsOutOfRangeTarget()
	{
		var target = NinjaLimitBreakTarget(1, healthRatio: 0.10f, distanceToPlayer: 12f);

		AssertFalse(NinjaPvPLimitBreakPolicy.ShouldExecute(target), "Seiton Tenchu should respect the 10y management range");
	}

	static void NinjaSeitonTenchuPrefersNearestExecutableTarget()
	{
		var far = NinjaLimitBreakTarget(1, healthRatio: 0.10f, distanceToPlayer: 9f);
		var near = NinjaLimitBreakTarget(2, healthRatio: 0.20f, distanceToPlayer: 3f);
		var outOfRange = NinjaLimitBreakTarget(3, healthRatio: 0.05f, distanceToPlayer: 15f);

		var ranked = NinjaPvPLimitBreakPolicy.Rank([far, near, outOfRange]);

		AssertEqual(2, ranked.Count, "Seiton Tenchu ranking should drop the out-of-range execute target");
		AssertEqual(2UL, ranked[0].TargetId, "Seiton Tenchu should prefer the nearest eligible execute target");
	}
}
