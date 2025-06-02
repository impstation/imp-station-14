using Content.Shared.Hands;

namespace Content.Shared._Impstation.Decapoids;

/// <summary>
/// Handles hands body parts spawning with an item held.
/// </summary>
public sealed class InnateHeldItemSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<InnateHeldItemComponent, HandAddedEvent_Imp>(OnHandAdded);
    }

    private void OnHandAdded(Entity<InnateHeldItemComponent> ent, ref HandAddedEvent_Imp args)
    {
        if (args.NewHand.Container != null)
            PredictedTrySpawnInContainer(ent.Comp.ItemPrototype, args.Receiver.Owner, args.NewHand.Container.ID, out _);
    }
}
