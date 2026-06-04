
using Content.Shared.Access;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._Impstation.Tools.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class KeyRingComponent : Component
{
    [DataField, AutoNetworkedField]
    public TimeSpan UseDelay = TimeSpan.Zero;

    [DataField("blacklist")]
    public HashSet<AccessLevelPrototype> Blacklist = new();

    [DataField("minUseTime")]
    public double MinUseTime = 15;

    [DataField("maxUseTime")]
    public double MaxUseTime = 30;
}

