using System.Numerics;

namespace RotationSolver.Basic.Actions.PvPTargetSelection;

/// <summary>
/// Value-only combatant facts used by PvP query helpers.
/// </summary>
public readonly record struct PvPCombatantSnapshot(
	ulong ObjectId,
	float HealthRatio,
	uint CurrentHp,
	ulong TargetObjectId,
	Vector3 Position,
	float HitboxRadius);
