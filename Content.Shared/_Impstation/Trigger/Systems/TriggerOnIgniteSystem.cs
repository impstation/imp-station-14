using Content.Shared._Impstation.Trigger.Components.Triggers;
using Content.Shared.Atmos;
using Content.Shared.Projectiles;
using Content.Shared.Trigger.Systems;

namespace Content.Shared._Impstation.Trigger.Systems;

public sealed class TriggerOnIgniteSystem : EntitySystem
{
    [Dependency] private readonly TriggerSystem _trigger = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TriggerOnIgniteComponent, IgnitedEvent>(OnIgniteTriggered);
    }

    private void OnIgniteTriggered(Entity<TriggerOnIgniteComponent> ent, ref IgnitedEvent args)
    {
        _trigger.Trigger(ent);
    }
}
