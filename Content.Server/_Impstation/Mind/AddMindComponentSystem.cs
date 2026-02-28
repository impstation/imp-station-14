using Content.Shared.Mind.Components;

namespace Content.Server._Impstation.Mind;

public sealed class AddMindComponentSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AddMindComponentComponent, MindAddedMessage>(OnMindAdded);
    }

    private void OnMindAdded(Entity<AddMindComponentComponent> ent, ref MindAddedMessage args)
    {
        var entMan = IoCManager.Resolve<IEntityManager>();
        entMan.AddComponents(args.Mind, ent.Comp.Components, removeExisting: ent.Comp.RemoveExisting);
        RemCompDeferred<AddMindComponentComponent>(ent);
    }
}
