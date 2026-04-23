using Content.Shared.Chemistry.Reagent;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Shared._Impstation.Sneezing;

/// <summary>
/// Marks entities that can sneeze and defines their sneeze/sniffle behavior.
/// </summary>
[RegisterComponent]
public sealed partial class SneezingComponent : Component
{
    [DataField]
    public ProtoId<SoundCollectionPrototype> SneezeCollection = "UnisexSneezes";
    [DataField]
    public ProtoId<SoundCollectionPrototype> SneezeCollection = "MaleSneezes";
    [DataField]
    public ProtoId<SoundCollectionPrototype> SneezeCollection = "FemaleSneezes";

    [DataField]
    public ProtoId<SoundCollectionPrototype> CoughCollection = "MaleCough";

    [DataField]
    public ProtoId<ReagentPrototype> MucusPrototype = "Mucus";

    [DataField]
    public float MucusAmount = 1f;

    [DataField]
    public float SniffleChance = 0.3f;
}
