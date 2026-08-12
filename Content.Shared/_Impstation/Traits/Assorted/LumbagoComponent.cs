using Robust.Shared.GameStates;

namespace Content.Shared._Impstation.Traits.Assorted;

[RegisterComponent, NetworkedComponent]
public sealed partial class LumbagoComponent : Component
{
    [DataField]
    public float LumbagoReminderChance = 0.005f;

    [DataField]
    public float FlareUpChance = 0.005f;

    [DataField]
    public float PullWalkSpeedMod = 0.5f;

    [DataField]
    public float PullSprintSpeedMod = 0.5f;
}
