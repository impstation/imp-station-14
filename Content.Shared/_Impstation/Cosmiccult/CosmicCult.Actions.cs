using Content.Shared.Actions;
using Robust.Shared.GameStates;

namespace Content.Shared._Impstation.Cosmiccult;

[RegisterComponent, NetworkedComponent]
public sealed partial class CosmicCultActionComponent : Component
{
    [DataField] public bool EntropyRequired = false;
    [DataField] public float EntropyCost = 0;

}

public sealed partial class CosmicSiphonEvent : EntityTargetActionEvent { }
public sealed partial class EventCosmicToolToggle : InstantActionEvent { }
