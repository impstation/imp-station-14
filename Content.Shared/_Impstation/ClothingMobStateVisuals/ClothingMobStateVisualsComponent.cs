using Robust.Shared.GameStates;

namespace Content.Shared._Impstation.ClothingMobStateVisuals;

[RegisterComponent, NetworkedComponent]
public sealed partial class ClothingMobStateVisualsComponent : Component
{
    [DataField]
    public string IncapacitatedPrefix = "incapacitated";

    public string? ClothingPrefix = null;
}

public sealed class ClothingMobStateChangedEvent : EntityEventArgs
{
    public ClothingMobStateChangedEvent()
    {

    }
}
