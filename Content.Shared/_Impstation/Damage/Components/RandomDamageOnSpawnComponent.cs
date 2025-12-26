using Content.Shared.Damage;
using Robust.Shared.GameStates;

namespace Content.Shared._Impstation.Damage.Components;

/// <summary>
/// Applies random damage to the entity when it spawns.
/// The damage is a flat addition.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RandomDamageOnSpawnComponent : Component
{

    [DataField(required: true), AutoNetworkedField]
    public DamageSpecifier Damage = default!;

    [DataField, AutoNetworkedField]
    public float MinDamage = 0f;

    [DataField, AutoNetworkedField]
    public float MaxDamage = 0f;

    [DataField, AutoNetworkedField]
    public bool IgnoreResistances = false;
}
