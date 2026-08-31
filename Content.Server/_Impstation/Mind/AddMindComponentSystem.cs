using Content.Shared.Mind.Components;

namespace Content.Server._Impstation.Mind;

/// <summary>
/// Adds components to an entity's mind when they get one, deletes itself after.
/// </summary>
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
