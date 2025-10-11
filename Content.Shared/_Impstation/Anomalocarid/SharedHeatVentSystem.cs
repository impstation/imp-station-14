using Content.Shared.Actions;
using Content.Shared.DoAfter;
using Robust.Shared.Serialization;
using Content.Shared.Popups;

namespace Content.Shared._Impstation.Anomalocarid;

public abstract class SharedHeatVentSystem : EntitySystem
{
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

    /// <summary>
    ///     Run when the VentAction is used.
    /// </summary>
    private void OnVentStart(Entity<HeatVentComponent> ent, ref HeatVentActionEvent args)
    {
        if (args.Handled)
            return;

        var doAfter = new DoAfterArgs(EntityManager,
            ent,
            Math.Clamp(ent.Comp.HeatStored * ent.Comp.VentLengthMultiplier, ent.Comp.VentLengthMin, ent.Comp.VentLengthMax),
            new HeatVentDoAfterEvent(),
            ent)
        {
            BlockDuplicate = true,
        };

        if (_doAfter.TryStartDoAfter(doAfter))
            _popup.PopupEntity(Loc.GetString(ent.Comp.VentStartPopup,  ("target", ent)), ent);

        args.Handled = true;
    }
}

/// <summary>
///     Relayed upon using heat vent action.
/// </summary>
public sealed partial class HeatVentActionEvent : InstantActionEvent;

/// <summary>
/// Is relayed after the doafter finishes.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class HeatVentDoAfterEvent : SimpleDoAfterEvent;
