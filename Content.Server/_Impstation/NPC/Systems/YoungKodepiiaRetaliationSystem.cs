using Content.Server._Impstation.NPC.Components;
using Content.Shared.CombatMode;
using Content.Shared.Mobs.Components;
using Content.Shared.NPC.Components;
using Content.Shared.NPC.Systems;
using Robust.Shared.Collections;
using Robust.Shared.Timing;
using System.Linq;
using Content.Shared.Actions;
using Content.Shared.Interaction;
using Robust.Shared.Random;

namespace Content.Server._Impstation.NPC.Systems;

/// <summary>
///     Handles NPC which become aggressive after being interacted with.
///     Modified from NPCRetaliationSystem
/// </summary>
public sealed class YoungKodepiiaRetaliationSystem : EntitySystem
{
    [Dependency] private readonly NpcFactionSystem _npcFaction = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    /// <inheritdoc />
    public override void Initialize()
    {
        SubscribeLocalEvent<YoungKodepiiaRetaliationComponent, ActivateInWorldEvent>(OnActivate);
        SubscribeLocalEvent<YoungKodepiiaRetaliationComponent, AfterInteractEvent>(OnAfterInteract);
    }

    private void OnActivate(Entity<YoungKodepiiaRetaliationComponent> ent, ActivateInWorldEvent args)
    {
        TryRetaliate(ent, args.user);
    }

    private void OnAfterInteract(Entity<YoungKodepiiaRetaliationComponent> ent, AfterInteractEvent args)
    {
        TryRetaliate(ent, args.user);
    }

    public bool TryRetaliate(Entity<YoungKodepiiaRetaliationComponent> ent, EntityUid target)
    {
        // don't retaliate against inanimate objects.
        if (!HasComp<MobStateComponent>(target))
            return false;

        // don't retaliate against the same faction
        if (_npcFaction.IsEntityFriendly(ent.Owner, target))
            return false;

        _npcFaction.AggroEntity(ent.Owner, target);
        if (ent.Comp.AttackMemoryLength is {} memoryLength)
            ent.Comp.AttackMemories[target] = _timing.CurTime + memoryLength;

        return true;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<YoungKodepiiaRetaliationComponent, FactionExceptionComponent>();
        while (query.MoveNext(out var uid, out var retaliationComponent, out var factionException))
        {
            // TODO: can probably reuse this allocation and clear it
            foreach (var entity in new ValueList<EntityUid>(retaliationComponent.AttackMemories.Keys))
            {
                if (!TerminatingOrDeleted(entity) && _timing.CurTime < retaliationComponent.AttackMemories[entity])
                    continue;

                _npcFaction.DeAggroEntity((uid, factionException), entity);
                // TODO: should probably remove the AttackMemory, thats the whole point of the ValueList right??
            }
        }
    }
}
