using Content.Shared.Trigger;

namespace Content.Shared._ES.TileFires.Trigger;

public sealed class ESTileFireOnTriggerSystem : XOnTriggerSystem<ESTileFireOnTriggerComponent>
{
    [Dependency] private readonly ESSharedTileFireSystem _tileFire = default!;

    protected override void OnTrigger(Entity<ESTileFireOnTriggerComponent> ent, EntityUid target, ref TriggerEvent args)
    {
        args.Handled |= _tileFire.TryDoTileFire(ent.Owner, args.User, ent.Comp.Stage);
    }
}
