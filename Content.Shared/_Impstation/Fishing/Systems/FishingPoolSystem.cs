using Content.Shared._Impstation.Fishing.Components;
using Content.Shared._Impstation.Fishing.Events;
using Content.Shared.Interaction;

namespace Content.Shared._Impstation.Fishing.Systems;

public sealed class FishingPoolSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<FishingPoolComponent, InteractUsingEvent>(OnInteractUsing);
    }

    private void OnInteractUsing(Entity<FishingPoolComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        var ev = new FishingAttemptEvent(ent);
        RaiseLocalEvent(args.Used, ev);
    }
}
