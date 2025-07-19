using Content.Server.Xenoarchaeology.XenoArtifacts.Effects.Components;
using Content.Server.Xenoarchaeology.XenoArtifacts.Events;
using Content.Shared.Magic.Events;

namespace Content.Server.Xenoarchaeology.XenoArtifacts.Effects.Systems;

public sealed class KnockArtifactSystem : EntitySystem
{
    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<KnockArtifactComponent, ArtifactActivatedEvent>(OnActivated);
    }

    private void OnActivated(Entity<KnockArtifactComponent> ent, ref ArtifactActivatedEvent args)
    {
        var ev = new KnockSpellEvent
        {
            Performer = ent,
            Range = ent.Comp.KnockRange
        };
        RaiseLocalEvent(ev);
    }
}