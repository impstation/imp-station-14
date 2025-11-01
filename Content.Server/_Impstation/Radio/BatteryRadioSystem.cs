using Content.Server.Popups;
using Content.Server.Power.EntitySystems;
using Content.Server.PowerCell;
using Content.Server.Radio;
using Content.Server.Radio.EntitySystems;
using Content.Shared._Impstation.Radio;
using Content.Shared.PowerCell.Components;
using Content.Shared.Radio.Components;
using Content.Shared.Speech;
using Content.Shared.UserInterface;

namespace Content.Server._Impstation.Radio;

public sealed class BatteryRadioSystem : SharedBatteryRadioSystem
{
    [Dependency] private readonly BatterySystem _battery = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly PowerCellSystem _powerCell = default!;
    [Dependency] private readonly RadioDeviceSystem _radio = default!;


    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BatteryRadioComponent, ListenAttemptEvent>(OnAttemptListen);
        SubscribeLocalEvent<BatteryRadioComponent, RadioReceiveAttemptEvent>(OnReceiveRadio);
        SubscribeLocalEvent<BatteryRadioComponent, ActivatableUIOpenAttemptEvent>(OnBeforeHandheldRadioUiOpen);

        SubscribeLocalEvent<ActiveBatteryRadioComponent, PowerCellChangedEvent>(OnPowerCellChanged);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<ActiveBatteryRadioComponent, BatteryRadioComponent>();

        while (query.MoveNext(out var uid, out _, out _))
        {
            if (_powerCell.TryGetBatteryFromSlot(uid, out var batteryUid, out var battery))
            {
                if (!_battery.TryUseCharge(batteryUid.Value, GetCurrentWattage(uid) * frameTime, battery))
                {
                    if (TryComp<RadioMicrophoneComponent>(uid, out var microphone))
                        _radio.SetMicrophoneEnabled(uid, null, false, component: microphone);
                    if (TryComp<RadioSpeakerComponent>(uid, out var speaker))
                        _radio.SetSpeakerEnabled(uid, null, false, component: speaker);
                }
            }
        }
    }

    private void OnAttemptListen(Entity<BatteryRadioComponent> ent, ref ListenAttemptEvent args)
    {
        if (!HasComp<ActiveBatteryRadioComponent>(ent))
            args.Cancel();
    }

    private void OnReceiveRadio(Entity<BatteryRadioComponent> ent, ref RadioReceiveAttemptEvent args)
    {
        if (!HasComp<ActiveBatteryRadioComponent>(ent))
            args.Cancelled = true;
    }

    private void OnBeforeHandheldRadioUiOpen(Entity<BatteryRadioComponent> ent, ref ActivatableUIOpenAttemptEvent args)
    {
        if (!_powerCell.HasCharge(ent, ent.Comp.Wattage))
        {
            _popup.PopupEntity(Loc.GetString("handheld-radio-no-battery"), args.User, args.User);
            args.Cancel();
        }
    }

    private void OnPowerCellChanged(Entity<ActiveBatteryRadioComponent> ent, ref PowerCellChangedEvent args)
    {
        if (args.Ejected)
            RemCompDeferred<ActiveBatteryRadioComponent>(ent);
    }

    public float GetCurrentWattage(Entity<ActiveBatteryRadioComponent?, BatteryRadioComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp1, ref ent.Comp2))
            return 0;

        var multiplier = 0;
        if (ent.Comp1.UsingMicrophone)
            multiplier += 1;
        if (ent.Comp1.UsingSpeaker)
            multiplier += 1;
        return multiplier;
    }

    public void ActivateMicrophone(EntityUid uid, bool activate = true)
    {
        if (activate)
        {
            EnsureComp<ActiveBatteryRadioComponent>(uid, out var active);
            active.UsingMicrophone = true;
        }
        else if (TryComp<ActiveBatteryRadioComponent>(uid, out var active) && active.UsingSpeaker)
            active.UsingMicrophone = false;
        else
            RemCompDeferred<ActiveBatteryRadioComponent>(uid);
    }
}
