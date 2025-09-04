using Content.Shared.Arcfiend;
using Content.Shared.Chemistry.Components;
using Content.Shared.Cuffs.Components;
using Content.Shared.DoAfter;
using Content.Shared.FixedPoint;
using Content.Shared.IdentityManagement;
using Content.Shared.Mobs;
using Content.Shared.Store;
using Content.Shared.Store.Components;
using Content.Shared.Popups;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Atmos.Rotting;
using Content.Server.Objectives.Components;
using Content.Server.Light.Components;
using Content.Shared.Eye.Blinding.Systems;
using Content.Shared.Eye.Blinding.Components;
using Content.Shared.Flash.Components;
using Content.Shared.Forensics.Components;
using Content.Shared.Movement.Pulling.Components;
using Content.Shared.Stealth;
using Content.Shared.Stealth.Components;
using Content.Shared.Damage.Components;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Content.Shared.Mindshield.Components;
using Content.Shared.Destructible;
using Robust.Shared.Utility;
using Robust.Shared.GameObjects;
using Content.Server.Construction.Components;
using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Shared.Mobs.Components;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Toolshed.Commands.Values;
using Content.Shared.Mobs.Systems;
using Content.Server.Explosion.Components;
using Content.Server.Stunnable;
using Content.Server.Electrocution;
using Content.Server.Damage.Systems;
using Content.Server.Popups;
using Content.Server.Forensics;
using Content.Server.NPC.Queries.Considerations;
using Content.Shared.Interaction; //i just pulled allat from the ling files ill clean it up later

namespace Content.Server.Arcfiend;

public sealed partial class ArcfiendSystem : EntitySystem
{
    [Dependency] private readonly BatterySystem _battery = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly DamageableSystem _damageableSystem = default!;
    [Dependency] private readonly StaminaSystem _staminaSystem = default!;
    [Dependency] private readonly ElectrocutionSystem _electrocutionSystem = default!;
    [Dependency] private readonly PopupSystem _popupSystem = default!;
    [Dependency] private readonly SharedInteractionSystem _sharedInteractionSystem = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    public ProtoId<DamageTypePrototype> ShockDamageType = "Shock";

    public void SubscribeAbilities()
    {
        SubscribeLocalEvent<ArcfiendComponent, SapPowerEvent>(OnSapPower);
        SubscribeLocalEvent<ApcPowerReceiverComponent, DrainEnergyEvent>(OnDrainMachine);
        SubscribeLocalEvent<BatteryComponent, DrainEnergyEvent>(OnDrainBattery);
        SubscribeLocalEvent<MobStateComponent, DrainEnergyEvent>(OnDrainMob);

        SubscribeLocalEvent<ArcfiendComponent, DischargeEvent>(OnDischarge);
        SubscribeLocalEvent<ArcfiendComponent, FlashEvent>(OnFlash);
        SubscribeLocalEvent<ArcfiendComponent, ArcFlashEvent>(OnArcFlash);
        SubscribeLocalEvent<ArcfiendComponent, RideTheLightningEvent>(OnRideTheLightning);
        SubscribeLocalEvent<ArcfiendComponent, JammingFieldEvent>(OnJammingField);
        SubscribeLocalEvent<ArcfiendComponent, JoltEvent>(OnJolt);
    }

    private void OnSapPower(EntityUid uid, ArcfiendComponent comp, ref SapPowerEvent args)
    {
        var target = args.Target;
        _sharedInteractionSystem.DoContactInteraction(uid, target);
        if (CheckInsulated(uid, comp))
        {
            _popupSystem.PopupEntity("arcfiend-insulated-sap-attempt", uid, uid, PopupType.MediumCaution);
            return;
        }
        var drain = new DrainEnergyEvent(uid);

        RaiseLocalEvent(target, ref drain);
        var change = drain.Change;

        UpdateEnergy(uid, comp, change);
        Spawn("EffectSparks", Transform(target).Coordinates);
        _audio.PlayPvs(comp.SparkSound, target);
    }
    private void OnDrainMachine(EntityUid uid, ApcPowerReceiverComponent comp, ref DrainEnergyEvent args)
    {
        if (args.Handled) return;
        args.Handled = true;

        var power = comp.NetworkLoad.DesiredPower;

        comp.NetworkLoad.DesiredPower *= 2;

        //doafter

        comp.NetworkLoad.DesiredPower = power;
        args.Change = power * 0.005f; //this number needs tuning
    }
    private void OnDrainBattery(EntityUid uid, BatteryComponent comp, ref DrainEnergyEvent args)
    {
        if (args.Handled) return;
        args.Handled = true;

        var power = comp.MaxCharge / 5;

        //doafter

        if (comp.CurrentCharge > power)
        {
            _battery.SetCharge(uid, comp.CurrentCharge - power);
            args.Change = power * 0.005f; //this number needs tuning
        }
        else
        {
            _popupSystem.PopupPredicted("arcfiend-battery-fully-drained", uid, null, PopupType.LargeCaution);
            args.Change = comp.CurrentCharge;
            _battery.SetMaxCharge(uid, 0f);
        }
    }
    private void OnDrainMob(EntityUid uid, MobStateComponent comp, ref DrainEnergyEvent args)
    {
        if (args.Handled) return;
        args.Handled = true;

        _popupSystem.PopupPredicted("arcfiend-drain-humanoid", uid, uid, PopupType.LargeCaution);

        //doafter

        _damageableSystem.TryChangeDamage(uid, new DamageSpecifier(_proto.Index(ShockDamageType), 5));
        _staminaSystem.TakeStaminaDamage(uid, 20);
        _electrocutionSystem.TryDoElectrocution(uid, args.Source, 5, new TimeSpan(0, 0, 5), true);

        if (_mobState.IsAlive(uid))
        {
            args.Change = 100;
        }

    }
    private void OnDischarge(EntityUid uid, ArcfiendComponent comp, ref DischargeEvent args)
    {

    }
    private void OnFlash(EntityUid uid, ArcfiendComponent comp, ref FlashEvent args)
    {

    }
    private void OnArcFlash(EntityUid uid, ArcfiendComponent comp, ref ArcFlashEvent args)
    {

    }
    private void OnRideTheLightning(EntityUid uid, ArcfiendComponent comp, ref RideTheLightningEvent args)
    {

    }
    private void OnJammingField(EntityUid uid, ArcfiendComponent comp, ref JammingFieldEvent args)
    {

    }
    private void OnJolt(EntityUid uid, ArcfiendComponent comp, ref JoltEvent args)
    {

    }
}
