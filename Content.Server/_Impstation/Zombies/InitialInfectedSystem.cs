using Content.Server.Store.Systems;
using Content.Shared.Actions;
using Content.Shared.Administration.Systems;
using Content.Shared.Destructible;
using Content.Shared.Mobs.Systems;
using Content.Shared.Movement.Components;
using Content.Shared.PAI;
using Content.Shared.Store.Components;
using Content.Shared.Zombies;
using YamlDotNet.Core.Tokens;

namespace Content.Server.Zombies;

public sealed partial class InitialInfectedSystem : SharedInitialInfectedSystem
{
    [Dependency] private readonly StoreSystem _store = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly RejuvenateSystem _rejuvenate = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;



    public override void Initialize()
    {
        SubscribeLocalEvent<InitialInfectedComponent, MutationShopActionEvent>(OnShop);
        SubscribeLocalEvent<InitialInfectedComponent, MutateLungeEvent>(OnMutateLunge);
        SubscribeLocalEvent<InitialInfectedComponent, MutateUnstoppableEvent>(OnMutateUnstoppable);
        SubscribeLocalEvent<InitialInfectedComponent, UnstoppableActionEvent>(OnUseRevive);
    }

    private void OnShop(Entity<InitialInfectedComponent> ent, ref MutationShopActionEvent args)
    {
        if (!TryComp<StoreComponent>(ent, out var store))
            return;

        _store.ToggleUi(args.Performer, ent, store);
    }

    private void OnMutateLunge(Entity<InitialInfectedComponent> ent, ref MutateLungeEvent args)
    {
        //why is there not a thing in the listing prototype that's just "add this component"
        //am i going to code that? no
        var jump = EnsureComp<JumpAbilityComponent>(ent);
        _actions.RemoveAction(jump.ActionEntity); //gotta do this because adding jumpability adds a SECOND jump action but we need it for the FIRST one to WORK
    }

    private void OnMutateUnstoppable(Entity<InitialInfectedComponent> ent, ref MutateUnstoppableEvent args)
    {
        ent.Comp.RemainingRevives = 1;
    }

    private void OnUseRevive(Entity<InitialInfectedComponent> ent, ref UnstoppableActionEvent args)
    {
        if(ent.Comp.RemainingRevives <= 0 || _mobState.IsAlive(ent))
        {
            return;
        }
        ent.Comp.RemainingRevives--;
        _rejuvenate.PerformRejuvenate(ent);
    }
}
