using Content.Client.Gravity;
using Content.Shared._Impstation.Gravity;
using Robust.Shared.GameStates;

namespace Content.Client._Impstation.Gravity;

[RegisterComponent, NetworkedComponent]
[Access(typeof(ZeroGravityAreaSystem))]
public sealed partial class ZeroGravityAreaComponent : SharedZeroGravityAreaComponent
{
}
