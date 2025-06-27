using Content.Shared.Damage;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared._Impstation.DamageBarrier;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class DamageBarrierComponent : Component
{
    /// <summary>
    /// Amount of health the barrier has.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float BarrierHealth = 30f;

    /// <summary>
    /// Damage modifier to be applied to the entity with an active barrier.
    /// All damage absorbed will deduct points from the barrier health until the barrier breaks.
    /// Damage that is not defined in the modifier will go through the barrier and damage the entity.
    /// </summary>
    [DataField]
    public DamageModifierSet DamageModifier = new(); // in an ideal world this would be every damage type by default

    /// <summary>
    /// Optional timer if the barrier should break on its own after a certain amount of time has elapsed.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public float TimeRemaining;
    public bool Timer = false;

    /// <summary>
    /// Sound the barrier makes when hit.
    /// </summary>
    [DataField]
    public SoundSpecifier HitSound = new SoundPathSpecifier("/Audio/Weapons/block_metal1.ogg")
    {
        Params = AudioParams.Default.WithVolume(-3f).WithVariation(3f)
    };

    /// <summary>
    /// Sound the barrier makes when broken
    /// </summary>
    [DataField]
    public SoundSpecifier BreakSound = new SoundPathSpecifier("/Audio/Effects/metal_break1.ogg")
    {
        Params = AudioParams.Default.WithVolume(-3f).WithVariation(3f)
    };
}
