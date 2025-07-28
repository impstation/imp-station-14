using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Impstation.Tourist.Components;

/// <summary>
/// Component placed on a mob to make it a tourist and track photographed entities
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedTouristCameraSystem))]
public sealed partial class TouristComponent : Component
{
    [DataField]
    public EntProtoId PhotoCrewObjective = "PhotoCrewObjective";

    [DataField]
    public EntProtoId PhotoObjectObjective = "PhotoObjectObjective";

    [DataField("pisk")] //testing field
    public int pisk = 0;

    /*
    //Stores entities that were flashed by a tourist with their camera
    [DataField, AutoNetworkedField]
    public HashSet<EntityUid> PhotographedEntities = new();

    //Stores each objective and entities that fulfil it
    [DataField, AutoNetworkedField]
    public Dictionary<string, PhotoObjective> Objectives = new();
    */
}
