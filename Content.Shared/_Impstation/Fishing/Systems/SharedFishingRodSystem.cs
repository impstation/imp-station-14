using Content.Shared._Impstation.Fishing.Components;
using Content.Shared._Impstation.Fishing.Events;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction.Events;
using Content.Shared.Item;
using Content.Shared.Physics;
using Content.Shared.Popups;
using Content.Shared.Throwing;
using Robust.Shared.Containers;
using Robust.Shared.Network;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared._Impstation.Fishing.Systems;

public abstract class SharedFishingRodSystem : EntitySystem
{
    [Dependency] protected readonly IGameTiming Timing = default!;
    [Dependency] protected readonly IRobustRandom Random = default!;
    [Dependency] protected readonly SharedPopupSystem Popup = default!;
    [Dependency] protected readonly ThrowingSystem Throwing = default!;
    [Dependency] protected readonly ItemSlotsSystem ItemSlots = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<FishingRodComponent, GettingPickedUpAttemptEvent>(OnPickupAttempt);
        SubscribeLocalEvent<FishingRodComponent, EntGotInsertedIntoContainerMessage>(OnRodInserted);
        SubscribeLocalEvent<FishingRodComponent, DroppedEvent>(OnDropped);
        SubscribeLocalEvent<FishingRodComponent, EntInsertedIntoContainerMessage>(OnBobberInserted);
        SubscribeLocalEvent<FishingRodComponent, EntRemovedFromContainerMessage>(OnBobberRemoved);
        SubscribeLocalEvent<FishingRodComponent, FishingAttemptEvent>(OnFishingAttempt);
    }

    private void OnPickupAttempt(Entity<FishingRodComponent> ent, ref GettingPickedUpAttemptEvent args)
    {
        if (args.Cancelled)
            return;

        ent.Comp.Holder = args.User;
        Dirty(ent);
    }

    private void OnRodInserted(Entity<FishingRodComponent> ent, ref EntGotInsertedIntoContainerMessage args)
    {
        if (ent.Comp.Holder is not { } holder)
            return;

        if (IsBeingHeld(ent, holder))
            return;

        RemCompDeferred<JointVisualsComponent>(ent);
        ent.Comp.TargetPool = null;

        if (ent.Comp.Bobber is { } bobber && ItemSlots.GetItemOrNull(ent, ent.Comp.BobberSlotId) == null)
            ItemSlots.TryInsert(ent.Owner, ent.Comp.BobberSlotId, bobber, ent.Comp.Holder);

        if (_net.IsClient)
            return;

        ent.Comp.Holder = null;
        Dirty(ent);
    }

    private static void OnDropped(Entity<FishingRodComponent> ent, ref DroppedEvent args)
    {
        if (args.Handled)
            return;

        ent.Comp.Holder = null;
    }

    private void OnBobberInserted(Entity<FishingRodComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        if (!TryComp<FishingBobberComponent>(args.Entity, out var bobber))
            return;

        ent.Comp.Bobber = args.Entity;
        bobber.Parent = ent;
        bobber.Reeled = true;
    }

    private void OnBobberRemoved(Entity<FishingRodComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        if (!TryComp<FishingBobberComponent>(args.Entity, out var bobber))
            return;

        ent.Comp.Bobber = null;
        bobber.Parent = null;
        bobber.Reeled = false;
    }

    private void OnFishingAttempt(Entity<FishingRodComponent> ent, ref FishingAttemptEvent args)
    {
        if (args.Cancelled)
            return;

        var bobberOrNull = ItemSlots.GetItemOrNull(ent, ent.Comp.BobberSlotId);

        if (bobberOrNull is not { } bobber)
        {
            if (ent.Comp.Holder is { } holder)
                Popup.PopupPredicted(Loc.GetString("fishing-rod-no-bobber"), ent, holder);

            args.Cancel();
            return;
        }

        if (!TryComp<FishingBobberComponent>(bobber, out var bobberComp))
        {
            args.Cancel();
            return;
        }

        if (_net.IsClient)
            return;

        ent.Comp.TargetPool = args.FishingPool;
        ent.Comp.NextPullTime = Timing.CurTime + Random.Next(ent.Comp.MinPullTime, ent.Comp.MaxPullTime);

        var lineVisual = EnsureComp<JointVisualsComponent>(ent);
        lineVisual.Sprite = ent.Comp.LineSprite;
        lineVisual.Target = bobber;
        lineVisual.OffsetB = bobberComp.Offset;

        if (!ItemSlots.TryEject(ent.Owner, ent.Comp.BobberSlotId, ent.Comp.Holder, out _))
            return;

        // TODO: i should not have to reset all these fields because of the eject. this is silly. you are silly
        Throwing.TryThrow(bobber, Transform(args.FishingPool).Coordinates, compensateFriction: true, doSpin: false);
        ent.Comp.Bobber = bobber;
        Dirty(ent);
        bobberComp.Parent = ent;
        bobberComp.Reeled = false;
        Dirty(bobber, bobberComp);
    }

    private bool IsBeingHeld(EntityUid rod, EntityUid holder)
    {
        return TryComp<HandsComponent>(holder, out var hands) && _hands.IsHolding((holder, hands), rod);
    }
}
