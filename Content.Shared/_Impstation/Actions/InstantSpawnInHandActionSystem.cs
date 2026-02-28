using Content.Shared.Actions;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Popups;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;

namespace Content.Shared._Impstation.Actions;

public sealed class InstantSpawnInHandActionSystem : EntitySystem
{
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedContainerSystem _containers = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SharedPopupSystem _popups = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<InstantSpawnInHandActionEvent>(OnInstantSpawnInHandAction);
    }

    private void OnInstantSpawnInHandAction(InstantSpawnInHandActionEvent args)
    {
        if (_hands.GetActiveHand(args.Performer) is not { } activeHand)
            return;

        // Don't allow to summon an item if holding an unremoveable item.
        if (_hands.GetActiveItem(args.Performer) != null
            && !_hands.CanDropHeld(args.Performer, activeHand, false))
        {
            _popups.PopupClient(Loc.GetString("retractable-item-hand-cannot-drop"), args.Performer, args.Performer);
            return;
        }

        var ent = Spawn(args.Prototype);
        _hands.TryForcePickup(args.Performer, ent, activeHand, checkActionBlocker: false);
        _audio.PlayPredicted(args.SummonSounds, args.Performer, args.Performer);

        args.Handled = true;
    }
}
