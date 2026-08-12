using Content.Shared.Movement.Pulling.Components;
using Content.Shared.Movement.Systems;
using Content.Shared.Popups;
using Content.Shared.Random.Helpers;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared._Impstation.Traits.Assorted;

public sealed class LumbagoSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    private readonly TimeSpan LumbagoUpdateInterval= TimeSpan.FromSeconds(1);
    private TimeSpan LumbagoUpdateTimer = TimeSpan.Zero;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<LumbagoComponent, RefreshMovementSpeedModifiersEvent>(TryModifyMovementSpeed);
    }

    private void TryModifyMovementSpeed(Entity<LumbagoComponent> ent, ref RefreshMovementSpeedModifiersEvent args)
    {
       if (!TryComp<PullerComponent>(ent.Owner, out var pullerComp)||pullerComp.Pulling==null)
            return;
       args.ModifySpeed(ent.Comp.PullWalkSpeedMod, ent.Comp.PullSprintSpeedMod);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        var query= EntityQueryEnumerator<LumbagoComponent>();
        if(LumbagoUpdateTimer<LumbagoUpdateInterval)
            return;
        LumbagoUpdateTimer=TimeSpan.Zero;
        while (query.MoveNext(out var entity, out var lumbagoComp))
        {
            //TODO: Replace with random predicted when we get that.
            var seed = SharedRandomExtensions.HashCodeCombine((int)_timing.CurTick.Value, entity.GetHashCode());
            var rand = new System.Random(seed);

            var roll=rand.NextFloat(0f, 1f);
            if (roll<=lumbagoComp.FlareUpChance)
            {
                _popup.PopupClient("fuuucg",entity,entity,PopupType.LargeCaution);
            }
            if (roll<=lumbagoComp.LumbagoReminderChance)
            {
                _popup.PopupClient("outch",entity,entity,PopupType.Small);
            }

        }

    }
}
