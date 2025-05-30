using Content.Shared._RMC14.Actions;
using Content.Shared._RMC14.Xenonids.Construction;
using Content.Shared._RMC14.Xenonids.Energy;
using Content.Shared._RMC14.Xenonids.Parasite;
using Content.Shared._RMC14.Xenonids.Plasma;
using Content.Shared._RMC14.Xenonids.Strain;
using Content.Shared._RMC14.Damage;
using Content.Shared._RMC14.Atmos;
using Content.Shared.Coordinates;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.FixedPoint;
using Content.Shared.Interaction;
using Content.Shared.Jittering;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Xenonids.Heal;

public abstract class SharedXenoHealSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly EntityLookupSystem _entityLookup = default!;
    [Dependency] private readonly SharedInteractionSystem _interact = default!;
    [Dependency] private readonly SharedJitteringSystem _jitter = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly MobThresholdSystem _mobThreshold = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly XenoPlasmaSystem _xenoPlasma = default!;
    [Dependency] private readonly XenoEnergySystem _xenoEnergy = default!;
    [Dependency] private readonly XenoStrainSystem _xenoStrain = default!;
    [Dependency] private readonly RMCActionsSystem _rmcActions = default!;
    [Dependency] private readonly SharedRMCDamageableSystem _rmcDamageable = default!;
    [Dependency] private readonly SharedRMCFlammableSystem _flammable = default!;

    private static readonly ProtoId<DamageGroupPrototype> BruteGroup = "Brute";
    private static readonly ProtoId<DamageGroupPrototype> BurnGroup = "Burn";
    private static readonly ProtoId<DamageTypePrototype> BluntGroup = "Blunt";

    private readonly HashSet<Entity<XenoComponent>> _xenos = new();

    public void Heal(EntityUid target, FixedPoint2 amount)
    {
        var damage = _rmcDamageable.DistributeHealing(target, BruteGroup, amount);
        var totalHeal = damage.GetTotal();
        var leftover = amount - totalHeal;
        if (leftover > FixedPoint2.Zero)
            damage = _rmcDamageable.DistributeHealing(target, BurnGroup, leftover, damage);
        _damageable.TryChangeDamage(target, -damage, true);
    }

    public void CreateHealStacks(EntityUid target, FixedPoint2 healAmount, TimeSpan timeBetweenHeals, int charges, TimeSpan nextHealAt, bool ignoreFire = false)
    {
        if (!ignoreFire && _flammable.IsOnFire(target))
            return;

        var heal = EnsureComp<XenoBeingHealedComponent>(target);
        var healStack = new XenoHealStack()
        {
            Charges = charges,
            TimeBetweenHeals = timeBetweenHeals,
        };

        healStack.HealAmount = healAmount;
        healStack.NextHealAt = _timing.CurTime + nextHealAt;
        heal.HealStacks.Add(healStack);
        heal.ParallizeHealing = true;
    }
    public override void Update(float frameTime)
    {
        var time = _timing.CurTime;
        var healQuery = EntityQueryEnumerator<XenoBeingHealedComponent>();
        while (healQuery.MoveNext(out var uid, out var heal))
        {
            if (heal.HealStacks.Count == 0 || _mobState.IsDead(uid))
            {
                RemCompDeferred<XenoBeingHealedComponent>(uid);
                continue;
            }

            List<XenoHealStack> finishedStacks = new();

            foreach (var healStack in heal.HealStacks)
            {
                if (healStack.Charges <= 0)
                {
                    finishedStacks.Add(healStack);
                    continue;
                }

                if (healStack.NextHealAt > time)
                {
                    continue;
                }

                Dirty(uid, heal);

                Heal(uid, healStack.HealAmount);

                healStack.NextHealAt = time + healStack.TimeBetweenHeals;
                healStack.Charges = healStack.Charges - 1;

                if (!heal.ParallizeHealing)
                {
                    break;
                }
            }

            foreach (var stack in finishedStacks)
            {
                heal.HealStacks.Remove(stack);
            }
        }
    }
}
