using Content.Shared.Access;
using Content.Shared.Destructible.Thresholds;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._Impstation.Tools.Components;
/// <summary>
/// Component for the HD's keyring, an item that can open any door or lock, stores relevant information such as audio paths, use delays, audio stream, and access black list.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class KeyRingComponent : Component
{
    /// <summary>
    /// Stored doafter length
    /// </summary>
    [AutoNetworkedField]
    public TimeSpan UseDelay = TimeSpan.Zero;

    /// <summary>
    /// List of access tags to blacklist
    /// </summary>
    [DataField]
    public HashSet<ProtoId<AccessLevelPrototype>> Blacklist = new();

    /// <summary>
    /// Range of time for the keyring to use.
    /// </summary>
    [DataField]
    public MinMax UseTime = new (15,30);

    /// <summary>
    /// Audio stream for the key ring.
    /// </summary>
    public EntityUid? KeyringAudioStream;

    /// <summary>
    /// Audio played on a successful use
    /// </summary>
    [DataField]
    public SoundSpecifier SuccessAudio = new SoundPathSpecifier("/Audio/_Impstation/Items/keyring_success.ogg")
    {
        Params = new AudioParams
        {
            Volume = 1f
        }
    };

    /// <summary>
    /// Audio played when using the item.
    /// </summary>
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
