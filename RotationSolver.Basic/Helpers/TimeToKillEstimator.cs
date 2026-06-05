using System;

namespace RotationSolver.Basic.Helpers;

/// <summary>
/// Estimates how long until a target reaches 0 HP from a recent series of HP-ratio
/// samples. The kill-rate is the slope of an ordinary least-squares fit over the most
/// recent <c>windowSeconds</c> of samples, so the estimate tracks current throughput
/// (burst, execute, Limit Break) instead of a whole-fight average — which is why the
/// previous whole-window average let end-of-fight buffs fire just before death.
/// </summary>
/// <remarks>
/// Pure and dependency-free so it can be unit tested without game state. The impure
/// side (reading recorded HP and the clock) lives in the <c>GetTTK</c> adapter.
/// </remarks>
internal static class TimeToKillEstimator
{
	/// <summary>Trailing window, in seconds, used to fit the current kill-rate.</summary>
	internal const double DefaultWindowSeconds = 10d;

	/// <summary>Minimum in-window samples required for a meaningful slope.</summary>
	internal const int DefaultMinSamples = 3;

	/// <summary>
	/// Kill-rate (HP fraction per second) at or below which the rate is treated as
	/// unknown, e.g. flat HP during an invulnerability phase or a lull.
	/// </summary>
	internal const double DefaultRateEpsilon = 1e-4d;

	/// <summary>
	/// Estimates remaining seconds until 0 HP, or <see cref="double.NaN"/> when the recent
	/// kill-rate cannot be established (too few in-window samples, span under
	/// <paramref name="minSpanSeconds"/>, degenerate fit, or HP flat/increasing). NaN matches
	/// the existing "unknown TTK" sentinel so callers behave exactly as before.
	/// </summary>
	/// <param name="samples">Recent samples as (ageSeconds, hpRatio); ageSeconds is seconds before "now". Order does not matter.</param>
	/// <param name="currentHpRatio">Current HP fraction in [0,1].</param>
	/// <param name="windowSeconds">Only samples with ageSeconds &lt;= this take part in the fit.</param>
	/// <param name="minSamples">Minimum in-window samples for a usable fit.</param>
	/// <param name="minSpanSeconds">Minimum spread between oldest and newest in-window sample.</param>
	/// <param name="rateEpsilon">Kill-rate at/below which the result is unknown.</param>
	internal static double EstimateRemainingSeconds(
		ReadOnlySpan<(double ageSeconds, double hpRatio)> samples,
		double currentHpRatio,
		double windowSeconds,
		int minSamples,
		double minSpanSeconds,
		double rateEpsilon)
	{
		if (double.IsNaN(currentHpRatio))
		{
			return double.NaN;
		}

		var n = 0;
		double sumX = 0d, sumY = 0d, sumXX = 0d, sumXY = 0d;
		var minX = double.PositiveInfinity;
		var maxX = double.NegativeInfinity;

		foreach (var (ageSeconds, hpRatio) in samples)
		{
			if (ageSeconds > windowSeconds)
			{
				continue;
			}

			// x increases with time (newer = larger), so a falling HP gives a negative slope.
			var x = -ageSeconds;
			n++;
			sumX += x;
			sumY += hpRatio;
			sumXX += x * x;
			sumXY += x * hpRatio;
			if (x < minX)
			{
				minX = x;
			}

			if (x > maxX)
			{
				maxX = x;
			}
		}

		if (n < minSamples || (maxX - minX) < minSpanSeconds)
		{
			return double.NaN;
		}

		var denominator = (n * sumXX) - (sumX * sumX);
		if (denominator == 0d)
		{
			return double.NaN;
		}

		var slope = ((n * sumXY) - (sumX * sumY)) / denominator;
		var rate = -slope;
		if (rate <= rateEpsilon)
		{
			return double.NaN;
		}

		return currentHpRatio / rate;
	}
}
