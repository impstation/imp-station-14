using Content.Server._Impstation.Trigger.Components.Effects;
using Content.Shared.Buckle.Components;
using Content.Shared.StatusEffect;
using Content.Server.Stunnable;
using Content.Shared.Trigger;

namespace Content.Server._Impstation.Trigger.Systems;

public sealed class StunAreaOnTriggerSystem : EntitySystem
{
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly StunSystem _stuns = default!;

    private EntityQuery<BuckleComponent> _buckleQuery;
    private EntityQuery<StatusEffectsComponent> _statusQuery;


    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<StunAreaOnTriggerComponent, TriggerEvent>(OnActivated);
        _buckleQuery = GetEntityQuery<BuckleComponent>();
        _statusQuery = GetEntityQuery<StatusEffectsComponent>();

    }

    private void OnActivated(Entity<StunAreaOnTriggerComponent> ent, ref TriggerEvent args)
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

                _stuns.TryUpdateParalyzeDuration(target, TimeSpan.FromSeconds(ent.Comp.KnockdownTime));
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

                _stuns.TryUpdateParalyzeDuration(target, TimeSpan.FromSeconds(ent.Comp.KnockdownTime));
            }

        }
    }
}
