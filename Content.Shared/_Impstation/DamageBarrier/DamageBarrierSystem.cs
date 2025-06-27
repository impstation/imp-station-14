using Content.Shared.Damage;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;

namespace Content.Shared._Impstation.DamageBarrier;

public sealed partial class SharedDamageBarrierSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    // TODO: timer functionality

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DamageBarrierComponent, DamageModifyEvent>(OnDamaged);
    }

    public void ApplyDamageBarrier(EntityUid uid, DamageModifierSet damageModifier, float? time = null, SoundSpecifier? hitSound = null, SoundSpecifier? breakSound = null)
    {
        EnsureComp<DamageBarrierComponent>(uid, out var comp);

        if (time != null)
        {
            comp.Timer = true;
            comp.TimeRemaining = time.Value;
        }
        if (hitSound != null)
            comp.HitSound = hitSound;
        if (breakSound != null)
            comp.BreakSound = breakSound;

        comp.DamageModifier = damageModifier;
    }

    private void OnDamaged(Entity<DamageBarrierComponent> ent, ref DamageModifyEvent args)
    {
        if (!args.Damage.AnyPositive())
            return;
        // TODO: maybe we want to let through damage dealt by ourself. cant think of a use case for this rn

        // making a new modifier set which will affect our barrier.
        DamageModifierSet barrierModifier = new();

        // coefficients are fractional multipliers, so values need to rotate around 1.
        // we'll clamp at 0 so we dont end up accidentally healing our barrier.
        foreach (var (key, value) in ent.Comp.DamageModifier.Coefficients)
            barrierModifier.Coefficients.Add(key, Math.Max(1 - value, 0));

        // for flat reduction, we're just accumulating all these numbers to deal to our barrier at once.
        // but ONLY IF that damage type is in our args.
        var barrierDamageTotal = 0f;

        foreach (var (modKey, modValue) in ent.Comp.DamageModifier.FlatReduction)
            foreach (var (damKey, damValue) in args.Damage.DamageDict)
                if (damKey == modKey)
                    barrierDamageTotal += modValue;

        var barrierDamage = DamageSpecifier.ApplyModifierSet(args.Damage, barrierModifier);

        // actually deal damage to the barrier.
        barrierDamageTotal += barrierDamage.GetTotal().Float();
        if (barrierDamageTotal > 0)
        {
            ent.Comp.BarrierHealth -= barrierDamageTotal;
        }
        // we only play our hit sound if we dont need to play our break sound.
        if (barrierDamageTotal > 0 && ent.Comp.BarrierHealth > 0)
            _audio.PlayPvs(ent.Comp.HitSound, ent);
        else if (ent.Comp.BarrierHealth <= 0)
            OnBreak(ent);

        // and let through all the unabsorbed damage when we're done.
        args.Damage = DamageSpecifier.ApplyModifierSet(args.Damage, ent.Comp.DamageModifier);
        return;
    }

    private void OnBreak(Entity<DamageBarrierComponent> ent)
    {
        _audio.PlayPvs(ent.Comp.BreakSound, ent);
        RemComp<DamageBarrierComponent>(ent);
        var ev = new DamageBarrierBreakEvent();
        RaiseLocalEvent(ent, ref ev);
    }

}

/// <summary>
/// Event raised on an entity when an active damage barrier is broken.
/// </summary>
[ByRefEvent]
public record struct DamageBarrierBreakEvent;
