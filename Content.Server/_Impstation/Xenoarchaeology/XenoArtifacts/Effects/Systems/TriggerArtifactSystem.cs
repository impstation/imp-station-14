using Content.Server.Xenoarchaeology.XenoArtifacts.Effects.Components;
using Content.Server.Xenoarchaeology.XenoArtifacts.Events;
using Content.Shared.Trigger.Systems;

namespace Content.Server.Xenoarchaeology.XenoArtifacts.Effects.Systems;

/// <summary>
/// This handles <see cref="TriggerArtifactComponent"/>
/// </summary>
public sealed class TriggerArtifactSystem : EntitySystem
{
    [Dependency] private readonly TriggerSystem _trigger = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<TriggerArtifactComponent, ArtifactActivatedEvent>(OnArtifactActivated);
    }

    private void OnArtifactActivated(Entity<TriggerArtifactComponent> ent, ref ArtifactActivatedEvent args)
    {
        _trigger.Trigger(ent, args.Activator);
    }
}
