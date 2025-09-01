using Robust.Shared.GameStates;

namespace Content.Shared._Goobstation.Changeling;


/// <summary>
///     Component that indicates that a person's DNA has been absorbed by a changeling.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(GoobAbsorbedSystem))]
public sealed partial class GoobAbsorbedComponent : Component
{

}
