using RotationSolver.Basic.Helpers;

namespace RotationSolver.Tests;

internal static partial class PvETestSuite
{
	// Local tolerance assert for floating-point estimates.
	static void AssertClose(double expected, double actual, double tolerance, string message)
	{
		AssertTrue(
			Math.Abs(expected - actual) <= tolerance,
			$"{message}. Expected {expected} +/- {tolerance}, got {actual}");
	}

	// Builds (ageSeconds, hpRatio) samples for a constant per-second drop.
	// i == 0 is the newest (age 0); older samples have larger age and higher HP.
	static (double, double)[] ConstantRateSamples(int count, double ratioAtNewest, double dropPerSecond)
	{
		var samples = new (double, double)[count];
		for (var i = 0; i < count; i++)
		{
			var age = (double)i;
			samples[i] = (age, ratioAtNewest + (dropPerSecond * i));
		}
		return samples;
	}

	static void TimeToKillEstimatorMatchesConstantKillRate()
	{
		var samples = ConstantRateSamples(count: 10, ratioAtNewest: 0.12, dropPerSecond: 0.02);
		var est = TimeToKillEstimator.EstimateRemainingSeconds(
			samples, currentHpRatio: 0.12,
			TimeToKillEstimator.DefaultWindowSeconds, TimeToKillEstimator.DefaultMinSamples,
			minSpanSeconds: 2.5, TimeToKillEstimator.DefaultRateEpsilon);
		AssertClose(6.0, est, 1e-6, "constant 0.02/s drop at 12% HP should estimate 6.0s remaining");
	}

	static void TimeToKillEstimatorTracksSteepRecentRateBelowDyingThreshold()
	{
		var samples = ConstantRateSamples(count: 4, ratioAtNewest: 0.10, dropPerSecond: 0.05);
		var est = TimeToKillEstimator.EstimateRemainingSeconds(
			samples, currentHpRatio: 0.10,
			TimeToKillEstimator.DefaultWindowSeconds, TimeToKillEstimator.DefaultMinSamples,
			minSpanSeconds: 2.5, TimeToKillEstimator.DefaultRateEpsilon);
		AssertClose(2.0, est, 1e-6, "steep recent rate should estimate 2.0s remaining");
		AssertTrue(est < 10.0, "steep recent rate must fall below the dying threshold so the buff is blocked");
	}

	static void TimeToKillEstimatorIgnoresSamplesOutsideWindow()
	{
		var inWindow = ConstantRateSamples(count: 10, ratioAtNewest: 0.12, dropPerSecond: 0.02);
		var all = new (double, double)[inWindow.Length + 3];
		Array.Copy(inWindow, all, inWindow.Length);
		all[inWindow.Length + 0] = (20.0, 0.95);
		all[inWindow.Length + 1] = (21.0, 0.95);
		all[inWindow.Length + 2] = (22.0, 0.95);
		var est = TimeToKillEstimator.EstimateRemainingSeconds(
			all, currentHpRatio: 0.12,
			TimeToKillEstimator.DefaultWindowSeconds, TimeToKillEstimator.DefaultMinSamples,
			minSpanSeconds: 2.5, TimeToKillEstimator.DefaultRateEpsilon);
		AssertClose(6.0, est, 1e-6, "samples older than the window must not affect the estimate");
	}

	static void TimeToKillEstimatorReturnsNaNForTooFewSamples()
	{
		var samples = new (double, double)[] { (1.0, 0.30), (0.0, 0.28) };
		var est = TimeToKillEstimator.EstimateRemainingSeconds(
			samples, currentHpRatio: 0.28,
			TimeToKillEstimator.DefaultWindowSeconds, TimeToKillEstimator.DefaultMinSamples,
			minSpanSeconds: 2.5, TimeToKillEstimator.DefaultRateEpsilon);
		AssertTrue(double.IsNaN(est), "fewer than the minimum samples should return NaN");
	}

	static void TimeToKillEstimatorReturnsNaNForShortSpan()
	{
		var samples = new (double, double)[] { (2.0, 0.20), (1.0, 0.18), (0.0, 0.16) };
		var est = TimeToKillEstimator.EstimateRemainingSeconds(
			samples, currentHpRatio: 0.16,
			TimeToKillEstimator.DefaultWindowSeconds, TimeToKillEstimator.DefaultMinSamples,
			minSpanSeconds: 2.5, TimeToKillEstimator.DefaultRateEpsilon);
		AssertTrue(double.IsNaN(est), "a sample span under the minimum should return NaN");
	}

	static void TimeToKillEstimatorReturnsNaNForFlatHp()
	{
		var samples = ConstantRateSamples(count: 10, ratioAtNewest: 0.50, dropPerSecond: 0.0);
		var est = TimeToKillEstimator.EstimateRemainingSeconds(
			samples, currentHpRatio: 0.50,
			TimeToKillEstimator.DefaultWindowSeconds, TimeToKillEstimator.DefaultMinSamples,
			minSpanSeconds: 2.5, TimeToKillEstimator.DefaultRateEpsilon);
		AssertTrue(double.IsNaN(est), "flat HP (rate at/below epsilon) should return NaN");
	}

	static void TimeToKillEstimatorReturnsNaNForRisingHp()
	{
		var samples = ConstantRateSamples(count: 10, ratioAtNewest: 0.28, dropPerSecond: -0.02);
		var est = TimeToKillEstimator.EstimateRemainingSeconds(
			samples, currentHpRatio: 0.28,
			TimeToKillEstimator.DefaultWindowSeconds, TimeToKillEstimator.DefaultMinSamples,
			minSpanSeconds: 2.5, TimeToKillEstimator.DefaultRateEpsilon);
		AssertTrue(double.IsNaN(est), "rising HP should return NaN");
	}

	static void TimeToKillEstimatorHighHpYieldsLargeEstimateForOpener()
	{
		var samples = ConstantRateSamples(count: 4, ratioAtNewest: 0.80, dropPerSecond: 0.05);
		var est = TimeToKillEstimator.EstimateRemainingSeconds(
			samples, currentHpRatio: 0.80,
			TimeToKillEstimator.DefaultWindowSeconds, TimeToKillEstimator.DefaultMinSamples,
			minSpanSeconds: 2.5, TimeToKillEstimator.DefaultRateEpsilon);
		AssertClose(16.0, est, 1e-6, "80% HP at 0.05/s should estimate 16.0s remaining");
		AssertTrue(est >= 10.0, "high HP must keep the opener estimate above the dying threshold");
	}

	static void TimeToKillEstimatorReturnsNaNForNaNCurrentHp()
	{
		var samples = ConstantRateSamples(count: 10, ratioAtNewest: 0.12, dropPerSecond: 0.02);
		var est = TimeToKillEstimator.EstimateRemainingSeconds(
			samples, currentHpRatio: double.NaN,
			TimeToKillEstimator.DefaultWindowSeconds, TimeToKillEstimator.DefaultMinSamples,
			minSpanSeconds: 2.5, TimeToKillEstimator.DefaultRateEpsilon);
		AssertTrue(double.IsNaN(est), "a NaN current HP ratio should return NaN");
	}
}
