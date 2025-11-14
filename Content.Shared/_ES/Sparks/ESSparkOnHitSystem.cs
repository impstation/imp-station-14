using Content.Shared._ES.Sparks.Components;
using Content.Shared.Damage.Systems;

namespace Content.Shared._ES.Sparks;

public sealed class ESSparkOnHitSystem : EntitySystem
{
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

        if (args.DamageDelta.GetTotal() < ent.Comp.Threshold)
            return;

        _sparks.DoSparks(ent, ent.Comp.Count, ent.Comp.SparkPrototype);
    }
}
