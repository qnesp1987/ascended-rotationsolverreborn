namespace RotationSolver.Basic.Rotations.Openers;

/// <summary>
/// Immutable opener progress so rotations can hold, copy, and replay opener position
/// without hidden state: the owning GCD step, the next GCD index, the pending weave
/// slot, and whether the opener has ended.
/// </summary>
public readonly record struct OpenerState(
	int Step,
	int NextGcdIndex,
	OpenerWeaveSlot NextWeaveSlot,
	bool IsTerminal)
{
	private const int PrepullStep = 0;
	private const int FirstGcdIndex = 1;

	/// <summary>
	/// Starts at the prepull weave slot when the script schedules a pre-pull ability,
	/// otherwise at the first GCD, mirroring how openers begin on a countdown.
	/// </summary>
	public static OpenerState Start(bool startsWithPrepullWeave)
	{
		return startsWithPrepullWeave
			? new OpenerState(Step: PrepullStep, NextGcdIndex: FirstGcdIndex, NextWeaveSlot: OpenerWeaveSlot.Prepull, IsTerminal: false)
			: new OpenerState(Step: FirstGcdIndex, NextGcdIndex: FirstGcdIndex, NextWeaveSlot: OpenerWeaveSlot.None, IsTerminal: false);
	}

	/// <summary>
	/// Marks the opener ended so every later request resolves to
	/// <see cref="OpenerResultKind.Complete"/> instead of replaying script entries.
	/// </summary>
	public OpenerState Complete()
	{
		return this with { IsTerminal = true };
	}
}
