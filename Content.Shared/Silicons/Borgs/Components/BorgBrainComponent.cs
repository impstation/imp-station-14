using Content.Shared.Actions;
using Robust.Shared.GameStates;

namespace Content.Shared.Silicons.Borgs.Components;

/// <summary>
/// This is used for brains and mind receptacles
/// that can be inserted into a borg to transfer a mind.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(SharedBorgSystem))]
public sealed partial class BorgBrainComponent : Component
{

}

// imp addition
/// <summary>
/// Hibernation event for cyborg brains.
/// </summary>
public sealed partial class BorgBrainHibernationEvent : InstantActionEvent;
