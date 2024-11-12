using Content.Shared.Actions;
using Robust.Shared.GameStates;

namespace Content.Shared._Impstation.Cosmiccult;

[RegisterComponent, NetworkedComponent]
public sealed partial class CosmicCultActionComponent : Component
{

}

// public sealed partial class CosmicSiphonEvent : EntityTargetActionEvent { }
public sealed partial class CosmicToolEvent : InstantActionEvent { }
