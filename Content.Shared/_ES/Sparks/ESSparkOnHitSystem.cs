using Content.Shared._ES.Sparks.Components;
using Content.Shared.Damage.Systems;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared._ES.Sparks;

public sealed class ESSparkOnHitSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
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

        _sparks.DoSparks(ent, ent.Comp.Count, ent.Comp.SparkPrototype);
        ent.Comp.LastSparkTime = _timing.CurTime;
    }
}
