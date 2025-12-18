using Content.Shared._ES.Sparks.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Power.Components;
using Content.Shared.Power.EntitySystems;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared._ES.Sparks;

public sealed class ESSparkOnHitSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedPowerReceiverSystem _powerReceiver = default!;
    [Dependency] private readonly ESSparksSystem _sparks = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<ESSparkOnHitComponent, DamageChangedEvent>(OnDamaged);
    }

    private void OnDamaged(Entity<ESSparkOnHitComponent> ent, ref DamageChangedEvent args)
    {
        if (args.DamageDelta is null)
            return;

        if (!_random.Prob(ent.Comp.Prob))
            return;

        if (args.DamageDelta.GetTotal() < ent.Comp.Threshold)
            return;

        if (_timing.CurTime - ent.Comp.LastSparkTime < ent.Comp.SparkDelay)
            return;

        SharedApcPowerReceiverComponent? powerReceiver = null;
        if (_powerReceiver.ResolveApc(ent, ref powerReceiver) &&
            (!_powerReceiver.IsPowered((ent, powerReceiver)) || powerReceiver.Load <= 0))
            return;

        _sparks.DoSparks(ent, ent.Comp.Count, ent.Comp.SparkPrototype, ent.Comp.TileFireChance);
        ent.Comp.LastSparkTime = _timing.CurTime;
    }
}
