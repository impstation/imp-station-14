using Content.Shared.Actions;
using Content.Shared.DoAfter;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Serialization;

namespace Content.Shared._Impstation.Slasher;

/// <summary>
/// Gain-shroud event.
/// </summary>
public sealed partial class SlasherGainShroudEvent : InstantActionEvent { }

/// <summary>
/// Lose-shroud event.
/// </summary>
public sealed partial class SlasherLoseShroudEvent : InstantActionEvent { }

/// <summary>
/// Rend event.
/// </summary>
public sealed partial class SlasherRendEvent : EntityTargetActionEvent { }

/// <summary>
/// Rend do-after event.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class SlasherRendDoAfterEvent : SimpleDoAfterEvent { }

/// <summary>
/// Soul-harvest do-after event.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class SlasherHarvestSoulDoAfterEvent : SimpleDoAfterEvent { }

/// <summary>
/// Meathook placement event.
/// </summary>
public sealed partial class SlasherPlaceMeathookEvent : WorldTargetActionEvent { }

/// <summary>
/// Meathook placement do-after.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class SlasherPlaceMeathookDoAfterEvent : DoAfterEvent
{
	/// <summary>
	/// World coordinates where the meathook should be placed on success.
	/// </summary>
	[DataField]
	public NetCoordinates TargetCoordinates;

	/// <summary>
	/// Action entity that may need to be removed after placement succeeds.
	/// </summary>
	[DataField]
	public NetEntity ActionEntity;

	/// <summary>
	/// Whether the action entity should be removed after a successful placement.
	/// </summary>
	[DataField]
	public bool RemoveActionOnSuccess;

	private SlasherPlaceMeathookDoAfterEvent()
	{
	}

	/// <summary>
	/// Create the meathook placement payload.
	/// </summary>
	/// <param name="targetCoordinates">World coordinates where the meathook should be placed.</param>
	/// <param name="actionEntity">Action entity associated with the placement attempt.</param>
	/// <param name="removeActionOnSuccess">Whether to remove the action entity after success.</param>
	public SlasherPlaceMeathookDoAfterEvent(NetCoordinates targetCoordinates, NetEntity actionEntity, bool removeActionOnSuccess)
	{
		TargetCoordinates = targetCoordinates;
		ActionEntity = actionEntity;
		RemoveActionOnSuccess = removeActionOnSuccess;
	}

	/// <summary>Clone the placement payload.</summary>
	public override DoAfterEvent Clone() => new SlasherPlaceMeathookDoAfterEvent(TargetCoordinates, ActionEntity, RemoveActionOnSuccess);
}

/// <summary>
/// Effigy placement event.
/// </summary>
public sealed partial class SlasherPlaceEffigyEvent : WorldTargetActionEvent { }

/// <summary>
/// Effigy placement do-after.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class SlasherPlaceEffigyDoAfterEvent : DoAfterEvent
{
	/// <summary>
	/// World coordinates where the effigy should be placed on success.
	/// </summary>
	[DataField]
	public NetCoordinates TargetCoordinates;

	/// <summary>
	/// Action entity associated with the effigy placement attempt.
	/// </summary>
	[DataField]
	public NetEntity ActionEntity;

	private SlasherPlaceEffigyDoAfterEvent()
	{
	}

	/// <summary>
	/// Create the effigy placement payload.
	/// </summary>
	/// <param name="targetCoordinates">World coordinates where the effigy should be placed.</param>
	/// <param name="actionEntity">Action entity associated with the placement attempt.</param>
	public SlasherPlaceEffigyDoAfterEvent(NetCoordinates targetCoordinates, NetEntity actionEntity)
	{
		TargetCoordinates = targetCoordinates;
		ActionEntity = actionEntity;
	}

	/// <summary>Clone the placement payload.</summary>
	public override DoAfterEvent Clone() => new SlasherPlaceEffigyDoAfterEvent(TargetCoordinates, ActionEntity);
}

/// <summary>
/// Dark-step teleport event.
/// </summary>
public sealed partial class SlasherDarkStepEvent : WorldTargetActionEvent { }

/// <summary>
/// Dark-heal event.
/// </summary>
public sealed partial class SlasherDarkHealEvent : InstantActionEvent { }

/// <summary>
/// Effigy locator toggle event.
/// </summary>
public sealed partial class SlasherLocateEffigyEvent : InstantActionEvent { }

/// <summary>
/// Effigy feed do-after event.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class SlasherFeedEffigyDoAfterEvent : SimpleDoAfterEvent { }

/// <summary>
/// Effigy restore do-after event.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class SlasherRestoreEffigyDoAfterEvent : SimpleDoAfterEvent { }

/// <summary>
/// Fires when the effigy victory timer expires.
/// </summary>
[ByRefEvent]
public record struct SlasherVictoryTimerFiredEvent(EntityUid RuleEntity);
