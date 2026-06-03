using Dalamud.Game.ClientState.Objects.Types;
using RotationSolver.Basic.Actions;
using RotationSolver.Basic.Actions.PvPTargetSelection;

namespace RotationSolver.Tests;

internal static partial class PvPTestSuite
{
	static void PvPSingleTargetActionUseRestrictsTargetAndMapsOptions()
	{
		var candidates = new IBattleChara[]
		{
			new TestBattleChara(10),
			new TestBattleChara(20),
			new TestBattleChara(30),
		};
		var testAction = new TestAction(candidates);
		Func<IBattleChara, bool> originalCanTarget = candidate => candidate.GameObjectId != 30;
		testAction.Setting.CanTarget = originalCanTarget;
		var options = new PvPSingleTargetActionOptions(
			SkipStatusProvideCheck: true,
			SkipStatusNeed: true,
			SkipTargetStatusNeedCheck: true,
			SkipComboCheck: true,
			SkipCastingCheck: true,
			UsedUp: true,
			SkipAoeCheck: true,
			SkipTtkCheck: true,
			GcdCountForAbility: 2,
			CheckActionManager: true,
			TargetOverride: TargetType.Nearest);

		var canUse = PvPSingleTargetActionUse.TryUseOn(testAction, 20, options, out var result);

		AssertTrue(canUse, "exact target helper should return the action CanUse result");
		AssertTrue(ReferenceEquals(testAction, result), "exact target helper should return the selected action");
		AssertEqual(1, testAction.AcceptedTargetIds.Count, "exact target helper should accept one candidate");
		AssertEqual(20UL, testAction.AcceptedTargetIds[0], "exact target helper should restrict CanTarget to the requested object id");
		AssertEqual(options, testAction.CapturedOptions, "exact target helper should forward CanUse options by name");
		AssertTrue(ReferenceEquals(originalCanTarget, testAction.Setting.CanTarget), "exact target helper should restore the original predicate");
	}

	static void PvPSingleTargetActionUseRestoresPredicateAfterFailure()
	{
		var testAction = new TestAction([new TestBattleChara(20)], shouldThrow: true);
		Func<IBattleChara, bool> originalCanTarget = candidate => candidate.GameObjectId == 20;
		testAction.Setting.CanTarget = originalCanTarget;

		try
		{
			PvPSingleTargetActionUse.TryUseOn(testAction, 20, new PvPSingleTargetActionOptions(), out _);
			throw new InvalidOperationException("expected helper call to throw");
		}
		catch (ApplicationException)
		{
		}

		AssertTrue(ReferenceEquals(originalCanTarget, testAction.Setting.CanTarget), "exact target helper should restore the predicate when CanUse throws");
	}

	static void PvPSingleTargetActionUseComposesOriginalPredicate()
	{
		var testAction = new TestAction(
		[
			new TestBattleChara(20),
			new TestBattleChara(30),
		]);
		Func<IBattleChara, bool> originalCanTarget = candidate => candidate.GameObjectId != 30;
		testAction.Setting.CanTarget = originalCanTarget;

		var canUse = PvPSingleTargetActionUse.TryUseOn(testAction, 30, new PvPSingleTargetActionOptions(), out _);

		AssertFalse(canUse, "exact target helper should keep the original target predicate");
		AssertEqual(1, testAction.CanUseCallCount, "exact target helper should still evaluate CanUse for nonzero target ids");
		AssertEqual(0, testAction.AcceptedTargetIds.Count, "exact target helper should not accept a target rejected by the original predicate");
		AssertTrue(ReferenceEquals(originalCanTarget, testAction.Setting.CanTarget), "exact target helper should restore the original predicate after rejected target checks");
	}

	static void PvPSingleTargetActionUseRejectsZeroTargetWithoutPredicateChange()
	{
		var testAction = new TestAction([new TestBattleChara(20)]);
		Func<IBattleChara, bool> originalCanTarget = candidate => candidate.GameObjectId == 20;
		testAction.Setting.CanTarget = originalCanTarget;

		var canUse = PvPSingleTargetActionUse.TryUseOn(testAction, 0, new PvPSingleTargetActionOptions(), out var result);

		AssertFalse(canUse, "exact target helper should reject a zero object id");
		AssertEqual(0, testAction.CanUseCallCount, "exact target helper should not call CanUse for a zero object id");
		AssertEqual<IAction?>(null, result, "exact target helper should leave no action for a zero object id");
		AssertTrue(ReferenceEquals(originalCanTarget, testAction.Setting.CanTarget), "exact target helper should not replace the predicate for a zero object id");
	}

	private sealed class TestAction(IReadOnlyList<IBattleChara> candidates, bool shouldThrow = false) : IBaseAction
	{
		public ActionSetting Setting { get; set; } = new();

		public List<ulong> AcceptedTargetIds { get; } = [];

		public PvPSingleTargetActionOptions CapturedOptions { get; private set; }

		public int CanUseCallCount { get; private set; }

		public bool CanUse(
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
			TargetType targetOverride = default)
		{
			CanUseCallCount++;
			CapturedOptions = new PvPSingleTargetActionOptions(
				SkipStatusProvideCheck: skipStatusProvideCheck,
				SkipStatusNeed: skipStatusNeed,
				SkipTargetStatusNeedCheck: skipTargetStatusNeedCheck,
				SkipComboCheck: skipComboCheck,
				SkipCastingCheck: skipCastingCheck,
				UsedUp: usedUp,
				SkipAoeCheck: skipAoeCheck,
				SkipTtkCheck: skipTTKCheck,
				GcdCountForAbility: gcdCountForAbility,
				CheckActionManager: checkActionManager,
				TargetOverride: targetOverride);

			foreach (var candidate in candidates)
			{
				if (Setting.CanTarget(candidate))
				{
					AcceptedTargetIds.Add(candidate.GameObjectId);
				}
			}

			if (shouldThrow)
			{
				throw new ApplicationException("can use failed");
			}

			act = this;
			return AcceptedTargetIds.Count > 0;
		}
	}

	private sealed class TestBattleChara(ulong gameObjectId) : IBattleChara
	{
		public ulong GameObjectId { get; } = gameObjectId;
	}
}
