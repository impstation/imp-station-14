using Content.Shared.NPC.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Impstation.Traits.Assorted;

/// <summary>
/// This is used for the Hated by Dogs trait
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class HatedByDogsComponent : Component
{
    /// <summary>
    /// The faction added by the trait.
    /// </summary>
    [DataField]
    public ProtoId<NpcFactionPrototype> Faction = "DogHated";
}
