using Robust.Shared.Audio;

namespace Content.Server._Impstation.Shuttles.Components;

[RegisterComponent]
public sealed partial class AssaultPodConsoleComponent : Component
{
    [ViewVariables]
    public bool Purchased;

    [ViewVariables]
    public bool Started;

    [DataField]
    public int LandingVariationRange = 20;

    [DataField]
    public LocId BeginDepartureTimerAnnouncement = "assault-pod-announcement-begin-departure-timer";

    [DataField]
    public LocId NukieAnnouncementSender = "assault-pod-announcement-sender";

    [DataField]
    public SoundSpecifier BeginDepartureTimerAnnouncementSound = new SoundPathSpecifier("/Audio/_RMC14/Effects/ob_alert.ogg");
}
