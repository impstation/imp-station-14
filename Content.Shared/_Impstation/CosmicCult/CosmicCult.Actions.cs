using Content.Shared.Actions;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._Impstation.CosmicCult;

[RegisterComponent, NetworkedComponent]
public sealed partial class CosmicCultActionComponent : Component
{

}
public sealed partial class EventCosmicSiphon : EntityTargetActionEvent { }
public sealed partial class EventCosmicBlank : EntityTargetActionEvent { }
public sealed partial class EventCosmicLapse : EntityTargetActionEvent { }
public sealed partial class EventCosmicPlaceMonument : InstantActionEvent { }
public sealed partial class EventCosmicReturn : InstantActionEvent { }
