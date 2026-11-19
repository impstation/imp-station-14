namespace Content.Server._Impstation.StationEvents.Components;

/// <summary>
/// This is used to change the moods of Thavens. Currently used with Delam.
/// </summary>
[RegisterComponent]
public sealed partial class ThavenMoodUpsetRuleComponent : Component
{
    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public bool NewSharedMoods;
}
