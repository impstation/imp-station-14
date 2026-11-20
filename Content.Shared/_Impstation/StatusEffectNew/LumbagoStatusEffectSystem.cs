using Content.Shared._Impstation.StatusEffectNew.Components;
using Content.Shared.Movement.Pulling.Components;
using Content.Shared.Movement.Systems;
using Content.Shared.Popups;
using Content.Shared.Random.Helpers;
using Content.Shared.StatusEffectNew;
using Content.Shared.StatusEffectNew.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Shared._Impstation.StatusEffectNew;
/// <summary>
/// System for the lumbago status effect.
/// Occasionally send popups about back pain, makes pulling slower, and occasionally causes a blanket move speed debuff.
/// </summary>
public sealed class LumbagoStatusEffectSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly MovementModStatusSystem _movementMod = default!;
    [Dependency] private readonly StatusEffectsSystem _statusEffects = default!;

    private readonly TimeSpan _lumbagoUpdateInterval= TimeSpan.FromSeconds(1);
    private TimeSpan _lumbagoUpdateTimer = TimeSpan.Zero;
    private readonly EntProtoId _flareUpStatusEffect = "LumbagoFlareUpSlowdownStatusEffect";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<LumbagoStatusEffectComponent, MapInitEvent>(StatusEffectApplied);
        SubscribeLocalEvent<LumbagoStatusEffectComponent, StatusEffectRelayedEvent<RefreshMovementSpeedModifiersEvent>>(TryModifyMovementSpeed);
    }


    private void StatusEffectApplied(Entity<LumbagoStatusEffectComponent> ent, ref MapInitEvent args)
    {
        //TODO: Replace with random predicted when we get that.
        var seed = SharedRandomExtensions.HashCodeCombine((int)_timing.CurTick.Value, GetNetEntity(ent).Id);
        var rand = new System.Random(seed);
        ent.Comp.LumbagFlareUpDelay=_timing.CurTime + // we're setting the timers here so doing it like this is okay
                                    TimeSpan.FromSeconds(ent.Comp.LumbagoFlareUpDelayMinMax.Next(rand));
        ent.Comp.LumbagoReminderDelay=_timing.CurTime +
                                      TimeSpan.FromSeconds(ent.Comp.LumbagoReminderDelayMinMax.Next(rand));
        Dirty(ent);
    }

    /// <summary>
    /// Selectively modifies pulling movespeed.
    /// </summary>
    private void TryModifyMovementSpeed(Entity<LumbagoStatusEffectComponent> ent, ref StatusEffectRelayedEvent<RefreshMovementSpeedModifiersEvent> args)
    {
       if (!HasComp<ActivePullerComponent>(ent.Comp.Affected))
            return;
       args.Args.ModifySpeed(ent.Comp.PullWalkSpeedMod, ent.Comp.PullSprintSpeedMod);
    }

    /// <summary>
    /// Every second we check the flare up timers of each entity with the status effect.
    /// </summary>
    /// <param name="frameTime"></param>
    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if(_timing.CurTime < _lumbagoUpdateTimer)
            return;

        var query = EntityQueryEnumerator<LumbagoStatusEffectComponent,StatusEffectComponent>();
        _lumbagoUpdateTimer += _lumbagoUpdateInterval;

        while (query.MoveNext(out var lumbagoEnt, out var lumbagoComp, out var statusComp))
        {
            if (statusComp.AppliedTo is not { } statusOwner)
                continue;

            //TODO: Replace with random predicted when we get that.
            var seed = SharedRandomExtensions.HashCodeCombine((int)_timing.CurTick.Value, GetNetEntity(statusOwner).Id);
            var rand = new System.Random(seed);

            //if we have reached the time for the next flare up, trigger it.
            if (_timing.CurTime >= lumbagoComp.LumbagFlareUpDelay)
            {
                var duration = TimeSpan.FromSeconds(rand.NextFloat(lumbagoComp.FlareUpDurationMinMax.Min, lumbagoComp.FlareUpDurationMinMax.Max));
                _movementMod.TryAddMovementSpeedModDuration(statusOwner, _flareUpStatusEffect, duration,lumbagoComp.FlareUpMovementSpeedMod);
                lumbagoComp.LumbagFlareUpDelay += duration + TimeSpan.FromSeconds(lumbagoComp.LumbagoFlareUpDelayMinMax.Next(rand));
                Dirty(lumbagoEnt,lumbagoComp);
            }

            if (_timing.CurTime < lumbagoComp.LumbagoReminderDelay)
                return;

            //Send a reminder to the player when the timer runs out and there is a flair up occuring.
            if (_statusEffects.HasStatusEffect(statusOwner, _flareUpStatusEffect))
            {
                var selected = rand.Pick(lumbagoComp.BadPainReminders);

                _popup.PopupClient(Loc.GetString(selected), statusOwner, statusOwner, PopupType.SmallCaution);

                lumbagoComp.LumbagoReminderDelay += TimeSpan.FromSeconds(lumbagoComp.LumbagoReminderDelayMinMax.Next(rand));
                Dirty(lumbagoEnt,lumbagoComp);

            }
            //Send a reminder to the player when the timer runs out.
            else
            {
                var selected = rand.Pick(lumbagoComp.MildPainReminders);

                _popup.PopupClient(Loc.GetString(selected), statusOwner, statusOwner);

                lumbagoComp.LumbagoReminderDelay += TimeSpan.FromSeconds(lumbagoComp.LumbagoReminderDelayMinMax.Next(rand));
                Dirty(lumbagoEnt,lumbagoComp);
            }

        }

    }
}
