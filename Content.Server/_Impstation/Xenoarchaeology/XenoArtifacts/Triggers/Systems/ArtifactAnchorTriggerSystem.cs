using Content.Server.Xenoarchaeology.XenoArtifacts.Triggers.Components;

namespace Content.Server.Xenoarchaeology.XenoArtifacts.Triggers.Systems;

public sealed class ArtifactAnchorTriggerSystem : EntitySystem
{
    [Dependency] private readonly ArtifactSystem _artifact = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<ArtifactAnchorTriggerComponent, AnchorStateChangedEvent>(OnAnchorStateChanged);
    }

    private void OnAnchorStateChanged(Entity<ArtifactAnchorTriggerComponent> ent, ref AnchorStateChangedEvent args)
    {
        if (args.Detaching)
            return;

        _artifact.TryActivateArtifact(ent);
    }
}