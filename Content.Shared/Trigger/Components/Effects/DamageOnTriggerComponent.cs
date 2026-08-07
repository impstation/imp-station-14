using Content.Shared.Damage;
using Robust.Shared.GameStates;

namespace Content.Shared.Trigger.Components.Effects;

/// <summary>
/// Will damage an entity when triggered.
/// If TargetUser is true it the user will take damage instead.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class DamageOnTriggerComponent : BaseXOnTriggerComponent
{

    /// <summary>
    /// Imp add - Damage entity containing this entity instead (for example for wearable clothing).
    /// Has priority over TargetUser.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool TargetContainer;

    /// <summary>
    /// Should the damage ignore resistances?
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool IgnoreResistances;

    /// <summary>
    /// The base damage amount that is dealt.
    /// May be further modified by <see cref="Systems.BeforeDamageOnTriggerEvent"/> subscriptions.
    /// </summary>
    [DataField(required: true), AutoNetworkedField]
    public DamageSpecifier Damage = default!;
}
