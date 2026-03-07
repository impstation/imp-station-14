using Content.Shared._Impstation.BloodlessChimp.Components;
using Content.Shared.Movement.Events;
using Content.Shared.Popups;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared._Impstation.BloodlessChimp;

public sealed class FearRadiusSystem : EntitySystem
{
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IRobustRandom _random = default!;


    public override void Update(float frameTime)// you may ask yourself. Sev, why aren't you using collision for this? Well. You see, COLLISION IS NOT PREDICTED!!!!!
    {
        base.Update(frameTime);
        var query = EntityQueryEnumerator<FearRadiusComponent>();
        while (query.MoveNext(out var uid, out var fearRadius))
        {

            if (fearRadius.Target == null || _timing.CurTime <= fearRadius.CurrentCooldown)
                return;

            var targetPos = _transform.GetWorldPosition(fearRadius.Target.Value);
            var chimpPos = _transform.GetWorldPosition(uid);

            var distance = Math.Pow(targetPos.X - chimpPos.X, 2) + Math.Pow(targetPos.Y - chimpPos.Y, 2);
            distance = Math.Sqrt(distance);

            if (!(distance <= fearRadius.Radius))
                return;

            var warning = fearRadius.Warnings[_random.Next(fearRadius.Warnings.Count)];
            _popup.PopupPredicted(Loc.GetString(warning),
                fearRadius.Target.Value,
                fearRadius.Target.Value,
                Filter.Entities(fearRadius.Target.Value),
                true,
                type: PopupType.MediumCaution);
            fearRadius.CurrentCooldown = _timing.CurTime + fearRadius.CooldownTime;
            Dirty(uid,fearRadius);
        }
    }
}
