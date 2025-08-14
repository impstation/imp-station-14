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
using Content.Shared.Mindshield.Components; //i just pulled allat from the ling files ill clean it up later

namespace Content.Server.Arcfiend;

public sealed partial class ArcfiendSystem : EntitySystem
{
    public void SubscribeAbilities()
    {
        SubscribeLocalEvent<ArcfiendComponent, SapPowerEvent>(OnSapPower);
        SubscribeLocalEvent<ArcfiendComponent, DischargeEvent>(OnDischarge);
        SubscribeLocalEvent<ArcfiendComponent, FlashEvent>(OnFlash);
        SubscribeLocalEvent<ArcfiendComponent, ArcFlashEvent>(OnArcFlash);
        SubscribeLocalEvent<ArcfiendComponent, RideTheLightningEvent>(OnRideTheLightning);
        SubscribeLocalEvent<ArcfiendComponent, JammingFieldEvent>(OnJammingField);
        SubscribeLocalEvent<ArcfiendComponent, JoltEvent>(OnJolt);
    }

    private void OnSapPower(EntityUid uid, ArcfiendComponent comp, ref SapPowerEvent args)
    {
        var change = 100; //debug number
        //do things
        UpdateEnergy(uid, comp, change);
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
