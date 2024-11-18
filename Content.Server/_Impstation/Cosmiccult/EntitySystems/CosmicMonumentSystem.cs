using System.Linq;
using Content.Shared.Interaction;
using Content.Server.Popups;
using Content.Shared._Impstation.Cosmiccult;
using Content.Server._Impstation.Cosmiccult.Components;
using Content.Shared._Impstation.Cosmiccult.Components;
using Robust.Shared.Prototypes;

namespace Content.Server._Impstation.Cosmiccult.EntitySystems;

public sealed class CosmicMonumentSystem : EntitySystem
{
    // [Dependency] private readonly IPrototypeManager _prototype = default!;

    // [Dependency] private readonly PopupSystem _popupSystem = default!;

    // public override void Initialize()
    // {
    //     base.Initialize();
    //     SubscribeLocalEvent<CosmicEntropyMoteComponent, AfterInteractEvent>(OnAfterInteract);
    //     SubscribeLocalEvent<CosmicEntropyMoteComponent, MapInitEvent>(OnMapInit);

    //     private void OnAfterInteract(EntityUid uid, CosmicEntropyMoteComponent component, AfterInteractEvent args)
    //     {
    //         if (!args.CanReach)
    //             return;

    //         if (!TryComp<CosmicMonumentComponent>(args.Target, out var server))
    //             return;

    //         _research.ModifyServerPoints(args.Target.Value, component.Points, server);
    //         _popupSystem.PopupEntity(Loc.GetString("research-disk-inserted", ("points", component.Points)), args.Target.Value, args.User);
    //         EntityManager.QueueDeleteEntity(uid);
    //         args.Handled = true;
    //     }

    //     private void OnMapInit(EntityUid uid, ResearchDiskComponent component, MapInitEvent args)
    //     {
    //         if (!component.UnlockAllTech)
    //             return;

    //         component.Points = _prototype.EnumeratePrototypes<TechnologyPrototype>()
    //             .Sum(tech => tech.Cost);
    //     }
    // }
}
