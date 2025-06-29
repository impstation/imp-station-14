using Content.Server.Xenoarchaeology.XenoArtifacts.Effects.Components;
using Content.Server.Xenoarchaeology.XenoArtifacts.Events;
using Content.Shared.Damage;
using Content.Shared.Whitelist;
using Robust.Shared.Random;

namespace Content.Server.Xenoarchaeology.XenoArtifacts.Effects.Systems;

public sealed class BreakWindowArtifactSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelistSystem = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<DamageNearbyArtifactComponent, ArtifactActivatedEvent>(OnActivated);
    }

    private void OnActivated(Entity<DamageNearbyArtifactComponent> ent, ref ArtifactActivatedEvent args)
    {
        var otherEnts = _lookup.GetEntitiesInRange(ent, ent.Comp.Radius);
        if (args.Activator != null)
            otherEnts.Add(args.Activator.Value);
        foreach (var other in otherEnts)
        {
            if (_whitelistSystem.IsWhitelistFail(ent.Comp.Whitelist, other))
                continue;

            if (!_random.Prob(ent.Comp.DamageChance))
                return;

            _damageable.TryChangeDamage(other, ent.Comp.Damage, ent.Comp.IgnoreResistances);
        }
    }
}