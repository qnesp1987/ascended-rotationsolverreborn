namespace RotationSolver.Basic.Rotations.Openers;

/// <summary>
/// One opener request: current state, what kind of action the rotation is about to
/// resolve, whether that action is usable, and the job's condition bits. The controller
/// never inspects <typeparamref name="TContext"/> — only the job's script hooks do.
/// </summary>
public readonly record struct OpenerInput<TContext>(
	OpenerState State,
	OpenerRequestKind RequestKind,
	bool CanUseRequestedAction,
	TContext Context)
	where TContext : struct
{
	/// <summary>
	/// Builds a GCD request. The GCD path consults no script hooks, so the context is
	/// intentionally <see langword="default"/>.
	/// </summary>
	public static OpenerInput<TContext> ForGcd(OpenerState state, bool canUseRequestedAction = true)
	{
		return new OpenerInput<TContext>(state, OpenerRequestKind.Gcd, canUseRequestedAction, default);
	}

	/// <summary>
	/// Builds an ability request carrying the job's condition bits for the script's
	/// interjection hooks.
	/// </summary>
	public static OpenerInput<TContext> ForAbility(OpenerState state, TContext context, bool canUseRequestedAction = true)
	{
		return new OpenerInput<TContext>(state, OpenerRequestKind.Ability, canUseRequestedAction, context);
	}
}
