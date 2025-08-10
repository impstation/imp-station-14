using Content.Server.Xenoarchaeology.XenoArtifacts;
using Content.Server.Xenoarchaeology.Equipment.Components;
using Robust.Shared.Containers;

namespace Content.Server.Xenoarchaeology.Equipment.Systems;
public sealed class OldSuppressArtifactContainerSystem : EntitySystem
{
    [Dependency] private readonly ArtifactSystem _artifact = default!;
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<OldSuppressArtifactContainerComponent, EntInsertedIntoContainerMessage>(OnInserted);
        SubscribeLocalEvent<OldSuppressArtifactContainerComponent, EntRemovedFromContainerMessage>(OnRemoved);
    }

    private void OnInserted(EntityUid uid, OldSuppressArtifactContainerComponent component, EntInsertedIntoContainerMessage args)
    {
        if (!TryComp<ArtifactComponent>(args.Entity, out var artifact))
            return;
        _artifact.SetIsSuppressed(args.Entity, true, artifact);
    }

    private void OnRemoved(EntityUid uid, OldSuppressArtifactContainerComponent component, EntRemovedFromContainerMessage args)
    {
        if (!TryComp<ArtifactComponent>(args.Entity, out var artifact))
            return;
        _artifact.SetIsSuppressed(args.Entity, false, artifact);
    }
}
