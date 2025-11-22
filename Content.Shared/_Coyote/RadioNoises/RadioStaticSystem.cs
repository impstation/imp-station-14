using Content.Shared.Popups;
using Content.Shared.Radio.Components;
using Content.Shared.Verbs;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Prototypes;

namespace Content.Shared._Coyote.RadioNoises;

/// <summary>
/// This handles...
/// </summary>
public sealed class RadioStaticSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototype = null!;
    [Dependency] private readonly SharedAudioSystem _audioSystem = null!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = null!;

    public static readonly VerbCategory RadioSquelchCat =
        new("verb-categories-radiosquelch", null);

    public static readonly VerbCategory RadioVolumeCat =
        new("verb-categories-radiovolume", null);

    public readonly List<float> RadioVolumeList =
        new()
        {
            0f, -1f, -2f, -3f, -4f, -5f, -6f, -7f, -8f, -9f,
        };

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<RadioStaticComponent, RadioReceivedEvent>(OnRadioReceive);
        SubscribeLocalEvent<RadioStaticComponent, GetVerbsEvent<Verb>>(AddRadioSquelchListVerb);
    }

    private void OnRadioReceive(
        EntityUid uid,
        RadioStaticComponent component,
        RadioReceivedEvent args
    )
    {
        if (IsSquelched(
                component,
                args.Channel,
                true))
            return;

        // First, get the correct sound pack based on the channel.
        var soundPack = component.SoundPack;
        // check if THIS one is a valid prototype.
        if (!_prototype.HasIndex(soundPack))
        {
            Logger.Warning(
                $"RadioStaticComponent on {ToPrettyString(uid)} has an invalid DEFAULT sound pack: {soundPack}");
            return;
        }

        if (component.DepartmentSoundPacks.TryGetValue(args.Channel, out var departmentSoundPack))
        {
            // is this a valid prototype?
            if (!_prototype.HasIndex(departmentSoundPack))
            {
                Logger.Warning(
                    $"RadioStaticComponent on {ToPrettyString(uid)} has an invalid department sound pack: {departmentSoundPack}");
            }
            else
            {
                // If it is, use it.
                soundPack = departmentSoundPack;
            }
        }

        // Now, judge the intent of the message.
        // See if its a stutter, a yell, exclamation, etc.
        var intent = GetIntentFromMessage(args.Message);

        PlaySound(
            uid,
            args.Receiver,
            soundPack,
            intent,
            component);
    }

    private void PlaySound(EntityUid uid,
        EntityUid? receiver,
        ProtoId<RadioStaticPrototype> soundPack,
        RadioStaticIntent intent,
        RadioStaticComponent component
    )
    {
        // First, turn proto ID into somthing we can reference.
        if (!_prototype.TryIndex(soundPack, out RadioStaticPrototype? radProt))
        {
            Logger.Warning($"RadioStaticComponent on {ToPrettyString(uid)} has an invalid sound pack: {soundPack}");
            return;
        }

        // so we have our sound pack, now we need to get the sound from it.
        SoundSpecifier? sound;
        switch (intent)
        {
            case RadioStaticIntent.Ask:
                sound = radProt.AskSound;
                break;
            case RadioStaticIntent.Stutter:
                sound = radProt.StutterSound;
                break;
            case RadioStaticIntent.Yell:
                sound = radProt.YellSound;
                break;
            case RadioStaticIntent.Exclamation:
                sound = radProt.ExclaimSound;
                break;
            case RadioStaticIntent.Mumble:
                sound = radProt.MumbleSound;
                break;
            case RadioStaticIntent.Say:
            default:
                sound = radProt.SaySound;
                break;
        }

        // radProt's sound intent entry may be null, so we need to check that.
        sound ??= radProt.SaySound;

        // if its a secret sound, find the owner of the radio and play it for them only.
        if (radProt.Secret)
        {
            // We need to find the owner of the radio.
            if (receiver is null)
            {
                return;
            }

            var hearer = receiver.Value;
            // Play the sound for the owner.
            _audioSystem.PlayEntity(
                sound,
                hearer,
                hearer,
                AudioParams.Default.WithVolume(component.Volume));
            return;
        }

        // Play the sound.
        _audioSystem.PlayPredicted(
            sound,
            uid,
            null,
            AudioParams.Default.WithVolume(component.Volume));
    }

    /// <summary>
    /// Gets the intent of the radio message based on its cuntent.
    /// See <see cref="RadioStaticIntent"/> for the intents.
    /// </summary>
    private static RadioStaticIntent GetIntentFromMessage(string message)
    {
        // If the message ends with a question mark, its an ask.
        if (message.EndsWith("?"))
        {
            return RadioStaticIntent.Ask;
        }

        // If the message ends with an exclamation mark, its an exclamation.
        // THOUGh if it ends with MORE than one exclamation mark, its a yell.
        if (message.EndsWith("!!"))
        {
            return RadioStaticIntent.Yell;
        }

        if (message.EndsWith("!"))
        {
            return RadioStaticIntent.Exclamation;
        }

        // If the message ends with a --, its a stutter.
        if (message.EndsWith("--"))
        {
            return RadioStaticIntent.Stutter;
        }

        // If the message ends with ... its a mumble.
        if (message.EndsWith("..."))
        {
            return RadioStaticIntent.Mumble;
        }

        // If it doesn't match any of the above, its a normal say.
        return RadioStaticIntent.Say;
    }

    /// <summary>
    /// makes a list of verbs that allow you to squelch the radio noise for a specific channel.
    /// </summary>
    private void AddRadioSquelchListVerb(EntityUid uid, RadioStaticComponent component, GetVerbsEvent<Verb> args)
    {
        // at the top, add a verb to toggle squelch for all channels.
        Verb toggleAllVerb = new()
        {
            Text = Loc.GetString(
                $"radio-squelch-verb-toggle-all-{(
                    component.OmniSquelch
                        ? "squelch"
                        : "unsquelch"
                )}"),
            Category = RadioSquelchCat,
            Act = () =>
            {
                ToggleOmniSquelch(
                    uid,
                    component,
                    args.User);
            },
            Disabled = false,
            Message = null
        };
        args.Verbs.Add(toggleAllVerb);
        // if its just some handy radio, dont need the fancy stuff here.
        if (!TryComp<EncryptionKeyHolderComponent>(uid, out var encHolder))
            return;
        // first, get a list of all the channels that this radio can hear.
        foreach (var chammel in encHolder.Channels)
        {
            bool quelched = IsSquelched(
                component,
                chammel);
            // add the verb
            Verb verb = new()
            {
                Text = Loc.GetString(
                    $"radio-squelch-verb-{(quelched
                        ? "unsquelch"
                        : "squelch")}",
                    ("channel", chammel)),
                Category = RadioSquelchCat,
                Act = () =>
                {
                    ToggleSquelch(
                        uid,
                        component,
                        chammel,
                        args.User);
                },
                Disabled = false,
                Message = null
            };
            args.Verbs.Add(verb);
        }
        // and now, add a verb to change the volume of the radio! from 1 to 20.
        foreach (var volume in RadioVolumeList)
        {
            // turn the volume into a string, formatted of 01, 02... 10, 11, 12... 20.
            var adjVolume = (int) (10f + volume);
            var volumeString = adjVolume.ToString("00");
            Verb volumeVerb = new()
            {
                Text = Loc.GetString(
                    "radio-volume-verb",
                    ("volume", volumeString)),
                Category = RadioVolumeCat,
                Disabled = Math.Abs(volume - component.Volume) < 0.1f,
                Act = () =>
                {
                    SetVolume(
                        uid,
                        component,
                        volume,
                        args);
                },
                Message = $"Set how loud the radio goes KSHHT to {(int)(10f + volume)} / 10",
            };
            args.Verbs.Add(volumeVerb);
        }
    }

    /// <summary>
    /// Toggles the squelch state of a radio for a specific channel.
    /// </summary>
    private void ToggleSquelch(
        EntityUid uid,
        RadioStaticComponent component,
        string channel,
        EntityUid? user = null
    )
    {
        var isAlreadySquelched = IsSquelched(
            component,
            channel);
        // if the channel is already squelched, unsquelch it.
        if (isAlreadySquelched)
        {
            component.SquelchedChannels.Remove(channel);
        }
        else
        {
            // otherwise, squelch it.
            component.SquelchedChannels.Add(channel);
        }

        if (user != null)
            return;
        var trueUser = user ?? EntityUid.Invalid;
        // Notify the user via a self-only popup.
        _popupSystem.PopupEntity(
            Loc.GetString(
                $"radio-squelch-{(isAlreadySquelched ? "unsquelched" : "squelched")}",
                ("channel", channel)),
            trueUser,
            trueUser);
    }

    /// <summary>
    /// Sets the volume of a radio.
    /// </summary>
    private void SetVolume(
        EntityUid uid,
        RadioStaticComponent component,
        float volume,
        GetVerbsEvent<Verb> args)
    {
        // set the volume of the radio.
        component.Volume = volume;
        _popupSystem.PopupEntity(
            Loc.GetString("radio-volume-verb-popup", ("volume", volume)),
            args.User,
            args.User);
    }

    /// <summary>
    /// Toggles the omni-squelch state of a radio.
    /// </summary>
    private void ToggleOmniSquelch(
        EntityUid uid,
        RadioStaticComponent component,
        EntityUid? user = null
    )
    {
        // if the omni-squelch is already on, turn it off.
        if (component.OmniSquelch)
        {
            component.OmniSquelch = false;
        }
        else
        {
            // otherwise, turn it on.
            component.OmniSquelch = true;
        }

        if (user != null)
            return;

        var trueUser = user ?? EntityUid.Invalid;
        // Notify the user via a self-only popup.
        _popupSystem.PopupEntity(
            Loc.GetString($"radio-squelch-omni-{(component.OmniSquelch ? "enabled" : "disabled")}"),
            trueUser,
            trueUser);
    }

    /// <summary>
    /// Checks if a radio is squelched for a specific channel
    /// </summary>
    private static bool IsSquelched(
        RadioStaticComponent component,
        string channel,
        bool checkOmniSquelch = false)
    {
        if (checkOmniSquelch && component.OmniSquelch)
        {
            // if the omni-squelch is on, return true.
            return true;
        }

        // if the channel is in the squelched channels, return true.
        return component.SquelchedChannels.Contains(channel);
    }
}

/// <summary>
/// Radio received event!
/// </summary>
[ByRefEvent]
public sealed class RadioReceivedEvent(
    EntityUid radioUid,
    EntityUid sender,
    EntityUid? receiver,
    string channel,
    string message)
    : EntityEventArgs
{
    public EntityUid RadioUid = radioUid;
    public EntityUid Sender = sender;
    public EntityUid? Receiver = receiver;
    public string Channel = channel;
    public string Message = message;
}
