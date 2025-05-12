using Content.Shared._DV.Polymorph;
using Content.Shared.ActionBlocker;
using Content.Shared.Buckle.Components;
using Content.Shared.Climbing.Events;
using Content.Shared.DoAfter;
using Content.Shared.Hands;
using Content.Shared.Hands.Components;
using Content.Shared.Interaction.Events;
using Content.Shared.Inventory.VirtualItem;
using Content.Shared.Mind.Components;
using Content.Shared.Mobs;
using Content.Shared.Movement.Events;
using Content.Shared.Movement.Pulling.Components;
using Content.Shared.Movement.Pulling.Events;
using Content.Shared.Movement.Systems;
using Content.Shared.Popups;
using Content.Shared.Resist;
using Content.Shared.Standing;
using Content.Shared.Stunnable;
using Content.Shared.Throwing;
using Content.Shared.Verbs;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics.Components;
using System.Numerics;
using System.Threading;
// frontier:
using Content.Shared.Contests;
using Content.Shared.Movement.Pulling.Systems;
using Robust.Shared.Network;

namespace Content.Shared._DV.Carrying;
public sealed class CarryingSystem : EntitySystem
{
    [Dependency] private readonly ActionBlockerSystem _actionBlocker = default!;
    [Dependency] private readonly CarryingSlowdownSystem _slowdown = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _movementSpeed = default!;
    [Dependency] private readonly PullingSystem _pulling = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly StandingStateSystem _standingState = default!;
    [Dependency] private readonly SharedVirtualItemSystem _virtualItem = default!;
    [Dependency] private readonly ContestsSystem _contests = default!; // frontier

    private EntityQuery<PhysicsComponent> _physicsQuery;
    public const float BaseDistanceCoeff = 0.5f; // Frontier: default throwing speed reduction
    public const float MaxDistanceCoeff = 1.0f; // Frontier: default throwing speed reduction
    public const float DefaultMaxThrowDistance = 4.0f; // Frontier: maximum throwing distance

    public override void Initialize()
    {
        base.Initialize();

        _physicsQuery = GetEntityQuery<PhysicsComponent>();

        SubscribeLocalEvent<CarriableComponent, GetVerbsEvent<AlternativeVerb>>(AddCarryVerb);
        SubscribeLocalEvent<CarryingComponent, VirtualItemDeletedEvent>(OnVirtualItemDeleted);
        SubscribeLocalEvent<CarryingComponent, BeforeThrowEvent>(OnThrow);
        SubscribeLocalEvent<CarryingComponent, EntParentChangedMessage>(OnParentChanged);
        SubscribeLocalEvent<CarryingComponent, MobStateChangedEvent>(OnMobStateChanged);
        SubscribeLocalEvent<CarryingComponent, DownedEvent>(OnDowned);
        SubscribeLocalEvent<CarryingComponent, BeforePolymorphedEvent>(OnBeforePolymorphed);
        SubscribeLocalEvent<BeingCarriedComponent, InteractionAttemptEvent>(OnInteractionAttempt);
        SubscribeLocalEvent<BeingCarriedComponent, UpdateCanMoveEvent>(OnMoveAttempt);
        SubscribeLocalEvent<BeingCarriedComponent, StandAttemptEvent>(OnStandAttempt);
        SubscribeLocalEvent<BeingCarriedComponent, GettingInteractedWithAttemptEvent>(OnInteractedWith);
        SubscribeLocalEvent<BeingCarriedComponent, PullAttemptEvent>(OnPullAttempt);
        SubscribeLocalEvent<BeingCarriedComponent, StartClimbEvent>(OnDrop);
        SubscribeLocalEvent<BeingCarriedComponent, BuckledEvent>(OnDrop);
        SubscribeLocalEvent<BeingCarriedComponent, UnbuckledEvent>(OnDrop);
        SubscribeLocalEvent<BeingCarriedComponent, StrappedEvent>(OnDrop);
        SubscribeLocalEvent<BeingCarriedComponent, UnstrappedEvent>(OnDrop);
        SubscribeLocalEvent<BeingCarriedComponent, EscapeInventoryEvent>(OnDrop);
        SubscribeLocalEvent<BeingCarriedComponent, ComponentRemove>(OnRemoved);
        SubscribeLocalEvent<CarriableComponent, CarryDoAfterEvent>(OnDoAfter);
    }

    private void AddCarryVerb(Entity<CarriableComponent> ent, ref GetVerbsEvent<AlternativeVerb> args)
    {
        var user = args.User;
        var target = args.Target;
        if (!args.CanInteract || !args.CanAccess || user == target)
            return;

        if (!CanCarry(user, ent))
            return;

        args.Verbs.Add(new AlternativeVerb()
        {
            Act = () => StartCarryDoAfter(user, ent),
            Text = Loc.GetString("carry-verb"),
            Priority = 2
        });
    }

    /// <summary>
    /// Since the carried entity is stored as 2 virtual items, when deleted we want to drop them.
    /// </summary>
    private void OnVirtualItemDeleted(Entity<CarryingComponent> ent, ref VirtualItemDeletedEvent args)
    {
        if (HasComp<CarriableComponent>(args.BlockingEntity))
            DropCarried(ent, args.BlockingEntity);
    }

    /// <summary>
    /// Basically using virtual item passthrough to throw the carried person. A new age!
    /// Maybe other things besides throwing should use virt items like this...
    /// </summary>
    private void OnThrow(Entity<CarryingComponent> ent, ref BeforeThrowEvent args)
    {
        if (!TryComp<VirtualItemComponent>(args.ItemUid, out var virtItem) || !HasComp<CarriableComponent>(virtItem.BlockingEntity))
            return;

        var carried = virtItem.BlockingEntity;
        args.ItemUid = carried;

        var contestCoeff = _contests.MassContest(ent, virtItem.BlockingEntity, false, 2f) * _contests.StaminaContest(ent, virtItem.BlockingEntity); // Frontier: "args.throwSpeed *="<"var contestCoeff ="

        // Frontier: sanitize our range regardless of CVar values - TODO: variable throw distance ranges (via traits, etc.)
        contestCoeff = float.Min(BaseDistanceCoeff * contestCoeff, MaxDistanceCoeff);
        if (args.Direction.Length() > DefaultMaxThrowDistance * contestCoeff)
            args.Direction = args.Direction.Normalized() * DefaultMaxThrowDistance * contestCoeff;
        // End Frontier
    }

    private void OnParentChanged(Entity<CarryingComponent> ent, ref EntParentChangedMessage args)
    {
        var xform = Transform(ent);
        if (xform.MapUid != args.OldMapId)
            return;

        // Do not drop the carried entity if the new parent is a grid
        if (xform.ParentUid == xform.GridUid)
            return;

        DropCarried(ent, ent.Comp.Carried);
    }

    private void OnMobStateChanged(Entity<CarryingComponent> ent, ref MobStateChangedEvent args)
    {
        DropCarried(ent, ent.Comp.Carried);
    }

    private void OnDowned(Entity<CarryingComponent> ent, ref DownedEvent args)
    {
        DropCarried(ent, ent.Comp.Carried);
    }

    private void OnBeforePolymorphed(Entity<CarryingComponent> ent, ref BeforePolymorphedEvent args)
    {
        if (HasComp<MindContainerComponent>(ent.Comp.Carried))
            DropCarried(ent, ent.Comp.Carried);
    }

    /// <summary>
    /// Only let the person being carried interact with their carrier and things on their person.
    /// </summary>
    private void OnInteractionAttempt(Entity<BeingCarriedComponent> ent, ref InteractionAttemptEvent args)
    {
        if (args.Target is not { } target)
            return;

        var targetParent = Transform(target).ParentUid;

        var carrier = ent.Comp.Carrier;
        if (target != carrier && targetParent != carrier && targetParent != ent.Owner)
            args.Cancelled = true;
    }

    private void OnMoveAttempt(Entity<BeingCarriedComponent> ent, ref UpdateCanMoveEvent args)
    {
        args.Cancel();
    }

    private void OnStandAttempt(Entity<BeingCarriedComponent> ent, ref StandAttemptEvent args)
    {
        args.Cancel();
    }

    private void OnInteractedWith(Entity<BeingCarriedComponent> ent, ref GettingInteractedWithAttemptEvent args)
    {
        if (args.Uid != ent.Comp.Carrier)
            args.Cancelled = true;
    }

    private void OnPullAttempt(Entity<BeingCarriedComponent> ent, ref PullAttemptEvent args)
    {
        args.Cancelled = true;
    }

    private void OnDrop<TEvent>(Entity<BeingCarriedComponent> ent, ref TEvent args) // Augh
    {
        DropCarried(ent.Comp.Carrier, ent);
    }

    private void OnRemoved(Entity<BeingCarriedComponent> ent, ref ComponentRemove args)
    {
        /*
            This component has been removed for whatever reason, so just make sure that the
            carrier is cleaned up.
            */
        if (!TryComp<CarryingComponent>(ent.Comp.Carrier, out var carryingComponent))
            // This carrier has probably already been cleaned, no reason to try again
            return;

        CleanupCarrier(ent.Comp.Carrier, ent);
    }

    private void OnDoAfter(Entity<CarriableComponent> ent, ref CarryDoAfterEvent args)
    {
        ent.Comp.CancelToken = null;
        if (args.Handled || args.Cancelled
        || !CanCarry(args.Args.User, ent))
            return;

        Carry(args.Args.User, ent);
        args.Handled = true;
    }

    private void StartCarryDoAfter(EntityUid carrier, Entity<CarriableComponent> carried)
    {
        // NF: change arbitrary doafter length cancel to a mass check
        if (!TryComp<PhysicsComponent>(carrier, out var carrierPhysics)
        || !TryComp<PhysicsComponent>(carried, out var carriedPhysics)
        || carriedPhysics.Mass > carrierPhysics.Mass * 2f)
        {
            _popup.PopupClient(Loc.GetString("carry-too-heavy"), carried, carrier, PopupType.SmallCaution);
            return;
        }

        var length = carried.Comp.PickupDuration //Frontier: removed outer TimeSpan.FromSeconds()
        * _contests.MassContest(carriedPhysics, carrierPhysics, false, 4f)
        * _contests.StaminaContest(carrier, carried)
        * (_standingState.IsDown(carried) ? 0.5f : 1); // Frontier: replace !HasComp<KnockedDownComponent> with IsDown

        // Frontier: sanitize time duration regardless of CVars - no near-instant pickups.
        var duration = TimeSpan.FromSeconds(float.Clamp(length, carried.Comp.MinPickupDuration, carried.Comp.MaxPickupDuration));

        carried.Comp.CancelToken = new CancellationTokenSource();
        // End Frontier

        var ev = new CarryDoAfterEvent();
        var args = new DoAfterArgs(EntityManager, carrier, duration, ev, carried, target: carried) // Frontier: length > duration
        {
            BreakOnMove = true,
            NeedHand = true
        };

        _doAfter.TryStartDoAfter(args);

        // Show a popup to the person getting picked up
        _popup.PopupEntity(Loc.GetString("carry-started", ("carrier", carrier)), carried, carried);
    }

    private void Carry(EntityUid carrier, EntityUid carried)
    {
        if (TryComp<PullableComponent>(carried, out var pullable))
            _pulling.TryStopPull(carried, pullable);

        var carrierXform = Transform(carrier);
        var xform = Transform(carried);
        _transform.AttachToGridOrMap(carrier);
        _transform.AttachToGridOrMap(carried);
        _transform.SetParent(carried, carrier);

        var carryingComp = EnsureComp<CarryingComponent>(carrier);
        carryingComp.Carried = carried;
        Dirty(carrier, carryingComp);
        var carriedComp = EnsureComp<BeingCarriedComponent>(carried);
        carriedComp.Carrier = carrier;
        Dirty(carried, carriedComp);
        EnsureComp<KnockedDownComponent>(carried);

        ApplyCarrySlowdown(carrier, carried);

        _actionBlocker.UpdateCanMove(carried);

        if (_net.IsClient) // no spawning prediction
            return;

        _virtualItem.TrySpawnVirtualItemInHand(carried, carrier);
        _virtualItem.TrySpawnVirtualItemInHand(carried, carrier);
    }

    public bool TryCarry(EntityUid carrier, Entity<CarriableComponent?> toCarry)
    {
        if (!Resolve(toCarry, ref toCarry.Comp, false))
            return false;

        if (!CanCarry(carrier, (toCarry, toCarry.Comp)) || HasComp<BeingCarriedComponent>(carrier))
            return false;

        // Frontier: replace timespan with phys check
        if (TryComp<PhysicsComponent>(carrier, out var carrierPhysics) && TryComp<PhysicsComponent>(toCarry, out var toCarryPhysics) && carrierPhysics.Mass < toCarryPhysics.Mass * 2f)
            return false;

        Carry(carrier, toCarry);

        return true;
    }

    public void DropCarried(EntityUid carrier, EntityUid carried)
    {
        Drop(carried);
        CleanupCarrier(carrier, carried);
    }

    private void CleanupCarrier(EntityUid carrier, EntityUid carried)
    {
        RemComp<CarryingComponent>(carrier); //get rid of this first so we don't recursively fire that event
        RemComp<CarryingSlowdownComponent>(carrier);
        _virtualItem.DeleteInHandsMatching(carrier, carried);

        _movementSpeed.RefreshMovementSpeedModifiers(carrier);
    }

    private void Drop(EntityUid carried)
    {
        RemComp<BeingCarriedComponent>(carried);
        RemComp<KnockedDownComponent>(carried); // TODO SHITMED: make sure this doesnt let you make someone with no legs walk

        _actionBlocker.UpdateCanMove(carried);
        _transform.AttachToGridOrMap(carried);
        _standingState.Stand(carried);
    }

    private void ApplyCarrySlowdown(EntityUid carrier, EntityUid carried)
    {
        // Frontier edits. Yup, we're using mass contests again.
        var massRatio = _contests.MassContest(carrier, carried, true);
        var massRatioSq = MathF.Pow(massRatio, 2);
        var modifier = 1 - 0.15f / massRatioSq;
        modifier = Math.Max(0.1f, modifier);
        // End frontier edits

        _slowdown.SetModifier(carrier, modifier);
    }

    public bool CanCarry(EntityUid carrier, Entity<CarriableComponent> carried)
    {
        return
            carrier != carried.Owner &&
            // can't carry multiple people, even if you have 4 hands it will break invariants when removing carryingcomponent for first carried person
            !HasComp<CarryingComponent>(carrier) &&
            // can't carry someone in a locker, buckled, etc
            HasComp<MapGridComponent>(Transform(carrier).ParentUid) &&
            // no tower of spacemen or stack overflow
            !HasComp<BeingCarriedComponent>(carrier) &&
            !HasComp<BeingCarriedComponent>(carried) &&
            // finally check that there are enough free hands
            TryComp<HandsComponent>(carrier, out var hands) &&
            hands.CountFreeHands() >= carried.Comp.FreeHandsRequired;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<BeingCarriedComponent, TransformComponent>();
        while (query.MoveNext(out var carried, out var comp, out var xform))
        {
            var carrier = comp.Carrier;
            if (TerminatingOrDeleted(carrier))
            {
                RemCompDeferred<BeingCarriedComponent>(carried);
                continue;
            }

            // SOMETIMES - when an entity is inserted into disposals, or a cryosleep chamber - it can get re-parented without a proper reparent event
            // when this happens, it needs to be dropped because it leads to weird behavior
            if (xform.ParentUid != carrier)
            {
                DropCarried(carrier, carried);
                continue;
            }

            // Make sure the carried entity is always centered relative to the carrier, as gravity pulls can offset it otherwise
            _transform.SetLocalPosition(carried, Vector2.Zero);
        }
    }
}
