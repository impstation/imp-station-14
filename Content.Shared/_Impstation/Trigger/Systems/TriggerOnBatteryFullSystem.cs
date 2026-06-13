using Content.Shared._Impstation.Trigger.Components.Triggers;
using Content.Shared.Power;
using Content.Shared.Power.Components;
using Content.Shared.Power.EntitySystems;
using Content.Shared.Trigger.Systems;

namespace Content.Shared._Impstation.Trigger.Systems;

public sealed class TriggerOnBatteryFullSystem : EntitySystem
{
    [Dependency] private readonly SharedBatterySystem _battery = default!;
    [Dependency] private readonly TriggerSystem _trigger = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TriggerOnBatteryFullComponent, ChargeChangedEvent>(OnChargeChanged);
    }

    private void OnChargeChanged(Entity<TriggerOnBatteryFullComponent> ent, ref ChargeChangedEvent args)
    {
        if (TryComp(ent.Owner, out BatteryComponent? battery) && _battery.GetCharge((ent, battery)) >= battery.MaxCharge)
        {
            _trigger.Trigger(ent);
        }
    }
}
