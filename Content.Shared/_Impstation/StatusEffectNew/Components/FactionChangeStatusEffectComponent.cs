using Content.Shared.NPC.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Impstation.StatusEffectNew.Components;
/// <summary>
/// A status effect to add a faction to an entity temporarily.
/// If using a status effect that is added or removed dynamically it is recommended to add a specific faction per status effect to avoid potential conflicts with faction addition and removal.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(raiseAfterAutoHandleState: true)]
public sealed partial class FactionChangeStatusEffectComponent : Component
{
    // The faction to be added
    [DataField(required: true)]
    public ProtoId<NpcFactionPrototype> Faction;

    // If the entity is already in the faction
    [DataField, AutoNetworkedField]
    public bool AlreadyIn;
}
