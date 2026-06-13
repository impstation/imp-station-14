using Content.Shared.DoAfter;
using Content.Shared.EntityTable;
using Content.Shared.RatKing.Components;
using Content.Shared.Verbs;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;
using Robust.Shared.Serialization;
using Robust.Shared.Timing; // imp

namespace Content.Shared.RatKing.Systems;

public sealed class RummagerSystem : EntitySystem
{
    [Dependency] private readonly EntityTableSystem _entityTable =  default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!; // imp

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RummageableComponent, GetVerbsEvent<AlternativeVerb>>(OnGetVerb);
        SubscribeLocalEvent<RummageableComponent, RummageDoAfterEvent>(OnDoAfterComplete);
    }

    private void OnGetVerb(Entity<RummageableComponent> ent, ref GetVerbsEvent<AlternativeVerb> args)
    {

        if (!HasComp<RummagerComponent>(args.User) || ent.Comp.Looted && !ent.Comp.Relootable) // imp add relootable
            return;

        // IMP ADD: if the ent is relootable but not currently lootable, skip adding verbs
        if (!IsCurrentlyLootable(ent))
            return;

        var user = args.User;

        args.Verbs.Add(new AlternativeVerb
        {
            //Text = Loc.GetString("rat-king-rummage-text"),
            Text = ent.Comp.RummageVerb, // imp edit
            Priority = 0,
            Act = () =>
            {
                var rummageDuration = ent.Comp.RummageDuration * ent.Comp.RummageModifier; // imp add
                _doAfter.TryStartDoAfter(new DoAfterArgs(EntityManager,
                    user,
                    rummageDuration, // imp, was ent.comp.RummageDuration
                    new RummageDoAfterEvent(),
                    ent,
                    ent)
                {
                    BlockDuplicate = true,
                    BreakOnDamage = true,
                    BreakOnMove = true,
                    DistanceThreshold = 2f
                });
            }
        });
    }

    private void OnDoAfterComplete(Entity<RummageableComponent> ent, ref RummageDoAfterEvent args)
    {
        if (args.Cancelled || ent.Comp.Looted
            || !IsCurrentlyLootable(ent)) // imp add
            return;

        ent.Comp.Looted = true;

        // imp: set the next refresh if the entity is relootable.
        if (ent.Comp.Relootable)
            ent.Comp.NextRelootable = _gameTiming.CurTime + ent.Comp.RelootableCooldown;

        Dirty(ent, ent.Comp);
        _audio.PlayPredicted(ent.Comp.Sound, ent, args.User);

        if (_net.IsClient)
            return;

        // imp add: use rummager's loot settings if provided
        var table = TryComp<RummagerComponent>(args.User, out var rummager) && rummager.RummageLoot is not null
            ? rummager.RummageLoot
            : ent.Comp.Table;

        var spawns = _entityTable.GetSpawns(table); // imp: comp table -> var
        var coordinates = Transform(ent).Coordinates;

        foreach (var spawn in spawns)
        {
            Spawn(spawn, coordinates);
        }
    }

    // imp add
    /// Checks if the entity is currently lootable - Does not check if the entity has been looted.
    private bool IsCurrentlyLootable(Entity<RummageableComponent> entity)
    {
        // if the entity doesn't have relootable, return true. if the entity's relootable cooldown is up, return true. else return false
        return !entity.Comp.Relootable || entity.Comp.NextRelootable < _gameTiming.CurTime;
    }
}

/// <summary>
/// DoAfter event for rummaging through a container with RummageableComponent.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class RummageDoAfterEvent : SimpleDoAfterEvent;
