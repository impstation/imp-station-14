using Content.Shared.Actions;
using Robust.Shared.GameStates;

namespace Content.Shared._Impstation.Cosmiccult;

[RegisterComponent, NetworkedComponent]
public sealed partial class CosmicCultActionComponent : Component
{

}
public sealed partial class EventCosmicToolToggle : InstantActionEvent { }
public sealed partial class EventCosmicSiphon : EntityTargetActionEvent { }
public sealed partial class EventCosmicBlank : EntityTargetActionEvent { }
public sealed partial class EventCosmicLapse : EntityTargetActionEvent { }
public sealed partial class EventCosmicPlaceMonument : InstantActionEvent { }
