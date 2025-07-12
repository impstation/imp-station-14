using Content.Server.StationEvents.Events;
using Robust.Shared.Prototypes;

namespace Content.Server._Impstation.Station.Components;

[RegisterComponent, Access(typeof(MeteorSwarmSystem)), AutoGenerateComponentPause]
public sealed partial class StationSpecificMeteorComponent : Component
{
    /// <summary>
    /// Default Small Meteor
    /// </summary>
    [DataField]
    public EntProtoId DefaultSmallMeteor = "MeteorSmall";
    /// <summary>
    /// Default Medium Meteor
    /// </summary>
    [DataField]
    public EntProtoId DefaultMediumMeteor = "MeteorLarge";
    /// <summary>
    /// Default Large Meteor
    /// </summary>
    [DataField]
    public EntProtoId DefaultLargeMeteor = "MeteorLarge";
    /// <summary>
    /// Replacement for Small Meteors
    /// </summary>
    [DataField]
    public EntProtoId? SmallMeteorReplacement;
    /// <summary>
    /// Replacement for Medium Meteors
    /// </summary>
    [DataField]
    public EntProtoId? MediumMeteorReplacement;
    /// <summary>
    /// Replacement for Large Meteors
    /// </summary>
    [DataField]
    public EntProtoId? LargeMeteorReplacement;
    /// <summary>
    /// Announcement Replacer
    /// </summary>
    [DataField]
    public string? ReplacementAnnouncement;
}
