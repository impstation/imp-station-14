using Content.Server.StationEvents.Events;
using Robust.Shared.Prototypes;

namespace Content.Server._Impstation.Station.Components;

[RegisterComponent, Access(typeof(MeteorSwarmSystem)), AutoGenerateComponentPause]
public sealed partial class StationSpecificMeteorComponent : Component
{
    /// <summary>
    /// Keypair of Meteors and their replacement prototypes
    /// </summary>
    [DataField]
    public Dictionary<EntProtoId, EntProtoId> MeteorReplacements = new();

    /// <summary>
    /// Keypair of meteor announcements and their replacements
    /// </summary>
    [DataField]
    public Dictionary<string, string> AnnouncementReplacements = new();

}
