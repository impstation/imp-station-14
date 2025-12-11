using Content.Shared.Damage.Components;
using Content.Shared._EE.Item.ItemToggle.Components;

namespace Content.Shared._EE.Damage.Events;

/// <summary>
///   Raised on a throwing weapon when DamageOtherOnHit has been successfully initialized.
/// </summary>
public record struct DamageOtherOnHitStartupEvent(Entity<DamageOtherOnHitComponent> Weapon);

/// <summary>
///   Raised on a throwing weapon when ItemToggleDamageOtherOnHit has been successfully initialized.
/// </summary>
public record struct ItemToggleDamageOtherOnHitStartupEvent(Entity<ItemToggleDamageOtherOnHitComponent> Weapon);
