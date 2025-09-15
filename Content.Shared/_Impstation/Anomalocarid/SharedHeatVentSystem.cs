using Content.Shared.Actions;
using Content.Shared.Damage;
using Content.Shared.DoAfter;
using Robust.Shared.Timing;
using Robust.Shared.Serialization;
using Content.Shared.Popups;

namespace Content.Shared._Impstation.Anomalocarid;

public abstract partial class SharedHeatVentSystem : EntitySystem
{
    [Dependency] private readonly DamageableSystem _damage = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HeatVentComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<HeatVentComponent, HeatVentActionEvent>(OnVentStart);
    }

    private void OnStartup(Entity<HeatVentComponent> ent, ref ComponentStartup args)
    {
        _actions.AddAction(ent, ent.Comp.VentAction);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<HeatVentComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (_timing.CurTime < comp.UpdateTimer)
                continue;

            comp.UpdateTimer = _timing.CurTime + TimeSpan.FromSeconds(comp.UpdateCooldown);

            Cycle((uid, comp));
        }
    }

    private void Cycle(Entity<HeatVentComponent> ent)
    {
        ent.Comp.HeatStored += ent.Comp.HeatAdded;

        if (ent.Comp.HeatStored >= ent.Comp.MaxHeat)
            _damage.TryChangeDamage(ent, ent.Comp.HeatDamage, ignoreResistances: true, interruptsDoAfters: false);

        // TODO: maybe include a popup when heat has gone beyond a certain threshold?
        // status effect icon maybe?
    }

    /// <summary>
    ///     Run when the VentAction is used.
    /// </summary>
    private void OnVentStart(Entity<HeatVentComponent> ent, ref HeatVentActionEvent args)
    {
        var doAfter = new DoAfterArgs(EntityManager, ent, ent.Comp.HeatStored * ent.Comp.VentLengthMultiplier, new HeatVentDoAfterEvent(), ent)
        {
            BlockDuplicate = true,
            CancelDuplicate = true,
        };

        if (_doAfter.TryStartDoAfter(doAfter))
            _popup.PopupEntity(ent.Comp.VentStartPopup, ent);
        // TODO: should this popup only show for the client?
    }
}

/// <summary>
///     Relayed upon using heat vent action.
/// </summary>
public sealed partial class HeatVentActionEvent : InstantActionEvent { }

/// <summary>
/// Is relayed after the doafter finishes.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class HeatVentDoAfterEvent : SimpleDoAfterEvent { }
