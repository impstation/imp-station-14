using Content.Server.Xenoarchaeology.XenoArtifacts.Effects.Components;
using Content.Shared.Buckle.Components;
using Content.Server.Xenoarchaeology.XenoArtifacts.Events;
using Content.Shared.StatusEffect;
using Content.Server.Stunnable;
using Content.Server.Explosion.EntitySystems;

namespace Content.Server.Xenoarchaeology.XenoArtifacts.Effects.Systems;

public sealed class StunOnTriggerSystem : EntitySystem
{
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly StunSystem _stuns = default!;

    private EntityQuery<BuckleComponent> _buckleQuery;
    private EntityQuery<StatusEffectsComponent> _statusQuery;


    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<StunOnTriggerComponent, TriggerEvent>(OnActivated);
        _buckleQuery = GetEntityQuery<BuckleComponent>();
        _statusQuery = GetEntityQuery<StatusEffectsComponent>();

    }

    private void OnActivated(Entity<StunOnTriggerComponent> ent, ref TriggerEvent args)
    {
        var transform = Transform(ent);
        var gridUid = transform.GridUid;
        // knock over everyone on the same grid, fall back to range if not on a grid.
        if (ent.Comp.EntireGrid && gridUid != null) {
            HashSet<Entity<StatusEffectsComponent>> targets = new();
            _lookup.GetGridEntities<StatusEffectsComponent>((EntityUid)gridUid, targets);
            foreach (var target in targets)
            {
                if (_buckleQuery.TryGetComponent(target, out var buckle))
                {
                    if (buckle.Buckled)
                        continue;
                }

                if (!_statusQuery.TryGetComponent(target, out var status))
                    continue;

                _stuns.TryParalyze(target, TimeSpan.FromSeconds(ent.Comp.KnockdownTime), true, status);
            }

        }
        else // knock over only people in range
        {
            var targets = _lookup.GetEntitiesInRange(ent, ent.Comp.Range);

            foreach (var target in targets)
            {
                if (_buckleQuery.TryGetComponent(target, out var buckle))
                {
                    if (buckle.Buckled)
                        continue;
                }

                if (!_statusQuery.TryGetComponent(target, out var status))
                    continue;

                _stuns.TryParalyze(target, TimeSpan.FromSeconds(ent.Comp.KnockdownTime), true, status);
            }

        }
    }
}
