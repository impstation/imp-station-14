using Content.Shared._Impstation.Trigger.Components.Triggers;
using Content.Shared.Projectiles;
using Content.Shared.Trigger.Systems;

namespace Content.Shared._Impstation.Trigger.Systems;

public sealed class TriggerOnEmbedSystem : EntitySystem
{

    [Dependency] private readonly TriggerSystem _trigger = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TriggerOnEmbedComponent, EmbedEvent>(OnEmbedTriggered);
    }

    private void OnEmbedTriggered(Entity<TriggerOnEmbedComponent> ent, ref EmbedEvent args)
    {
        _trigger.Trigger(ent, args.Embedded);
    }
}
