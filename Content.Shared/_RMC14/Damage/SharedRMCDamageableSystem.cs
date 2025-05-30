using System.Diagnostics.CodeAnalysis;
using Content.Shared._RMC14.Map;
using Content.Shared._RMC14.Xenonids.Construction.Nest;
using Content.Shared._RMC14.Xenonids.Devour;
using Content.Shared._RMC14.Xenonids.Parasite;
using Content.Shared.Armor;
using Content.Shared.Blocking;
using Content.Shared.Chat.Prototypes;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.FixedPoint;
using Content.Shared.Inventory;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.Projectiles;
using Content.Shared.Silicons.Borgs;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Whitelist;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;
using Robust.Shared.Physics.Events;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Damage;

public abstract class SharedRMCDamageableSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly EntityWhitelistSystem _entityWhitelist = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly MobThresholdSystem _mobThresholds = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly IPrototypeManager _prototypes = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly RMCMapSystem _rmcMap = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    private static readonly ProtoId<DamageGroupPrototype> BruteGroup = "Brute";
    private static readonly ProtoId<DamageGroupPrototype> BurnGroup = "Burn";

    private static readonly ProtoId<DamageTypePrototype> LethalDamageType = "Asphyxiation";
    private readonly HashSet<ProtoId<DamageTypePrototype>> _bruteTypes = new();
    private readonly HashSet<ProtoId<DamageTypePrototype>> _burnTypes = new();
    private readonly List<string> _types = [];

    private EntityQuery<DamageableComponent> _damageableQuery;
    private EntityQuery<MobStateComponent> _mobStateQuery;
    private EntityQuery<VictimInfectedComponent> _victimInfectedQuery;
    private EntityQuery<XenoNestedComponent> _xenoNestedQuery;

    public override void Initialize()
    {
        _damageableQuery = GetEntityQuery<DamageableComponent>();
        _mobStateQuery = GetEntityQuery<MobStateComponent>();
        _victimInfectedQuery = GetEntityQuery<VictimInfectedComponent>();
        _xenoNestedQuery = GetEntityQuery<XenoNestedComponent>();

        _bruteTypes.Clear();
        _burnTypes.Clear();

        if (_prototypes.TryIndex(BruteGroup, out var bruteProto))
        {
            foreach (var type in bruteProto.DamageTypes)
            {
                _bruteTypes.Add(type);
            }
        }

        if (_prototypes.TryIndex(BurnGroup, out var burnProto))
        {
            foreach (var type in burnProto.DamageTypes)
            {
                _burnTypes.Add(type);
            }
        }
    }

    public DamageSpecifier DistributeHealing(Entity<DamageableComponent?> damageable, ProtoId<DamageGroupPrototype> groupId, FixedPoint2 amount, DamageSpecifier? equal = null)
    {
        equal ??= new DamageSpecifier();
        if (!_damageableQuery.Resolve(damageable, ref damageable.Comp, false))
            return equal;

        if (!_prototypes.TryIndex(groupId, out var group))
            return equal;

        _types.Clear();
        foreach (var type in group.DamageTypes)
        {
            if (damageable.Comp.Damage.DamageDict.TryGetValue(type, out var current) &&
                current > FixedPoint2.Zero)
            {
                _types.Add(type);
            }
        }

        var damage = equal.DamageDict;
        var add = amount > FixedPoint2.Zero;
        var left = amount;
        while (add ? left > 0 : left < 0)
        {
            var lastLeft = left;
            for (var i = _types.Count - 1; i >= 0; i--)
            {
                var type = _types[i];
                var current = damageable.Comp.Damage.DamageDict[type];

                var existingHeal = add ? -damage.GetValueOrDefault(type) : damage.GetValueOrDefault(type);
                left += existingHeal;
                var toDamage = add
                    ? FixedPoint2.Min(existingHeal + left / (i + 1), current)
                    : -FixedPoint2.Min(-(existingHeal + left / (i + 1)), current);
                if (current <= FixedPoint2.Abs(toDamage))
                    _types.RemoveAt(i);

                damage[type] = toDamage;
                left -= toDamage;
            }

            if (lastLeft == left)
                break;
        }

        return equal;
    }

    public DamageSpecifier DistributeTypes(Entity<DamageableComponent?> damageable, FixedPoint2 amount, DamageSpecifier? equal = null)
    {
        foreach (var group in _prototypes.EnumeratePrototypes<DamageGroupPrototype>())
        {
            equal = DistributeHealing(damageable, group.ID, amount, equal);
        }

        return equal ?? new DamageSpecifier();
    }

    public DamageSpecifier DistributeTypesTotal(Entity<DamageableComponent?> damageable, FixedPoint2 amount, DamageSpecifier? equal = null)
    {
        foreach (var group in _prototypes.EnumeratePrototypes<DamageGroupPrototype>())
        {
            var total = equal?.GetTotal() ?? FixedPoint2.Zero;
            var left = amount - total;
            if (left <= FixedPoint2.Zero)
            {
                break;
            }

            equal = DistributeHealing(damageable, group.ID, left, equal);
            amount = left;
        }

        return equal ?? new DamageSpecifier();
    }

    protected virtual void DoEmote(EntityUid ent, ProtoId<EmotePrototype> emote)
    {
    }

    public virtual bool TryGetDestroyedAt(EntityUid destructible, [NotNullWhen(true)] out FixedPoint2? destroyed)
    {
        // TODO RMC14
        destroyed = default;
        return false;
    }
}
