using Content.Server.Atmos.EntitySystems;
using Content.Server.Body.Components;
using Content.Server.Body.Systems;
using Content.Server.Jittering;
using Content.Server.Popups;
using Content.Shared._Impstation.Anomalocarid;
using Content.Shared.Actions.Components;
using Content.Shared.Alert;
using Content.Shared.Damage.Systems;
using Content.Shared.Ghost;
using Content.Shared.Mind.Components;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server._Impstation.Anomalocarid;

public sealed class HeatVentSystem : SharedHeatVentSystem
{
    [Dependency] private readonly AlertsSystem _alerts = default!;
    [Dependency] private readonly AtmosphereSystem _atmos = default!;
    [Dependency] private readonly DamageableSystem _damage = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly JitteringSystem _jittering = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly RespiratorSystem _respirator = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HeatVentComponent, HeatVentDoAfterEvent>(OnVent);
        SubscribeLocalEvent<HeatVentComponent, MindAddedMessage>(OnMindAdded);
    }

    private void OnMindAdded(Entity<HeatVentComponent> ent, ref MindAddedMessage args)
    {
        ent.Comp.MindActive = true;
    }

    private void OnVent(Entity<HeatVentComponent> ent, ref HeatVentDoAfterEvent args)
    {
        var tileMix = _atmos.GetTileMixture(ent.Owner, excite: true);
        if (tileMix != null)
        {
            tileMix.AdjustMoles(ent.Comp.VentGas, ent.Comp.HeatStored * ent.Comp.MolesPerHeatStored);
            var gasTemp = ent.Comp.GasTempBase + ent.Comp.GasTempHeatMultiplier * ent.Comp.HeatStored;
            tileMix.Temperature = Math.Clamp(gasTemp, ent.Comp.GasTempMin, ent.Comp.GasTempMax);
        }

        _audio.PlayPvs(ent.Comp.VentSound, ent);
        _popup.PopupEntity(Loc.GetString(ent.Comp.VentDoAfterPopup, ("target", ent)), ent);
        ent.Comp.HeatStored = 0;
        UpdateAlert(ent);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<HeatVentComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (_timing.CurTime < comp.UpdateTimer || !comp.MindActive)
                continue;

            comp.UpdateTimer = _timing.CurTime + comp.UpdateCooldown;

            Cycle((uid, comp));
        }
    }

    public void Cycle(Entity<HeatVentComponent> ent)
    {
        if (!TryComp<RespiratorComponent>(ent, out var respirator) || !_respirator.IsBreathing((ent.Owner, respirator)))
            return;

        ent.Comp.HeatStored += ent.Comp.HeatAdded;

        if (ent.Comp.HeatStored >= ent.Comp.HeatDamageThreshold)
        {
            _jittering.DoJitter(ent, ent.Comp.UpdateCooldown, false);
            _damage.TryChangeDamage(ent.Owner, ent.Comp.HeatDamage, ignoreResistances: true, interruptsDoAfters: false);

            if (_random.NextFloat() < ent.Comp.TooHotPopupChance && ent.Comp.TooHotPopups != null)
                _popup.PopupEntity(Loc.GetString(_random.Pick(ent.Comp.TooHotPopups)), ent);
        }

        UpdateAlert(ent);
    }

    private void UpdateAlert(Entity<HeatVentComponent> ent)
    {
        short severity;
        switch (ent.Comp.HeatStored / ent.Comp.HeatDamageThreshold)
        {
            case >= 1f:
                severity = 5;
                break;
            case >= 0.6f:
                severity = 4;
                break;
            case >= 0.3f:
                severity = 3;
                break;
            case >= 0.15f:
                severity = 2;
                break;
            default:
                severity = 1;
                break;
        }

        if (TryComp<AlertsComponent>(ent, out var alerts))
        {
            _alerts.ShowAlert((ent, alerts), ent.Comp.Alert, severity);
        }
    }
}
