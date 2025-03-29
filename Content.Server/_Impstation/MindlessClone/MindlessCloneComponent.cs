using Content.Shared.Dataset;
using Content.Shared.DoAfter;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Content.Shared._Impstation.MindlessClone;

namespace Content.Server._Impstation.MindlessClone;
/// <summary>
/// When applied to an entity with HumanoidAppearance, copies the appearance data of the nearest entity with HumanoidAppearance when spawned.
/// </summary>
[RegisterComponent]
public sealed partial class MindlessCloneComponent : Component
{
    /// <summary>
    /// whether or not the entity will pick a randomized phrase to say after spawning.
    /// </summary>
    [DataField]
    public bool SpeakOnSpawn = false;

    [DataField]
    public ProtoId<DatasetPrototype> PhrasesToPick = "MindlessCloneConfusion";
}
