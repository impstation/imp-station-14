using Content.Shared.Access;
using Content.Shared.Destructible.Thresholds;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._Impstation.Tools.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class KeyRingComponent : Component
{
    [DataField, AutoNetworkedField]
    public TimeSpan UseDelay = TimeSpan.Zero;

    [DataField]
    public HashSet<ProtoId<AccessLevelPrototype>> Blacklist = new();

    [DataField]
    public MinMax Usetime = new (15,30);

    [DataField]
    public EntityUid? KeyringAudioStream;

    [DataField]
    public SoundSpecifier SuccessAudio = new SoundPathSpecifier("/Audio/_Impstation/Items/keyring_success.ogg")
    {
        Params = new AudioParams
        {
            Volume = 1f
        }
    };

    [DataField]
    public SoundSpecifier AttemptAudio = new SoundPathSpecifier("/Audio/_Impstation/Items/keyring_attempt.ogg")
    {
        Params = new AudioParams
        {
            Volume = 2f,
            Variation = 0.15f
        }
    };
}
