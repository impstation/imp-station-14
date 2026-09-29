using Content.Shared.Damage;
using Content.Shared.DoAfter;
using Content.Shared.Item.ItemToggle.Components;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Content.Shared.Whitelist; // imp

namespace Content.Shared.Medical;

/// <summary>
/// This is used for defibrillators; a machine that shocks a dead
/// person back into the world of the living.
/// Uses <see cref="ItemToggleComponent"/>
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class DefibrillatorComponent : Component
{
    /// <summary>
    /// How much damage is healed from getting zapped.
    /// </summary>
    [DataField(required: true), AutoNetworkedField]
    public DamageSpecifier ZapHeal = default!;

    /// <summary>
    /// The electrical damage from getting zapped.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int ZapDamage = 5;

    /// <summary>
    /// How long the victim will be electrocuted after getting zapped.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float WritheDuration = 3f; // imp float TODO IMP: This should be a TimeSpan

    /// <summary>
    /// ID of the cooldown use delay.
    /// </summary>
    [DataField]
    public string DelayId = "defib-delay";

    /// <summary>
    /// Cooldown after using the defibrillator.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan ZapDelay = TimeSpan.FromSeconds(5);

    /// <summary>
    /// How long the doafter for zapping someone takes.
    /// </summary>
    /// <remarks>
    /// This is synced with the audio; do not change one but not the other.
    /// </remarks>
    [DataField, AutoNetworkedField]
    public float DoAfterDuration = 3f; // imp, move timespan -> float TODO IMP: This should be a TimeSpan

    /// <summary>
    /// Defib only works on mobs with id in this list, or works for anything if this list is null #IMP
    /// </summary>
    [DataField]
    public EntityWhitelist? Whitelist;

    /// <summary>
    /// Whether or not to have the defib pop up text, such as body composition, rot, intelligence, etc. #IMP
    /// </summary>
    [DataField]
    public bool ShowMessages = true;

    /// <summary>
    /// Can we skip the doafter. #IMP
    /// </summary>
    [DataField]
    public bool SkipDoAfter = false;

    /// <summary>
    /// Can we ignore the toggle. #IMP
    /// </summary>
    [DataField]
    public bool IgnoreToggle = false;

    /// <summary>
    /// Can we ignore the powercell. #IMP
    /// </summary>
    [DataField]
    public bool IgnorePowerCell = false;

    /// <summary>
    /// Can the defibbed entity skip the critical state and go straight to alive if they have low enough damage?. #IMP
    /// </summary>
    [DataField]
    public bool AllowSkipCrit = false;

    /// <summary>
    /// If false cancels the doafter when moving.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool AllowDoAfterMovement = true;

    /// <summary>
    /// Can the defibrilator be used on mobs in critical mobstate?
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool CanDefibCrit = true;

    /// <summary>
    /// The sound to play when someone is zapped.
    /// </summary>
    [DataField]
    public SoundSpecifier? ZapSound = new SoundPathSpecifier("/Audio/Items/Defib/defib_zap.ogg");

    /// <summary>
    /// The sound to play when starting the doafter.
    /// </summary>
    [DataField]
    public SoundSpecifier? ChargeSound = new SoundPathSpecifier("/Audio/Items/Defib/defib_charge.ogg");

    [DataField]
    public SoundSpecifier? FailureSound = new SoundPathSpecifier("/Audio/Items/Defib/defib_failed.ogg");

    [DataField]
    public SoundSpecifier? SuccessSound = new SoundPathSpecifier("/Audio/Items/Defib/defib_success.ogg");

    [DataField]
    public SoundSpecifier? ReadySound = new SoundPathSpecifier("/Audio/Items/Defib/defib_ready.ogg");

    //Imp edit start, bools to control playing defib sounds
    //TODO IMP: remove these, all of the sound datafields are nullable, these shouldn't be needed

    [DataField]
    public bool PlayZapSound = true;

    [DataField]
    public bool PlayChargeSound = true;

    [DataField]
    public bool PlayFailureSound = true;

    [DataField]
    public bool PlaySuccessSound = true;

    [DataField]
    public bool PlayReadySound = true;

    //imp edit end
}

/// <summary>
/// DoAfterEvent for defibrilator use windup.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class DefibrillatorZapDoAfterEvent : SimpleDoAfterEvent;
