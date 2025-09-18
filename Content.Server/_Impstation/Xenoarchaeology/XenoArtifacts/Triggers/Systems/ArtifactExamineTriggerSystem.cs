using Content.Server.Xenoarchaeology.XenoArtifacts.Triggers.Components;
using Content.Shared.Examine;
using Content.Shared.Ghost;

namespace Content.Server.Xenoarchaeology.XenoArtifacts.Triggers.Systems;

public sealed class ArtifactExamineTriggerSystem : EntitySystem
{
    [Dependency] private readonly ArtifactSystem _artifact = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<ArtifactExamineTriggerComponent, ExaminedEvent>(OnExamine);
    }

    private void OnExamine(Entity<ArtifactExamineTriggerComponent> ent, ref ExaminedEvent args)
    {
        // Prevent ghosts from activating this trigger unless they have CanGhostInteract
        if (TryComp<GhostComponent>(args.Examiner, out var ghost) && !ghost.CanGhostInteract)
            return;

        if (ent.Comp.ExamineCountsAsInRange)
            _artifact.TryActivateArtifact(ent, args.Examiner);
        else
            _artifact.TryActivateArtifact(ent);
    }
}