using Robust.Shared.Audio;
using Robust.Shared.Map;

namespace Content.Server._Impstation.Shuttles.Components;

[RegisterComponent, AutoGenerateComponentPause]
public sealed partial class AssaultPodComponent : Component
{
    [ViewVariables(VVAccess.ReadOnly), AutoPausedField]
    public TimeSpan LaunchTime;

    [ViewVariables(VVAccess.ReadOnly)]
    public EntityCoordinates TravelCoordinates;

    [DataField]
    public float TravelTime = 11f;

    [DataField]
    public float ArrivalKnockRadius = 15f;

    [DataField]
    public TimeSpan TimeTillLaunch = TimeSpan.FromSeconds(30);

    [DataField]
    public string SetStationAlertLevel = "red";

    [DataField]
    public LocId DepartureStationAnnouncement = "station-announcement-departure";

    [DataField]
    public LocId StationAnnouncementSender = "station-announcement-sender";

    [DataField]
    public SoundSpecifier DepartureAnnouncementSound = new SoundPathSpecifier("/Audio/_RMC14/Announcements/ARES/dropship_emergency.ogg");

    [DataField]
    public SoundSpecifier TravelSound = new SoundPathSpecifier("/Audio/_RMC14/Weapons/gun_orbital_travel.ogg");

    [DataField]
    public SoundSpecifier ArrivalSound = new SoundCollectionSpecifier("RMCExplosionBig");
}
