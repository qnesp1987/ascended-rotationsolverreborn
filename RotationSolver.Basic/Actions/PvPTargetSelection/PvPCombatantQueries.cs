using System.Numerics;

namespace RotationSolver.Basic.Actions.PvPTargetSelection;

/// <summary>
/// Provides pure PvP combatant lookups so rotation code can share snapshot-based
/// focus and spatial rules without depending on live game objects.
/// </summary>
public static class PvPCombatantQueries
{
	private const float ProjectedY = 0f;

	/// <summary>
	/// Finds a combatant by object id after the caller has already built trusted snapshots.
	/// </summary>
	public static PvPCombatantSnapshot? FindById(
		IReadOnlyList<PvPCombatantSnapshot> combatants,
		ulong objectId)
	{
		ArgumentNullException.ThrowIfNull(combatants);

		foreach (var combatant in combatants)
		{
			if (combatant.ObjectId == objectId)
			{
				return combatant;
			}
		}

		return null;
	}

	/// <summary>
	/// Counts ally focus from target ids exactly as live targeting exposes it.
	/// </summary>
	public static int CountAlliesTargeting(
		IReadOnlyList<PvPCombatantSnapshot> allies,
		ulong hostileObjectId)
	{
		ArgumentNullException.ThrowIfNull(allies);

		return CountTargeting(allies, hostileObjectId);
	}

	/// <summary>
	/// Counts hostile focus from target ids exactly as live targeting exposes it.
	/// </summary>
	public static int CountHostilesTargeting(
		IReadOnlyList<PvPCombatantSnapshot> hostiles,
		ulong allyObjectId)
	{
		ArgumentNullException.ThrowIfNull(hostiles);

		return CountTargeting(hostiles, allyObjectId);
	}

	/// <summary>
	/// Counts living allies near a target position for shared burst support checks.
	/// </summary>
	public static int CountAlliesNear(
		IReadOnlyList<PvPCombatantSnapshot> allies,
		Vector3 targetPosition,
		float radius)
	{
		ArgumentNullException.ThrowIfNull(allies);

		return CountLivingNear(allies, targetPosition, radius);
	}

	/// <summary>
	/// Counts living hostiles near a position for shared pressure and safety checks.
	/// </summary>
	public static int CountHostilesNear(
		IReadOnlyList<PvPCombatantSnapshot> hostiles,
		Vector3 position,
		float radius)
	{
		ArgumentNullException.ThrowIfNull(hostiles);

		return CountLivingNear(hostiles, position, radius);
	}

	/// <summary>
	/// Measures the nearest living hostile distance after subtracting hostile hitbox radius.
	/// </summary>
	public static float DistanceToNearestHostile(
		IReadOnlyList<PvPCombatantSnapshot> hostiles,
		Vector3 position)
	{
		ArgumentNullException.ThrowIfNull(hostiles);

		var nearestDistance = float.MaxValue;
		foreach (var hostile in hostiles)
		{
			if (!IsLiving(hostile))
			{
				continue;
			}

			var distance = Vector3.Distance(position, hostile.Position) - hostile.HitboxRadius;
			if (distance < nearestDistance)
			{
				nearestDistance = distance;
			}
		}

		return nearestDistance;
	}

	/// <summary>
	/// Counts living hostiles intersecting an XZ-projected line action footprint.
	/// </summary>
	public static int CountHostilesInLine(
		IReadOnlyList<PvPCombatantSnapshot> hostiles,
		Vector3 origin,
		Vector3 targetPosition,
		float range,
		float halfWidth)
	{
		ArgumentNullException.ThrowIfNull(hostiles);

		var projectedOrigin = Project(origin);
		var projectedTarget = Project(targetPosition);
		var direction = projectedTarget - projectedOrigin;
		if (direction.LengthSquared() <= float.Epsilon)
		{
			return 0;
		}

		var lineDirection = Vector3.Normalize(direction);
		var count = 0;
		foreach (var hostile in hostiles)
		{
			if (!IsLiving(hostile))
			{
				continue;
			}

			if (IsInLine(hostile, projectedOrigin, lineDirection, range, halfWidth))
			{
				count++;
			}
		}

		return count;
	}

	private static int CountTargeting(
		IReadOnlyList<PvPCombatantSnapshot> combatants,
		ulong targetObjectId)
	{
		var count = 0;
		foreach (var combatant in combatants)
		{
			if (combatant.TargetObjectId == targetObjectId)
			{
				count++;
			}
		}

		return count;
	}

	private static int CountLivingNear(
		IReadOnlyList<PvPCombatantSnapshot> combatants,
		Vector3 position,
		float radius)
	{
		var count = 0;
		foreach (var combatant in combatants)
		{
			if (IsLiving(combatant)
				&& Vector3.Distance(combatant.Position, position) <= radius)
			{
				count++;
			}
		}

		return count;
	}

	private static bool IsInLine(
		PvPCombatantSnapshot hostile,
		Vector3 origin,
		Vector3 lineDirection,
		float range,
		float halfWidth)
	{
		var hostilePosition = Project(hostile.Position);
		var toHostile = hostilePosition - origin;
		var forwardDistance = Vector3.Dot(lineDirection, toHostile);
		if (Math.Clamp(forwardDistance, 0f, range) != forwardDistance)
		{
			return false;
		}

		var perpendicularDistance = Vector3.Cross(lineDirection, toHostile).Length();
		return perpendicularDistance <= halfWidth + hostile.HitboxRadius;
	}

	private static Vector3 Project(Vector3 position)
	{
		return new Vector3(position.X, ProjectedY, position.Z);
	}

	private static bool IsLiving(PvPCombatantSnapshot combatant)
	{
		return combatant.CurrentHp > 0;
	}
}
