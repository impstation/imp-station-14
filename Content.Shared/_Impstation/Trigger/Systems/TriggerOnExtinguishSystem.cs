using Content.Shared._Impstation.Trigger.Components.Triggers;
using Content.Shared.Atmos;
using Content.Shared.Trigger.Systems;

namespace Content.Shared._Impstation.Trigger.Systems;

public sealed class TriggerOnExtinguishSystem : EntitySystem
{
    [Dependency] private readonly TriggerSystem _trigger = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TriggerOnExtinguishComponent, ExtinguishedEvent>(OnExtinguishTriggered);
    }

    private void OnExtinguishTriggered(Entity<TriggerOnExtinguishComponent> ent, ref ExtinguishedEvent args)
    {
        _trigger.Trigger(ent);
    }
}
