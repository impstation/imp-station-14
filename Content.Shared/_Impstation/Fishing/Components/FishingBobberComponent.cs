using System.Numerics;
using Robust.Shared.GameStates;

namespace Content.Shared._Impstation.Fishing.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class FishingBobberComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntityUid? Parent;

    [DataField, AutoNetworkedField]
    public bool Reeled;

    [DataField]
    public Vector2 Offset = Vector2.Zero;
}
