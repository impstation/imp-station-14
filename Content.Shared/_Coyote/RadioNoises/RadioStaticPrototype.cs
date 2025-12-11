using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Shared._Coyote.RadioNoises;

/// <summary>
/// Prototype for the sound pack that contains the static noise.
/// </summary>
[Prototype("RadioStatic")]
public sealed class RadioStaticPrototype : IPrototype
{
    /// <summary>
    /// The ID of the sound pack.
    /// </summary>
    [IdDataField]
    public string ID { get; private set; } = null!;

    /// <summary>
    /// Just play this sound to the receiver when a radio message is received.
    /// For like, secret channels or something.
    /// </summary>
    [DataField("secret")]
    public bool Secret { get; set; } = false;

    /// <summary>
    /// Default / standard say sound to be played when a radio message is received.
    /// </summary>
    [DataField("saySound", required: true)]
    public SoundSpecifier SaySound { get; set; } =
        new SoundPathSpecifier("/Audio/_Coyote/Effects/Radio/general.ogg");

    /// <summary>
    /// Ask sound to be played when a radio message is received.
    /// </summary>
    [DataField("askSound")]
    public SoundSpecifier? AskSound { get; set; } = null!;

    /// <summary>
    /// Exclaim sound to be played when a radio message is received.
    /// </summary>
    [DataField("exclaimSound")]
    public SoundSpecifier? ExclaimSound { get; set; } = null!;

    /// <summary>
    /// Yell sound to be played when a radio message is received.
    /// </summary>
    [DataField("yellSound")]
    public SoundSpecifier? YellSound { get; set; } = null!;

    /// <summary>
    /// Mumble sound to be played when a radio message is received.
    /// </summary>
    [DataField("mumbleSound")]
    public SoundSpecifier? MumbleSound { get; set; } = null!;

    /// <summary>
    /// Stutter sound to be played when a radio message is received.
    /// </summary>
    [DataField("stutterSound")]
    public SoundSpecifier? StutterSound { get; set; } = null!;
}

/// <summary>
/// Enumeration for the intent of the radio message.
/// </summary>
public enum RadioStaticIntent
{
    /// <summary>
    /// Normal intent, no special handling.
    /// </summary>
    Say,

    /// <summary>
    /// Ask intent for a radio message.
    /// </summary>
    Ask,

    /// <summary>
    /// Intent for a yell message.
    /// </summary>
    Yell,

    /// <summary>
    /// Intent for an exclamation message.
    /// </summary>
    Exclamation,

    /// <summary>
    /// Intent for a stuttery message.
    /// </summary>
    Stutter,

    /// <summary>
    /// Intent for a mumble message.
    /// </summary>
    Mumble, // hey remember when people used Mumble?
}
