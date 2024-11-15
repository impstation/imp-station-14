using Content.Shared.DoAfter;
using Content.Shared.Damage;
using Robust.Shared.Prototypes;
using Content.Shared._Impstation.Cosmiccult.Components;
using Content.Shared._Impstation.Cosmiccult;
using Content.Shared.Actions;

namespace Content.Server._Impstation.Cosmiccult;

public sealed partial class CosmicCultSystem : EntitySystem
{
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;

    public void SubscribeAbilities()
    {
        SubscribeLocalEvent<CosmicCultComponent, EventCosmicToolToggle>(OnCosmicToolToggle);
        SubscribeLocalEvent<CosmicCultComponent, CosmicSiphonEvent>(OnCosmicSiphon);
        SubscribeLocalEvent<CosmicCultComponent, CosmicSiphonDoAfterEvent>(OnCosmicSiphonDoAfter);
    }

    private void OnCosmicSiphon(EntityUid uid, CosmicCultComponent comp, ref CosmicSiphonEvent args)
    {
        if (args.Handled)
            return;
        args.Handled = true;

        var doargs = new DoAfterArgs(EntityManager, uid, comp.CosmicSiphonDuration, new CosmicSiphonDoAfterEvent(), uid, args.Target)
        {
            Hidden = true,
            BreakOnDamage = true,
            BreakOnMove = true,
            BreakOnDropItem = true,
        };

        _doAfter.TryStartDoAfter(doargs);
    }

    private void OnCosmicSiphonDoAfter(EntityUid uid, CosmicCultComponent comp, CosmicSiphonDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled)
            return;

        args.Handled = true;

        _damageable.TryChangeDamage(args.Target, comp.CosmicSiphonDamage, origin: uid);

        var money = SpawnAtPosition(comp.CosmicSiphonResult, Transform(uid).Coordinates);
        _hands.TryForcePickupAnyHand(uid, money);
    }

    public bool TryUseAbility(EntityUid uid, CosmicCultComponent comp, BaseActionEvent action)
    {
        if (action.Handled)
            return false;

        if (!TryComp<CosmicCultActionComponent>(action.Action, out var cultAction))
            return false;

        action.Handled = true;
        return true;
    }

    private void OnCosmicToolToggle(EntityUid uid, CosmicCultComponent comp, ref EventCosmicToolToggle args)
    {

        if (!TryUseAbility(uid, comp, args))
            return;

        if (!TryToggleCosmicTool(uid, CultToolPrototype, comp))
            return;
    }
    public bool TryToggleCosmicTool(EntityUid uid, EntProtoId proto, CosmicCultComponent comp)
    {
        if (!comp.Equipment.TryGetValue(proto.Id, out var item))
        {
            item = Spawn(proto, Transform(uid).Coordinates);
            if (!_hands.TryForcePickupAnyHand(uid, (EntityUid) item))
            {
                _popup.PopupEntity(Loc.GetString("changeling-fail-hands"), uid, uid);
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
