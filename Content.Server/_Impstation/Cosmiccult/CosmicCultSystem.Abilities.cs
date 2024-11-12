using Content.Shared.Chemistry.Components;
using Content.Shared.Cuffs.Components;
using Content.Shared.DoAfter;
using Content.Shared.FixedPoint;
using Content.Shared.IdentityManagement;
using Content.Shared.Mobs;
using Content.Shared.Store.Components;
using Content.Shared.Popups;
using Content.Shared.Damage;
using Robust.Shared.Prototypes;
using Content.Shared.Damage.Prototypes;
using Content.Server.Objectives.Components;
using Content.Server.Light.Components;
using Content.Shared.Eye.Blinding.Systems;
using Content.Shared.Eye.Blinding.Components;
using Content.Server.Flash.Components;
using Content.Shared.Movement.Pulling.Components;
using Content.Shared.Stealth.Components;
using Content.Shared.Damage.Components;
using Content.Server.Radio.Components;
using Content.Shared._Impstation.Cosmiccult.Components;
using Content.Shared._Impstation.Cosmiccult;

namespace Content.Server._Impstation.Cosmiccult;

public sealed partial class CosmicCultSystem : EntitySystem
{
    public void SubscribeAbilities()
    {
        SubscribeLocalEvent<CosmicCultComponent, CosmicToolEvent>(OnCosmicToggleTool);
    }

    // private void OnCosmicSiphon(EntityUid uid, CosmicCultComponent comp, ref CosmicSiphonEvent args)
    // {
    //     if (!TrySting(uid, comp, args, true))
    //         return;

    //     var target = args.Target;
    //     if (!TryStealDNA(uid, target, comp, true))
    //     {
    //         _popup.PopupEntity(Loc.GetString("changeling-sting-extract-fail"), uid, uid);
    //         // royal cashback
    //     }
    //     else _popup.PopupEntity(Loc.GetString("changeling-sting", ("target", Identity.Entity(target, EntityManager))), uid, uid);
    // }
    private void OnCosmicToggleTool(EntityUid uid, CosmicCultComponent comp, ref CosmicToolEvent args)
    {

        if (!TryUseAbility(uid, comp, args))
            return;

        if (!TryToggleItem(uid, CultToolPrototype, comp))
            return;
    }
}
