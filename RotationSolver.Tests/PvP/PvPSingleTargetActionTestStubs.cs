namespace Dalamud.Game.ClientState.Objects.Types
{
	using System.Numerics;
	using Dalamud.Game.ClientState.Statuses;

	public interface IBattleChara
	{
		ulong GameObjectId { get; }

		uint CurrentHp => 0;

		uint MaxHp => 0;

		uint CurrentMp => 0;

		ulong TargetObjectId => 0;

		Vector3 Position => Vector3.Zero;

		float HitboxRadius => 0f;

		IReadOnlyList<IStatus>? StatusList => null;
	}
}

namespace Dalamud.Game.ClientState.Statuses
{
	public interface IStatus
	{
		uint StatusId { get; }
	}
}

namespace RotationSolver.Basic.Data
{
	public enum StatusID : uint
	{
		Guard = 3054,
		HallowedGround_1302 = 1302,
		Forte = 3178,
		Resilience = 3248,
	}
}

namespace RotationSolver.Basic.Actions
{
	using Dalamud.Game.ClientState.Objects.Types;

	public enum TargetType : byte
	{
		Big,
		Nearest,
	}

	public interface IAction
	{
	}

	public interface IBaseAction : IAction
	{
		ActionSetting Setting { get; set; }

		bool CanUse(
			out IAction act,
			bool skipStatusProvideCheck = false,
			bool skipStatusNeed = false,
			bool skipTargetStatusNeedCheck = false,
			bool skipComboCheck = false,
			bool skipCastingCheck = false,
			bool usedUp = false,
			bool skipAoeCheck = false,
			bool skipTTKCheck = false,
			byte gcdCountForAbility = 0,
			bool checkActionManager = false,
			TargetType targetOverride = default);
	}

	public class ActionSetting
	{
		public Func<IBattleChara, bool> CanTarget { get; set; } = _ => true;
	}
}
