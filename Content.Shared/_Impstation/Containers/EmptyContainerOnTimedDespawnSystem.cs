using Robust.Shared.Containers;
using Robust.Shared.Spawners;

namespace Content.Shared._Impstation.Containers;

public sealed class EmptyContainerOnTimedDespawnSystem : EntitySystem
{
    [Dependency] private readonly SharedContainerSystem _container = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<EmptyContainerOnTimedDespawnComponent, TimedDespawnEvent>(OnTimedDespawn);
    }

    private void OnTimedDespawn(Entity<EmptyContainerOnTimedDespawnComponent> ent, ref TimedDespawnEvent args)
    {
        if (!_container.TryGetContainer(ent, ent.Comp.ContainerId, out var container))
            return;

        _container.EmptyContainer(container, true);
    }
}
