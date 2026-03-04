namespace Content.Server._Impstation.StationEvents.Components;

[RegisterComponent]
public sealed partial class BloodlessChimpRuleComponent: Component
{
    [DataField]
    public LocId ChimpAnnouncement = "station-event-bloodless-chimp-announcement";

    [DataField]
    public LocId TargetAnnouncement = "station-event-bloodless-chimp-hunt-announcement";
}
