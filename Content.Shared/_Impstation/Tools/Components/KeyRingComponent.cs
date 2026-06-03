
using Content.Shared.Access;

namespace Content.Shared._Impstation.Tools.Components;

[RegisterComponent]
public sealed partial class KeyRingComponent : Component
{
    /// <summary>
    /// Queue representing the key cards on the ring
    /// </summary>
    public Queue<AccessLevelPrototype> KeyCards = new();

    [DataField("blacklist")]
    public HashSet<AccessLevelPrototype> Blacklist = new();
}

