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
using Content.Shared.Alert;
using Robust.Shared.Random;
using Robust.Shared.Audio.Systems;
using Content.Server.DoAfter;
using Content.Server.Chat.Systems;
using Content.Shared.Actions;
using Content.Server.Chat.Systems;

namespace Content.Server._Impstation.Cosmiccult;

public sealed partial class CosmicCultSystem : EntitySystem
{
    [Dependency] private readonly SharedEyeSystem _eye = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly IRobustRandom _rand = default!;
    [Dependency] private readonly ActionsSystem _actions = default!;
    [Dependency] private readonly AlertsSystem _alerts = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly SharedAudioSystem _aud = default!;
    [Dependency] private readonly DoAfterSystem _doafter = default!;
    [Dependency] private readonly ChatSystem _chat = default!;

    public EntProtoId CultToolPrototype = "AbilityCosmicCultTool";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CosmicCultComponent, ComponentInit>(OnCompInit);
        SubscribeLocalEvent<CosmicCultComponent, ComponentStartup>(OnStartup);
        // SubscribeLocalEvent<CosmicCultComponent, ComponentRemove>(OnComponentRemove);
        SubscribeLocalEvent<CosmicItemComponent, ExaminedEvent>(OnCosmicItemExamine);

        SubscribeAbilities();
    }

    // When the component initializes, we add the visibility mask for The Cult's Monument.
    private void OnCompInit(Entity<CosmicCultComponent> ent, ref ComponentInit args)
    {

        if (TryComp<EyeComponent>(ent, out var eye))
            _eye.SetVisibilityMask(ent, eye.VisibilityMask | CosmicMonumentComponent.LayerMask);
    }

    // When the entity with CosmicItemComponent is examined by an item with the CosmicCultComponent, append the contraband text.
    private void OnCosmicItemExamine(Entity<CosmicItemComponent> ent, ref ExaminedEvent args)
    {
        if (HasComp<CosmicCultComponent>(args.Examiner))
            return;

        args.PushMarkup(Loc.GetString("contraband-object-text-cosmiccult"));
    }

    // When the Component starts up, add the cosmic cult's abilities to the entity.
    private void OnStartup(EntityUid uid, CosmicCultComponent comp, ref ComponentStartup args)
    {
        // add actions
        foreach (var actionId in comp.BaseCosmicCultActions)
            _actions.AddAction(uid, actionId);
    }

    // Update the entropy values in the Alert UI.
    // private void UpdateEntropy(EntityUid uid, CosmicCultComponent comp, float? amount = null)
    // {
    //     comp.CurrentEntropy += amount ?? -1;
    //     comp.CurrentEntropy = Math.Clamp(comp.CurrentEntropy, 0, comp.MaxEntropy);
    //     Dirty(uid, comp);
    //     _alerts.ShowAlert(uid, "ChangelingBiomass");

    //     var random = (int) _rand.Next(1, 3);

    // }

    // Double check if the abillity's permissible before letting it go through.
    public bool TryUseAbility(EntityUid uid, CosmicCultComponent comp, BaseActionEvent action)
    {
        if (action.Handled)
            return false;

        if (!TryComp<CosmicCultActionComponent>(action.Action, out var cultAction))
            return false;

        // if (comp.Entropy < 1 && cultAction.RequireEntropy)
        // {
        //     _popup.PopupEntity(Loc.GetString("changeling-biomass-deficit"), uid, uid);
        //     return false;
        // }

        // UpdateEntropy(uid, comp, -cultAction.EntropyCost);

        action.Handled = true;
        return true;
    }
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

    // Try to siphon entropy from the target.
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

    // Try to toggle the item into the player's hands.

}
