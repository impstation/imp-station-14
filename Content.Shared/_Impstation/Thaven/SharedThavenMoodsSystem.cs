using Content.Shared._Impstation.StrangeMoods;
using Content.Shared.Bed.Sleep;
using Content.Shared.Emag.Systems;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;

namespace Content.Shared._Impstation.Thaven;

public abstract class SharedThavenMoodsSystem : SharedStrangeMoodsSystem
{
    [Dependency] private readonly EmagSystem _emag = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ThavenMoodsComponent, GotEmaggedEvent>(OnEmagged);
    }

    protected virtual void OnEmagged(Entity<ThavenMoodsComponent> ent, ref GotEmaggedEvent args)
    {
        if (!_emag.CompareFlag(args.Type, EmagType.Interaction))
            return;

        if (_emag.CheckFlag(ent, EmagType.Interaction))
            return;

        // if the target is not sleeping, dead, or crit, skip.
        var target = ent.Owner;
        var user = args.UserUid;
        if (!HasComp<SleepingComponent>(target) && !_mobState.IsIncapacitated(target) && target != user)
        {
            _popup.PopupClient(Loc.GetString("emag-thaven-alive", ("emag", ent), ("target", target)), user, user);
            return;
        }

        args.Handled = true;
    }
}
