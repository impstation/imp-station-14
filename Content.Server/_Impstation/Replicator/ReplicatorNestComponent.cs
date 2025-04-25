// these are HEAVILY based on the Bingle free-agent ghostrole from GoobStation, but reflavored and reprogrammed to make them more Robust (and less of a meme.)
// all credit for the core gameplay concepts and a lot of the core functionality of the code goes to the folks over at Goob, but I re-wrote enough of it to justify putting it in our filestructure.
// the original Bingle PR can be found here: https://github.com/Goob-Station/Goob-Station/pull/1519

using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Server._Impstation.Replicator;

[RegisterComponent, NetworkedComponent]
public sealed partial class ReplicatorNestComponent : Component
{
    /// <summary>
    /// The container we're storing things in. If the nest is destroyed, anything in this will be dumped out.
    /// </summary>
    public Container Hole = default!;

    /// <summary>
    /// Total stored points. Points are acquired by putting things in the hole.
    /// </summary>
    public int TotalPoints = 0;

    /// <summary>
    /// The current level of the nest.
    /// </summary>
    public int CurrentLevel = 1;

    /// <summary>
    /// The number of additional points given for living targets.
    /// </summary>
    public int BonusPointsAlive = 5;
    /// <summary>
    /// The number of additional points given for humanoid targets.
    /// </summary>
    public int BonusPointsHumanoid = 5;
    /// <summary>
    /// The number of points required to spawn a new replicator.
    /// </summary>
    public int SpawnNewAt = 20;

    /// <summary>
    /// Entity to be spawned when reaching spawn point thresholds.
    /// </summary>
    [DataField]
    public EntProtoId ToSpawn = "SpawnPointGhostReplicator";

    /// <summary>
    /// The level at which the nest starts accepting living beings.
    /// </summary>
    [DataField]
    public int AllowLivingThreshold = 2;

    public SoundSpecifier FallingSound = new SoundPathSpecifier("/Audio/Effects/falling.ogg");
}
