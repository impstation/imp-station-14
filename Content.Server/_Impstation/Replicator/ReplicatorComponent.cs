// these are HEAVILY based on the Bingle free-agent ghostrole from GoobStation, but reflavored and reprogrammed to make them more Robust (and less of a meme.)
// all credit for the core gameplay concepts and a lot of the core functionality of the code goes to the folks over at Goob, but I re-wrote enough of it to justify putting it in our filestructure.
// the original Bingle PR can be found here: https://github.com/Goob-Station/Goob-Station/pull/1519

using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Server._Impstation.Replicator;

[RegisterComponent, NetworkedComponent]
public sealed partial class ReplicatorComponent : Component
{
    /// <summary>
    /// If a replicator is Queen, it will spawn a nest when it spawns.
    /// </summary>
    [DataField]
    public bool Queen;

    /// <summary>
    /// Current upgrade stage. Allows us to have an arbitrary number of upgrades, dictated by MaxUpgradeStage.
    /// Currently this is functionally a boolean - it's up to Kazne if he wants to make more stages.
    /// </summary>
    [DataField]
    public int UpgradeStage = 0;

    /// <summary>
    /// Keeps track of the nest this Replicator is responsible for.
    /// </summary>
    public EntityUid? MyNest;
}

[Serializable, NetSerializable]
public enum ReplicatorVisuals : byte
{
    Combat
}
