using Content.Shared.NPC.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Impstation.StatusEffectNew.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(raiseAfterAutoHandleState: true)]
public sealed partial class FactionChangeStatusEffectComponent : Component
{
    [DataField(required: true)]
    public ProtoId<NpcFactionPrototype> Faction;

    [DataField, AutoNetworkedField]
    public bool AlreadyIn;
}
