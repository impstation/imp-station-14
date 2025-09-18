using Content.Server.Xenoarchaeology.XenoArtifacts.Triggers.Components;
using Content.Shared.Damage;

namespace Content.Server.Xenoarchaeology.XenoArtifacts.Triggers.Systems;

public sealed class ArtifactDamageTriggerSystem : EntitySystem
{
    [Dependency] private readonly ArtifactSystem _artifact = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<ArtifactDamageTriggerComponent, DamageChangedEvent>(OnDamageChanged);
    }

    private void OnDamageChanged(Entity<ArtifactDamageTriggerComponent> ent, ref DamageChangedEvent args)
    {
        if (!args.DamageIncreased)
            return;

        if (args.DamageDelta == null)
            return;

        foreach (var (type, amount) in args.DamageDelta.DamageDict)
        {
            if (ent.Comp.DamageTypes != null && !ent.Comp.DamageTypes.Contains(type))
                continue;

            ent.Comp.AccumulatedDamage += (float) amount;
        }

        if (ent.Comp.AccumulatedDamage >= ent.Comp.DamageThreshold)
            _artifact.TryActivateArtifact(ent, args.Origin);
    }
}