namespace RotationSolver.Tests;

internal static class TestAssertions
{
	internal static void True(bool actual, string message)
	{
		if (!actual)
		{
			throw new InvalidOperationException(message);
		}
	}

	internal static void Equal<T>(T expected, T actual, string message)
	{
		if (!EqualityComparer<T>.Default.Equals(expected, actual))
		{
			throw new InvalidOperationException($"{message}. Expected {expected}, got {actual}");
		}
	}

	internal static void False(bool actual, string message)
	{
		if (actual)
		{
			throw new InvalidOperationException(message);
		}
	}

	internal static void SequenceEqual(ReadOnlySpan<float> expected, ReadOnlySpan<float> actual, string message)
	{
		if (!expected.SequenceEqual(actual))
		{
			throw new InvalidOperationException($"{message}. Expected [{Format(expected)}], got [{Format(actual)}]");
		}
	}

	private static string Format(ReadOnlySpan<float> values)
	{
		return string.Join(", ", values.ToArray());
	}
}

internal static partial class PvPTestSuite
{
	static void AssertTrue(bool actual, string message)
	{
		TestAssertions.True(actual, message);
	}

	static void AssertEqual<T>(T expected, T actual, string message)
	{
		TestAssertions.Equal(expected, actual, message);
	}

	static void AssertFalse(bool actual, string message)
	{
		TestAssertions.False(actual, message);
	}
}

internal static partial class PvETestSuite
{
	static void AssertTrue(bool actual, string message)
	{
		TestAssertions.True(actual, message);
	}

	static void AssertEqual<T>(T expected, T actual, string message)
	{
		TestAssertions.Equal(expected, actual, message);
	}

	static void AssertFalse(bool actual, string message)
	{
		TestAssertions.False(actual, message);
	}

	static void AssertSequenceEqual(ReadOnlySpan<float> expected, ReadOnlySpan<float> actual, string message)
	{
		TestAssertions.SequenceEqual(expected, actual, message);
	}
}
