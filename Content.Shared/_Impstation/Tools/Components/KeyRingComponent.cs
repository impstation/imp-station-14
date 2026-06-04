
using Content.Shared.Access;
using Robust.Shared.Audio;
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

    [DataField]
    public EntityUid? JingleStream;

    public SoundSpecifier SoundJingle = new SoundPathSpecifier("/Audio/_Impstation/Items/keysjingle.ogg")
    {
        Params = new AudioParams
        {
            Volume = 1f,
            Variation = 0.15f
        }
    };
}

