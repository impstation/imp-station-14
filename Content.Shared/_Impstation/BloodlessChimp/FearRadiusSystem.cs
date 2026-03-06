using Content.Shared._Impstation.BloodlessChimp.Components;
using Content.Shared.Movement.Events;
using Content.Shared.Popups;

namespace Content.Shared._Impstation.BloodlessChimp;

public sealed class FearRadiusSystem : EntitySystem
{
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    [Dependency] private readonly SharedPopupSystem _popup = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<FearRadiusComponent, MoveEvent>(OnMove);
    }

    private void OnMove(Entity<FearRadiusComponent> ent, ref MoveEvent args)
    {
        if(ent.Comp.Target == null)
            return;

        var targetPos = _transform.GetWorldPosition(ent.Comp.Target.Value);
        var chimpPos = _transform.GetWorldPosition(ent.Owner);

        var distance = Math.Pow(targetPos.X-chimpPos.X,2)+Math.Pow(targetPos.Y-chimpPos.Y,2);
        distance = Math.Sqrt(distance);

        if (distance <= ent.Comp.Radius)
        {
            _popup.PopupClient("gogo", ent.Comp.Target.Value,ent.Comp.Target.Value);
            _popup.PopupPredicted("bubu", ent.Owner, ent.Owner);
        }
    }
}
