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

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BatteryDrainerEeepComponent, ComponentStartup>(OnStartup); // imp add
        SubscribeLocalEvent<BatteryDrainerEeepComponent, BeforeInteractHandEvent>(OnBeforeInteractHand);
        SubscribeLocalEvent<BatteryDrainerEeepComponent, NinjaBatteryChangedEvent>(OnBatteryChanged);
    }

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
    private void OnBeforeInteractHand(Entity<BatteryDrainerEeepComponent> ent, ref BeforeInteractHandEvent args)
    {
        var (uid, comp) = ent;
        var target = args.Target;
        if (args.Handled || comp.BatteryUid is not { } battery || !HasComp<PowerNetworkBatteryComponent>(target))
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
        /// I HATE MATH I HATE MATH IM CASTING A CURSE ON THIS .CS ///
        /// making all power sources give the same ammount based on percent charged ///
        var charge = targetBattery.CurrentCharge / targetBattery.MaxCharge;
        var powerget = charge * 500000;
        _battery.SetCharge(comp.BatteryUid.Value, battery.CurrentCharge + powerget);
        /// taking power away from target ///
        var available = targetBattery.CurrentCharge;
        var required = battery.MaxCharge - battery.CurrentCharge;
        var maxDrained = battery.MaxCharge - battery.CurrentCharge;
        var input = Math.Min(Math.Min(available, required), maxDrained);
        if (!_battery.TryUseCharge(target, input, targetBattery))
            return false;
        // TODO: create effect message or something
        Spawn("EffectSparks", Transform(target).Coordinates);
        _audio.PlayPvs(comp.SparkSound, target);
        _popup.PopupEntity(Loc.GetString("battery-drainer-success", ("battery", target)), uid, uid);

        // repeat the doafter until battery is full
        return !_battery.IsFull(comp.BatteryUid.Value, battery);
    }
}