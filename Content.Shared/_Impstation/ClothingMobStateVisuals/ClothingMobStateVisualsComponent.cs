using Content.Shared.Humanoid.Prototypes;
using Content.Shared.Mobs;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Toolshed.TypeParsers;

namespace Content.Shared._Impstation.ClothingMobStateVisuals;

[RegisterComponent, NetworkedComponent]
public sealed partial class ClothingMobStateVisualsComponent : Component
{
    [DataField]
    public string SpriteLayer = "incapacitated";

    [DataField]
    public Dictionary<string, List<PrototypeLayerData>> ClothingVisuals = new();

    [DataField]
    public string IncapacitatedPrefix = "incapacitated";

    public string? ClothingPrefix = null;
}

public sealed class ClothingMobStateChangedEvent : EntityEventArgs
{
    public ClothingMobStateChangedEvent(Entity<ClothingMobStateVisualsComponent> ent, EntityUid equippee, ProtoId<SpeciesPrototype> species, MobState newMobState)
    {
        Ent = ent;
        Equippee = equippee;
        SpeciesId = species;
        NewMobState = newMobState;
    }

    public Entity<ClothingMobStateVisualsComponent> Ent { get; }
    public EntityUid Equippee { get; }
    public ProtoId<SpeciesPrototype> SpeciesId { get; }
    public MobState NewMobState { get; }
}
