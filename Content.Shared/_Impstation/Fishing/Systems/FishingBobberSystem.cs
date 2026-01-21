using Content.Shared._Impstation.Fishing.Components;
using Content.Shared.Item;

namespace Content.Shared._Impstation.Fishing.Systems;

public sealed class FishingBobberSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<FishingBobberComponent, GettingPickedUpAttemptEvent>(OnPickupAttempt);
    }

    private static void OnPickupAttempt(Entity<FishingBobberComponent> ent, ref GettingPickedUpAttemptEvent args)
    {
        if (args.Cancelled || ent.Comp.Parent == null || ent.Comp.Reeled)
            return;

        args.Cancel();
    }
}
