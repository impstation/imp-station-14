using Content.Shared._EE.Damage.Events;
using Content.Shared._EE.Item.ItemToggle.Components;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Item.ItemToggle.Components;
using Content.Shared.Throwing;
using Content.Shared.Weapons.Melee;
using Robust.Shared.Audio;

namespace Content.Shared._EE.Damage.Systems;

/// <summary>
///     Handles setting up <see cref="DamageOtherOnHitComponent"/> inheritance from other components.
/// </summary>
/// <remarks>
///     The original EE file was named SharedDamageOtherOnHitSystem. Imp has renamed this file to EEThrowingSystem in
///     order to store component setup in a seperate namespace and keep the upstream file organized.
///     For actual throwing behaviour, see <see cref="SharedDamageOtherOnHitSystem"/>.
/// </remarks>
public sealed class EEThrowingSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DamageOtherOnHitComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<ItemToggleDamageOtherOnHitComponent, MapInitEvent>(OnItemToggleMapInit);
        SubscribeLocalEvent<DamageOtherOnHitComponent, ItemToggledEvent>(OnItemToggle);
    }

    /// <summary>
    ///   Inherit stats from MeleeWeapon.
    /// </summary>
    private void OnMapInit(Entity<DamageOtherOnHitComponent> ent, ref MapInitEvent _)
    {
        if (!TryComp<MeleeWeaponComponent>(ent, out var melee))
        {
            RaiseLocalEvent(ent, new DamageOtherOnHitStartupEvent(ent));
            return;
        }

        if (ent.Comp.Damage.Empty)
            ent.Comp.Damage = melee.Damage * ent.Comp.MeleeDamageMultiplier;

        ent.Comp.HitSound ??= melee.HitSound;

        if (ent.Comp.NoDamageSound == null)
        {
            if (melee.NoDamageSound != null)
                ent.Comp.NoDamageSound = melee.NoDamageSound;
            else
                ent.Comp.NoDamageSound = new SoundCollectionSpecifier("WeakHit");
        }

        RaiseLocalEvent(ent, new DamageOtherOnHitStartupEvent(ent));
        Dirty(ent);
    }

    /// <summary>
    ///   Inherit stats from ItemToggleMeleeWeaponComponent.
    /// </summary>
    private void OnItemToggleMapInit(Entity<ItemToggleDamageOtherOnHitComponent> ent, ref MapInitEvent _)
    {
        if (!TryComp<ItemToggleMeleeWeaponComponent>(ent, out var itemToggleMelee) ||
            !TryComp<DamageOtherOnHitComponent>(ent, out var damage))
            return;

        if (ent.Comp.ActivatedDamage == null &&
            itemToggleMelee.ActivatedDamage is { } activatedDamage)
            ent.Comp.ActivatedDamage = activatedDamage * damage.MeleeDamageMultiplier;

        ent.Comp.ActivatedHitSound ??= itemToggleMelee.ActivatedSoundOnHit;

        if (ent.Comp.ActivatedNoDamageSound == null &&
        itemToggleMelee.ActivatedSoundOnHitNoDamage is { } activatedSoundOnHitNoDamage)
            ent.Comp.ActivatedNoDamageSound = activatedSoundOnHitNoDamage;

        RaiseLocalEvent(ent, new ItemToggleDamageOtherOnHitStartupEvent(ent));
        Dirty(ent);
    }

    /// <summary>
    ///   Used to update the DamageOtherOnHit component on item toggle.
    /// </summary>
    private void OnItemToggle(Entity<DamageOtherOnHitComponent> ent, ref ItemToggledEvent args)
    {
        if (!TryComp<ItemToggleDamageOtherOnHitComponent>(ent, out var itemToggle))
            return;

        if (args.Activated)
        {
            if (itemToggle.ActivatedDamage is { } activatedDamage)
            {
                itemToggle.DeactivatedDamage ??= ent.Comp.Damage;
                ent.Comp.Damage = activatedDamage * ent.Comp.MeleeDamageMultiplier;
            }

            if (itemToggle.ActivatedStaminaCost is { } activatedStaminaCost)
            {
                itemToggle.DeactivatedStaminaCost ??= ent.Comp.StaminaCost;
                ent.Comp.StaminaCost = activatedStaminaCost;
            }

            itemToggle.DeactivatedHitSound ??= ent.Comp.HitSound;
            ent.Comp.HitSound = itemToggle.ActivatedHitSound;

            if (itemToggle.ActivatedNoDamageSound is { } activatedNoDamageSound)
            {
                itemToggle.DeactivatedNoDamageSound ??= ent.Comp.NoDamageSound;
                ent.Comp.NoDamageSound = activatedNoDamageSound;
            }
        }
        else
        {
            if (itemToggle.DeactivatedDamage is { } deactivatedDamage)
                ent.Comp.Damage = deactivatedDamage;

            if (itemToggle.DeactivatedStaminaCost is { } deactivatedStaminaCost)
                ent.Comp.StaminaCost = deactivatedStaminaCost;

            ent.Comp.HitSound = itemToggle.DeactivatedHitSound;

            if (itemToggle.DeactivatedNoDamageSound is { } deactivatedNoDamageSound)
                ent.Comp.NoDamageSound = deactivatedNoDamageSound;
        }
        Dirty(ent);
    }
}
