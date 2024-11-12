using Content.Server.Popups;
using Content.Server._Impstation.Cosmiccult.Components;
using Content.Shared._Impstation.Cosmiccult;
using Content.Shared._Impstation.Cosmiccult.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Popups;
using Content.Shared.Mind;
using Content.Shared.Examine;
using Content.Shared.Silicons.Borgs.Components;
using Content.Server.Actions;
using Robust.Shared.Prototypes;
using Content.Shared.Actions;

namespace Content.Server._Impstation.Cosmiccult;

public sealed partial class CosmicCultSystem : EntitySystem
{
    [Dependency] private readonly SharedEyeSystem _eye = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly ActionsSystem _actions = default!;


    public EntProtoId CultToolPrototype = "AbilityCosmicCultAstrolabe";


    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CosmicCultComponent, ComponentInit>(OnCompInit);
        SubscribeLocalEvent<CosmicCultComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<CosmicItemComponent, ExaminedEvent>(OnCosmicItemExamine);
    }

    private void OnCompInit(Entity<CosmicCultComponent> ent, ref ComponentInit args)
    {
        // add monument visibility layer
        if (TryComp<EyeComponent>(ent, out var eye))
            _eye.SetVisibilityMask(ent, eye.VisibilityMask | CosmicMonumentComponent.LayerMask);
    }

    private void OnCosmicItemExamine(Entity<CosmicItemComponent> ent, ref ExaminedEvent args)
    {
        if (HasComp<CosmicCultComponent>(args.Examiner))
            return;

        args.PushMarkup(Loc.GetString("contraband-object-text-cosmiccult"));
    }
    private void OnStartup(EntityUid uid, CosmicCultComponent comp, ref ComponentStartup args)
    {
        // add actions
        foreach (var actionId in comp.BaseCosmicCultActions)
            _actions.AddAction(uid, actionId);
    }

    public bool TryUseAbility(EntityUid uid, CosmicCultComponent comp, BaseActionEvent action)
    {
        if (action.Handled)
            return false;

        action.Handled = true;

        return true;
    }

    // public bool TrySiphon(EntityUid uid, CosmicCultComponent comp, EntityTargetActionEvent action, bool overrideMessage = false)
    // {
    //     if (!TryUseAbility(uid, comp, action))
    //         return false;

    //     var target = action.Target;

    //     // can't get his dna if he doesn't have it!
    //     if (!HasComp<AbsorbableComponent>(target) || HasComp<BorgBrainComponent>(target))
    //     {
    //         _popup.PopupEntity(Loc.GetString("changeling-sting-extract-fail"), uid, uid);
    //         return false;
    //     }

    //     if (HasComp<CosmicCultComponent>(target))
    //     {
    //         _popup.PopupEntity(Loc.GetString("changeling-sting-fail-self", ("target", Identity.Entity(target, EntityManager))), uid, uid);
    //         _popup.PopupEntity(Loc.GetString("changeling-sting-fail-ling"), target, target);
    //         return false;
    //     }
    //     if (!overrideMessage)
    //         _popup.PopupEntity(Loc.GetString("changeling-sting", ("target", Identity.Entity(target, EntityManager))), uid, uid);
    //     return true;
    // }

    public bool TryToggleItem(EntityUid uid, EntProtoId proto, CosmicCultComponent comp)
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
