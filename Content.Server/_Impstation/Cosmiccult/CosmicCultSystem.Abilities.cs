using Content.Shared.DoAfter;
using Content.Shared.Damage;
using Robust.Shared.Prototypes;
using Content.Shared._Impstation.Cosmiccult.Components;
using Content.Shared._Impstation.Cosmiccult;
using Content.Shared.Actions;
using Content.Shared.IdentityManagement;
using Content.Shared.Mind;
using Content.Server.Objectives.Components;
using Content.Server.Destructible.Thresholds.Triggers;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Content.Server._Impstation.Cosmiccult;

public sealed partial class CosmicCultSystem : EntitySystem
{
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;

    public void SubscribeAbilities()
    {
        SubscribeLocalEvent<CosmicCultComponent, EventCosmicToolToggle>(OnCosmicToolToggle);
        SubscribeLocalEvent<CosmicCultComponent, EventCosmicSiphon>(OnCosmicSiphon);
        SubscribeLocalEvent<CosmicCultComponent, EventCosmicSiphonDoAfter>(OnCosmicSiphonDoAfter);
    }

    /// <summary>
    /// A 'lil catch-all thing to double check.. stuff. Called by multiple abilities.
    /// </summary>
    public bool TryUseAbility(EntityUid uid, CosmicCultComponent comp, BaseActionEvent action)
    {
        if (action.Handled)
            return false;

        if (!TryComp<CosmicCultActionComponent>(action.Action, out var cultAction))
            return false;

        action.Handled = true;
        return true;
    }



    /// <summary>
    /// Called when someone clicks on a target using the cosmic siphon ability.
    /// </summary>
    private void OnCosmicSiphon(EntityUid uid, CosmicCultComponent comp, ref EventCosmicSiphon args)
    {
        var target = args.Target;

        if (!TryUseAbility(uid, comp, args))
            return;

        var doargs = new DoAfterArgs(EntityManager, uid, comp.CosmicSiphonDuration, new EventCosmicSiphonDoAfter(), uid, args.Target)
        {
            DistanceThreshold = 1.5f,
            Hidden = true,
            BreakOnDamage = true,
            BreakOnMove = true,
            BreakOnDropItem = true,
        };
        _doAfter.TryStartDoAfter(doargs);
    }



    /// <summary>
    /// Called when CosmicSiphon's DoAfter completes.
    /// </summary>
    private void OnCosmicSiphonDoAfter(EntityUid uid, CosmicCultComponent comp, EventCosmicSiphonDoAfter args)
    {
        if (args.Args.Target == null)
            return;

        var target = args.Args.Target.Value;
        if (args.Cancelled || args.Handled)
            return;

        args.Handled = true;
        _damageable.TryChangeDamage(args.Target, comp.CosmicSiphonDamage, origin: uid);
        _popup.PopupEntity(Loc.GetString("cosmicability-siphon-success", ("target", Identity.Entity(target, EntityManager))), uid, uid);

        var entropymote1 = SpawnAtPosition(comp.CosmicSiphonResult, Transform(uid).Coordinates);
        _hands.TryForcePickupAnyHand(uid, entropymote1);

        // increment the greentext tracker and then set our tracker to what the greentext's tracker currently is
        IncrementCultObjectiveEntropy();
        if (_mind.TryGetObjectiveComp<CosmicEntropyConditionComponent>(uid, out var obj))
            obj.Siphoned = ObjectiveEntropyTracker;
    }



    /// <summary>
    /// Called when someone uses the Beckon Compass ability.
    /// </summary>
    private void OnCosmicToolToggle(EntityUid uid, CosmicCultComponent comp, ref EventCosmicToolToggle args)
    {

        if (!TryUseAbility(uid, comp, args))
            return;

        if (!TryToggleCosmicTool(uid, CultToolPrototype, comp))
            return;
    }



    /// <summary>
    /// Called by the Beckon Compass ability's OnCosmicToolToggle. Why are we nesting it like this? Fucked if i know.
    /// </summary>
    public bool TryToggleCosmicTool(EntityUid uid, EntProtoId proto, CosmicCultComponent comp)
    {
        if (!comp.Equipment.TryGetValue(proto.Id, out var item))
        {
            item = Spawn(proto, Transform(uid).Coordinates);
            if (!_hands.TryForcePickupAnyHand(uid, (EntityUid) item))
            {
                _popup.PopupEntity(Loc.GetString("cosmicability-toggle-error"), uid, uid);
                QueueDel(item);
                return false;
            }
            comp.Equipment.Add(proto.Id, item);
            return true;
        }

        QueueDel(item);
        // assuming that it exists
        comp.Equipment.Remove(proto.Id);

        return true;
    }



}
