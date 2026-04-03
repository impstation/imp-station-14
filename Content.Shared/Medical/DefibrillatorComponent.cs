using Content.Shared.Damage;
using Content.Shared.DoAfter;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Content.Shared.Whitelist; // imp

namespace Content.Shared.Medical;

/// <summary>
/// This is used for defibrillators; a machine that shocks a dead
/// person back into the world of the living.
/// Uses <c>ItemToggleComponent</c>
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class DefibrillatorComponent : Component
{
    /// <summary>
    /// How much damage is healed from getting zapped.
    /// </summary>
    [DataField("zapHeal", required: true), ViewVariables(VVAccess.ReadWrite)]
    public DamageSpecifier ZapHeal = default!;

    /// <summary>
    /// The electrical damage from getting zapped.
    /// </summary>
    [DataField("zapDamage"), ViewVariables(VVAccess.ReadWrite)]
    public int ZapDamage = 5;

    /// <summary>
    /// How long the victim will be electrocuted after getting zapped.
    /// </summary>
    [DataField("writheDuration"), ViewVariables(VVAccess.ReadWrite)]
    public float WritheDuration = 3f; // imp float

    /// <summary>
    ///     ID of the cooldown use delay.
    /// </summary>
    [DataField]
    public string DelayId = "defib-delay";

    /// <summary>
    ///     Cooldown after using the defibrillator.
    /// </summary>
    [DataField]
    public TimeSpan ZapDelay = TimeSpan.FromSeconds(5);

    /// <summary>
    /// How long the doafter for zapping someone takes
    /// </summary>
    /// <remarks>
    /// This is synced with the audio; do not change one but not the other.
    /// </remarks>
    [DataField("doAfterDuration"), ViewVariables(VVAccess.ReadWrite)]
    public float DoAfterDuration = 3f; // imp, move timespan -> float

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

    [DataField]
    public bool AllowDoAfterMovement = true;

    [DataField]
    public bool CanDefibCrit = true;

    // imp
    [DataField]
    public bool PlayZapSound = true;

    /// <summary>
    /// The sound when someone is zapped.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("zapSound")]
    public SoundSpecifier? ZapSound = new SoundPathSpecifier("/Audio/Items/Defib/defib_zap.ogg");

    // imp
    [DataField]
    public bool PlayChargeSound = true;

    [ViewVariables(VVAccess.ReadWrite), DataField("chargeSound")]
    public SoundSpecifier? ChargeSound = new SoundPathSpecifier("/Audio/Items/Defib/defib_charge.ogg");

    // imp
    [DataField]
    public bool PlayFailureSound = true;

    [ViewVariables(VVAccess.ReadWrite), DataField("failureSound")]
    public SoundSpecifier? FailureSound = new SoundPathSpecifier("/Audio/Items/Defib/defib_failed.ogg");

    // imp
    [DataField]
    public bool PlaySuccessSound = true;

    [ViewVariables(VVAccess.ReadWrite), DataField("successSound")]
    public SoundSpecifier? SuccessSound = new SoundPathSpecifier("/Audio/Items/Defib/defib_success.ogg");

    // imp
    [DataField]
    public bool PlayReadySound = true;

    [ViewVariables(VVAccess.ReadWrite), DataField("readySound")]
    public SoundSpecifier? ReadySound = new SoundPathSpecifier("/Audio/Items/Defib/defib_ready.ogg");
}

[Serializable, NetSerializable]
public sealed partial class DefibrillatorZapDoAfterEvent : SimpleDoAfterEvent
{

}
