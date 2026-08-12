using Content.Shared.Movement.Pulling.Components;
using Content.Shared.Movement.Systems;
using Content.Shared.Popups;
using Content.Shared.Random.Helpers;
using Content.Shared.StatusEffectNew;
using Content.Shared.StatusEffectNew.Components;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Shared._Impstation.Traits.Assorted;
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

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<LumbagoStatusEffectComponent, StatusEffectAppliedEvent>(StatusEffectApplied);
        SubscribeLocalEvent<LumbagoStatusEffectComponent, StatusEffectRelayedEvent<RefreshMovementSpeedModifiersEvent>>(TryModifyMovementSpeed);
    }


    private void StatusEffectApplied(Entity<LumbagoStatusEffectComponent> ent, ref StatusEffectAppliedEvent args)
    {
        ent.Comp.Affected = args.Target;
    }

    /// <summary>
    /// Selectively modifies pulling movespeed.
    /// </summary>
    private void TryModifyMovementSpeed(Entity<LumbagoStatusEffectComponent> ent, ref StatusEffectRelayedEvent<RefreshMovementSpeedModifiersEvent> args)
    {
       if (!TryComp<PullerComponent>(ent.Comp.Affected, out var pullerComp)||pullerComp.Pulling==null)
            return;
       args.Args.ModifySpeed(ent.Comp.PullWalkSpeedMod, ent.Comp.PullSprintSpeedMod);
    }

    /// <summary>
    /// Every second we roll to start a flair up and send a reminder.
    /// </summary>
    /// <param name="frameTime"></param>
    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if(_timing.CurTime<_lumbagoUpdateTimer)
            return;

        var query= EntityQueryEnumerator<LumbagoStatusEffectComponent,StatusEffectComponent>();
        _lumbagoUpdateTimer=_timing.CurTime+_lumbagoUpdateInterval;

        while (query.MoveNext(out _, out var lumbagoComp, out var statusComp))
        {
            if (statusComp.AppliedTo is not { } statusOwner)
                continue;

            //TODO: Replace with random predicted when we get that.
            var seed = SharedRandomExtensions.HashCodeCombine((int)_timing.CurTick.Value, statusOwner.GetHashCode());
            var rand = new System.Random(seed);

            var roll=rand.NextFloat(0f, 1f);

            //if we roll below or at the chance for a flair up, give the status owner LumbagoFlareUpSlowdownStatusEffect
            if (roll<=lumbagoComp.FlareUpChance && !_statusEffects.HasStatusEffect(statusOwner, "LumbagoFlareUpSlowdownStatusEffect"))
            {
                var duration = TimeSpan.FromSeconds(rand.NextFloat(lumbagoComp.FlareUpDurationMin, lumbagoComp.FlareUpDurationMax));
                _movementMod.TryAddMovementSpeedModDuration(statusOwner, "LumbagoFlareUpSlowdownStatusEffect",duration,lumbagoComp.FlareUpMovementSpeedMod);
                DirtyEntity(statusOwner);
            }

            //Send a reminder to the player if we roll below or at reminder chance and there is a flair up occuring.
            if (roll<=lumbagoComp.LumbagoReminderChance && _statusEffects.HasStatusEffect(statusOwner, "LumbagoFlareUpSlowdownStatusEffect"))
            {
                var selected=rand.Next(lumbagoComp.BadPainReminders.Count);
                if(!lumbagoComp.BadPainReminders.TryGetValue(selected, out var reminder))
                    return;

                _popup.PopupClient(Loc.GetString(reminder),statusOwner,statusOwner,PopupType.SmallCaution);

            }
            //Send a reminder to the player if we roll below or at reminder chance
            else if (roll <= lumbagoComp.LumbagoReminderChance)
            {
                var selected=rand.Next(lumbagoComp.MildPainReminders.Count);
                if(!lumbagoComp.MildPainReminders.TryGetValue(selected, out var reminder))
                    return;

                _popup.PopupClient(Loc.GetString(reminder),statusOwner,statusOwner);
            }

        }

    }
}
