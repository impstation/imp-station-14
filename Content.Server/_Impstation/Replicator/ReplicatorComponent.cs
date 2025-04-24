// these are HEAVILY based on the Bingle free-agent ghostrole from GoobStation, but reflavored and reprogrammed to make them more Robust (and less of a meme.)
// all credit for the core gameplay concepts and a lot of the core functionality of the code goes to the folks over at Goob, but I re-wrote enough of it to justify putting it in our filestructure.
// the original Bingle PR can be found here: https://github.com/Goob-Station/Goob-Station/pull/1519

using Robust.Shared.GameStates;

namespace Content.Server._Impstation.Replicator;

[RegisterComponent, NetworkedComponent]
public sealed partial class ReplicatorComponent : Component
{
    /// <summary>
    /// If a replicator is Queen, it will spawn a nest. 
    /// </summary>
    [DataField]
    public bool Queen;

    [DataField]
    public bool Upgraded;

    /// <summary>
    /// Keeps track of the nest this Replicator is responsible for.
    /// </summary>
    public EntityUid? MyNest;
}
