using Content.Shared.Actions;
using Content.Shared.DoAfter;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Serialization;

namespace Content.Shared._Impstation.Slasher;

/// <summary>
/// Fired when the Slasher uses the gain-shroud action to enter stealth.
/// </summary>
public sealed partial class SlasherGainShroudEvent : InstantActionEvent { }

/// <summary>
/// Fired when the Slasher uses the lose-shroud action to voluntarily exit stealth.
/// </summary>
public sealed partial class SlasherLoseShroudEvent : InstantActionEvent { }

/// <summary>
/// Fired when the Slasher uses the Rend action to tear through an adjacent structure.
/// </summary>
public sealed partial class SlasherRendEvent : EntityTargetActionEvent { }

/// <summary>
/// Raised when the Slasher's Rend do-after completes or is cancelled.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class SlasherRendDoAfterEvent : SimpleDoAfterEvent { }

/// <summary>
/// Raised when the Slasher's meathook soul-harvest do-after completes or is cancelled.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class SlasherHarvestSoulDoAfterEvent : SimpleDoAfterEvent { }

/// <summary>
/// Fired when the Slasher uses the meathook placement action.
/// </summary>
public sealed partial class SlasherPlaceMeathookEvent : WorldTargetActionEvent { }

/// <summary>
/// Raised when the Slasher's meathook placement channel completes or is cancelled.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class SlasherPlaceMeathookDoAfterEvent : DoAfterEvent
{
	[DataField]
	public NetCoordinates TargetCoordinates;

	[DataField]
	public NetEntity ActionEntity;

	[DataField]
	public bool RemoveActionOnSuccess;

	/// <summary>
	/// Implements SlasherPlaceMeathookDoAfterEvent logic.
	/// </summary>
	private SlasherPlaceMeathookDoAfterEvent()
	{
	}

	/// <summary>
	/// Event type used by SlasherPlaceMeathookDoAfterEvent workflows.
	/// </summary>
	/// <param name="targetCoordinates">Parameter used by this method: targetCoordinates.</param>
	/// <param name="actionEntity">Parameter used by this method: actionEntity.</param>
	/// <param name="removeActionOnSuccess">Parameter used by this method: removeActionOnSuccess.</param>
	public SlasherPlaceMeathookDoAfterEvent(NetCoordinates targetCoordinates, NetEntity actionEntity, bool removeActionOnSuccess)
	{
		TargetCoordinates = targetCoordinates;
		ActionEntity = actionEntity;
		RemoveActionOnSuccess = removeActionOnSuccess;
	}

	/// <summary>
	/// Type definition for Clone.
	/// </summary>
	/// <param name="ActionEntity">Parameter used by this method: ActionEntity.</param>
	/// <param name="RemoveActionOnSuccess">Parameter used by this method: RemoveActionOnSuccess.</param>
	public override DoAfterEvent Clone() => new SlasherPlaceMeathookDoAfterEvent(TargetCoordinates, ActionEntity, RemoveActionOnSuccess);
}

/// <summary>
/// Fired when the Slasher uses the effigy placement action.
/// </summary>
public sealed partial class SlasherPlaceEffigyEvent : WorldTargetActionEvent { }

/// <summary>
/// Raised when the Slasher's effigy placement channel completes or is cancelled.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class SlasherPlaceEffigyDoAfterEvent : DoAfterEvent
{
	[DataField]
	public NetCoordinates TargetCoordinates;

	[DataField]
	public NetEntity ActionEntity;

	/// <summary>
	/// Implements SlasherPlaceEffigyDoAfterEvent logic.
	/// </summary>
	private SlasherPlaceEffigyDoAfterEvent()
	{
	}

	/// <summary>
	/// Event type used by SlasherPlaceEffigyDoAfterEvent workflows.
	/// </summary>
	/// <param name="targetCoordinates">Parameter used by this method: targetCoordinates.</param>
	/// <param name="actionEntity">Parameter used by this method: actionEntity.</param>
	public SlasherPlaceEffigyDoAfterEvent(NetCoordinates targetCoordinates, NetEntity actionEntity)
	{
		TargetCoordinates = targetCoordinates;
		ActionEntity = actionEntity;
	}

	/// <summary>
	/// Type definition for Clone.
	/// </summary>
	/// <param name="ActionEntity">Parameter used by this method: ActionEntity.</param>
	public override DoAfterEvent Clone() => new SlasherPlaceEffigyDoAfterEvent(TargetCoordinates, ActionEntity);
}

/// <summary>
/// Fired when the Slasher starts channeling a dark-step teleport.
/// </summary>
public sealed partial class SlasherDarkStepEvent : WorldTargetActionEvent { }

/// <summary>
/// Fired when the Slasher uses dark healing.
/// </summary>
public sealed partial class SlasherDarkHealEvent : InstantActionEvent { }

/// <summary>
/// Raised when the Slasher's effigy soul-fragment insertion do-after completes or is cancelled.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class SlasherFeedEffigyDoAfterEvent : SimpleDoAfterEvent { }

/// <summary>
/// Raised when a Slasher uses a soul fragment in hand to restore a destroyed effigy.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class SlasherRestoreEffigyDoAfterEvent : SimpleDoAfterEvent { }

/// <summary>
/// Fired by a timer 3 seconds after the effigy is fully fed to kick off the victory sequence.
/// </summary>
[ByRefEvent]
public record struct SlasherVictoryTimerFiredEvent(EntityUid RuleEntity);
