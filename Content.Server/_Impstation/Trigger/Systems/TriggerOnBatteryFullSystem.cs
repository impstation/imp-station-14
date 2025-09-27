using Content.Server._Impstation.Trigger.Components.Effects;
using Content.Server._Impstation.Trigger.Components.Triggers;
using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Shared.Trigger.Systems;

namespace Content.Server._Impstation.Trigger.Systems;

public sealed class TriggerOnBatteryFullSystem: EntitySystem
{
    [Dependency] private readonly TriggerSystem _trigger = default!;
    [Dependency] private readonly BatterySystem _battery = default!;
    public override void Initialize()
    {
        SubscribeLocalEvent<TriggerOnBatteryFullComponent, ChargeChangedEvent>(OnChargeChanged);
    }

    private void OnChargeChanged(Entity<TriggerOnBatteryFullComponent> ent, ref ChargeChangedEvent args)
    {
        if (TryComp(ent.Owner, out BatteryComponent? battery) && _battery.IsFull(ent, battery))
        {
            _trigger.Trigger(ent);
        }
    }
}
