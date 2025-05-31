using Content.Shared.ActionBlocker;
using Content.Shared.DoAfter;
using Content.Shared.Inventory;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Robust.Shared.Player;

namespace Content.Shared._Impstation.CPR;

public sealed class CprGiverSystem : EntitySystem
{
    [Dependency] private readonly ActionBlockerSystem _blocker = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CprGiverComponent, CprDoAfterEvent>(OnDoAfter);
    }

    private void OnDoAfter(Entity<CprGiverComponent> ent, ref CprDoAfterEvent args)
    {
        if (args.Target is not { } target)
            return;

        RemComp<ReceivingCprComponent>(target);

        if (args.Cancelled || args.Handled)
            return;

        args.Handled = true;

        var receiveEvent = new ReceiveCprEvent(ent);
        RaiseLocalEvent(target, ref receiveEvent);
    }

    public bool TryCpr(EntityUid user, EntityUid target, TimeSpan interval)
    {
        if (!TryComp<CprGiverComponent>(user, out var cprComp) || !CanCpr(user, target))
            return false;

        var performEvent = new PerformCprEvent(target);
        RaiseLocalEvent(user, ref performEvent);

        if (performEvent.Cancelled)
            return false;

        var doTime = Math.Min(interval.Seconds * cprComp.IntervalMultiplier, cprComp.IntervalMax);
        var doAfter = new DoAfterArgs(EntityManager, user, doTime, new CprDoAfterEvent(), target, target)
        {
            BreakOnDamage = true,
            BreakOnHandChange = true,
            BreakOnMove = true,
            NeedHand = true,
        };

        _doAfter.TryStartDoAfter(doAfter);
        EnsureComp<ReceivingCprComponent>(target);

        var popupUser = Loc.GetString("cpr-start-user", ("target", target));
        var popupTarget = Loc.GetString("cpr-start-patient", ("user", user));
        var popupOthers = Loc.GetString("cpr-start-others", ("user", user), ("target", target));
        var otherFilter = Filter.Pvs(target).RemovePlayersByAttachedEntity(user, target);

        _popup.PopupEntity(popupUser, target, user);
        _popup.PopupEntity(popupTarget, target, target);
        _popup.PopupEntity(popupOthers, user, otherFilter, true);

        return true;
    }

    private bool CanCpr(EntityUid user, EntityUid target)
    {
        if (!_blocker.CanInteract(user, target) || !_mobState.IsCritical(target))
            return false;

        if (HasComp<ReceivingCprComponent>(target))
        {
            var popup = Loc.GetString("cpr-already-receiving", ("target", target));
            _popup.PopupEntity(popup, target, user, PopupType.SmallCaution);
            return false;
        }

        // ohhhh my hardcoded outerClothing.. how i hate her
        if (_inventory.TryGetSlotEntity(target, "outerClothing", out var outer))
        {
            var popup = Loc.GetString("cpr-remove-item", ("item", outer));
            _popup.PopupEntity(popup, target, user, PopupType.SmallCaution);
            return false;
        }

        return true;
    }
}
