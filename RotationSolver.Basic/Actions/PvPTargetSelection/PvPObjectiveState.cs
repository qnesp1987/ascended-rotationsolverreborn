using System.Collections.Frozen;

namespace RotationSolver.Basic.Actions.PvPTargetSelection;

/// <summary>
/// Builds objective-relevant hostile ids from verified Crystalline Conflict state so
/// scoring can value objective pressure without guessing unknown game identifiers.
/// </summary>
public static class PvPObjectiveState
{
	/// <summary>
	/// Return hostile ids that have a verified objective role in the current frame.
	/// Empty output is intentional while carrier detection remains unverified.
	/// </summary>
	public static IReadOnlySet<ulong> BuildObjectiveRelevantTargetIds()
	{
		var carrierId = CrystalCarrierState.GetCurrentCarrierId();
		if (carrierId is null)
		{
			return FrozenSet<ulong>.Empty;
		}

		return new[] { carrierId.Value }.ToFrozenSet();
	}
}
