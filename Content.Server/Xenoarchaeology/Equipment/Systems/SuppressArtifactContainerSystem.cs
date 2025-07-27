using Content.Server.Xenoarchaeology.XenoArtifacts; //#IMP
using Content.Server.Xenoarchaeology.Equipment.Components; //#IMP
using Content.Shared.Xenoarchaeology.Artifact;
using Content.Shared.Xenoarchaeology.Artifact.Components;
using Robust.Shared.Containers;

namespace Content.Server.Xenoarchaeology.Equipment.Systems;

//#IMP: Moved from Shared to Server for reason of allowing it to access old Xenoarch.
public sealed class SuppressArtifactContainerSystem : EntitySystem
{
    [Dependency] private readonly SharedXenoArtifactSystem _xenoArtifact = default!;
    [Dependency] private readonly ArtifactSystem _artifact = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<SuppressArtifactContainerComponent, EntInsertedIntoContainerMessage>(OnInserted);
        SubscribeLocalEvent<SuppressArtifactContainerComponent, EntRemovedFromContainerMessage>(OnRemoved);
    }

    private void OnInserted(EntityUid uid, SuppressArtifactContainerComponent component, EntInsertedIntoContainerMessage args)
    {
        if (TryComp<XenoArtifactComponent>(args.Entity, out var artifact)) //#IMP changed this from if (!trycomp)
            _xenoArtifact.SetSuppressed((args.Entity, artifact), true);
        if (TryComp<ArtifactComponent>(args.Entity, out var oldArtifact)) //#IMP
            _artifact.SetIsSuppressed(args.Entity, true, oldArtifact);
    }

    private void OnRemoved(EntityUid uid, SuppressArtifactContainerComponent component, EntRemovedFromContainerMessage args)
    {
        if (TryComp<XenoArtifactComponent>(args.Entity, out var artifact)) //#IMP changed this from if (!trycomp)
            _xenoArtifact.SetSuppressed((args.Entity, artifact), false);
        if (TryComp<ArtifactComponent>(args.Entity, out var oldArtifact)) //#IMP
            _artifact.SetIsSuppressed(args.Entity, false, oldArtifact);
    }
}
