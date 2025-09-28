using Content.Server.Ninja.Events;
using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared._Impstation.Eeep.Components;
using Content.Shared._Impstation.Eeep.Systems;
using Content.Shared.Popups;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Content.Shared.Whitelist;
using Content.Server.PowerCell;
using Content.Shared.PowerCell.Components;


namespace Content.Server._Impstation.Eeep.Systems;

/// <summary>
/// Handles the doafter and power transfer when draining.
/// </summary>
public sealed class BatteryDrainerEeepSystem : SharedBatteryDrainerEeepSystem
{
    [Dependency] private readonly BatterySystem _battery = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;
    [Dependency] private readonly PowerCellSystem _powerCell = default!;
    [Dependency] private readonly ApcComponent _apcSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BatteryDrainerEeepComponent, ComponentStartup>(OnStartup); // imp add
        SubscribeLocalEvent<BatteryDrainerEeepComponent, BeforeInteractHandEvent>(OnBeforeInteractHand);
        SubscribeLocalEvent<BatteryDrainerEeepComponent, NinjaBatteryChangedEvent>(OnBatteryChanged);
    }

    // imp add
    /// <summary>
    ///     Allow entities who are a battery to use themselves as the battery for this component
    /// </summary>
    private void OnStartup(Entity<BatteryDrainerEeepComponent> ent, ref ComponentStartup args)
    {
        if (ent.Comp.BatteryUid == null && TryComp<BatteryComponent>(ent.Owner, out _))
            ent.Comp.BatteryUid = ent.Owner;
    }

    /// <summary>
    /// Start do after for draining a power source.
    /// Can't predict PNBC existing so only done on server.
    /// </summary>
    private void OnBeforeInteractHand(Entity<BatteryDrainerEeepComponent> ent, ApcComponent component, ref BeforeInteractHandEvent args)
    {
        var (uid, comp) = ent;
        var target = args.Target;
        if (args.Handled || comp.BatteryUid is not { } battery || !HasComp<PowerNetworkBatteryComponent>(target))
            return;
        // check whitelist
        if (_whitelist.IsWhitelistFail(component.Whitelist))
            return;
        // handles even if battery is full so you can actually see the poup
        args.Handled = true;

        if (_battery.IsFull(battery))
        {
            _popup.PopupEntity(Loc.GetString("battery-drainer-full"), uid, uid, PopupType.Medium);
            return;
        }

        var doAfterArgs = new DoAfterArgs(EntityManager, uid, comp.DrainTime, new DrainDoAfterEvent(), target: target, eventTarget: uid)
        {
            MovementThreshold = 0.5f,
            BreakOnMove = true,
            CancelDuplicate = false,
            AttemptFrequency = AttemptFrequency.StartAndEnd
        };

        _doAfter.TryStartDoAfter(doAfterArgs);
    }

    private void OnBatteryChanged(Entity<BatteryDrainerEeepComponent> ent, ref NinjaBatteryChangedEvent args)
    {
        SetBattery((ent, ent.Comp), args.Battery);
    }

    /// <inheritdoc/>
    protected override void OnDoAfterAttempt(Entity<BatteryDrainerEeepComponent> ent, ref DoAfterAttemptEvent<DrainDoAfterEvent> args)
    {
        base.OnDoAfterAttempt(ent, ref args);

        if (ent.Comp.BatteryUid is not {} battery || _battery.IsFull(battery))
            args.Cancel();
        if (_whitelist.IsWhitelistFail(component.Whitelist, targetEntity))
            args.Cancel();
    }

    /// <inheritdoc/>
    protected override bool TryDrainPower(Entity<BatteryDrainerEeepComponent> ent, EntityUid target)
    {
        var (uid, comp) = ent;
        if (comp.BatteryUid == null || !TryComp<BatteryComponent>(comp.BatteryUid.Value, out var battery))
            return false;

        if (!TryComp<BatteryComponent>(target, out var targetBattery) || !TryComp<PowerNetworkBatteryComponent>(target, out var pnb))
            return false;

        if (MathHelper.CloseToPercent(targetBattery.CurrentCharge, 0))
        {
            _popup.PopupEntity(Loc.GetString("battery-drainer-empty", ("battery", target)), uid, uid, PopupType.Medium);
            return false;
        }

        var available = targetBattery.CurrentCharge;
        var required = battery.MaxCharge - battery.CurrentCharge;
        // higher tier storages can charge more
        var maxDrained = pnb.MaxSupply * comp.DrainTime;
        var input = Math.Min(Math.Min(available, required / comp.DrainEfficiency), maxDrained);
        if (!_battery.TryUseCharge(target, input, targetBattery))
            return false;

        var output = input * comp.DrainEfficiency;
        _battery.SetCharge(comp.BatteryUid.Value, battery.CurrentCharge + output, battery);
        // TODO: create effect message or something
        Spawn("EffectSparks", Transform(target).Coordinates);
        _audio.PlayPvs(comp.SparkSound, target);
        _popup.PopupEntity(Loc.GetString("battery-drainer-success", ("battery", target)), uid, uid);

        // repeat the doafter until battery is full
        return !_battery.IsFull(comp.BatteryUid.Value, battery);
    }
}
