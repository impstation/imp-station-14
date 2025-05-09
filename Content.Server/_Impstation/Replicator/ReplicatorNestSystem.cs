// these are HEAVILY based on the Bingle free-agent ghostrole from GoobStation, but reflavored and reprogrammed to make them more Robust (and less of a meme.)
// all credit for the core gameplay concepts and a lot of the core functionality of the code goes to the folks over at Goob, but I re-wrote enough of it to justify putting it in our filestructure.
// the original Bingle PR can be found here: https://github.com/Goob-Station/Goob-Station/pull/1519

using Content.Server.Stunnable;
using Content.Shared.Humanoid;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.StepTrigger.Systems;
using Content.Shared.Stunnable;
using Content.Shared.Movement.Pulling.Components;
using Content.Shared.Movement.Pulling.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Timing;
using Content.Shared._Impstation.Replicator;
using Robust.Server.Containers;
using Robust.Server.Audio;
using Content.Server.Popups;
using Robust.Server.GameObjects;
using Content.Server.GameTicking;
using Content.Server.Pinpointer;
using Content.Shared.Movement.Events;
using Content.Shared._Impstation.SpawnedFromTracker;
using Content.Shared.Tools.Components;
using Content.Shared.Destructible;
using Robust.Shared.Serialization.TypeSerializers.Implementations;
using Robust.Shared.Random;
using Content.Shared.Mobs.Systems;
using Content.Server.Actions;
using Content.Shared.Actions;
using Content.Server.Mind;
using Content.Shared.Mind.Components;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Content.Shared.Item;
using Content.Server.Item;
using Robust.Shared.Prototypes;
using Content.Shared.Construction.Components;

namespace Content.Server._Impstation.Replicator;

public sealed class ReplicatorNestSystem : SharedReplicatorNestSystem
{
    [Dependency] private readonly ContainerSystem _containerSystem = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly PullingSystem _pulling = default!;
    [Dependency] private readonly StunSystem _stun = default!;
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly ReplicatorSystem _replicator = default!;
    [Dependency] private readonly StepTriggerSystem _stepTrigger = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly AppearanceSystem _appearance = default!;
    [Dependency] private readonly TransformSystem _transform = default!;
    [Dependency] private readonly NavMapSystem _navMap = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly ActionsSystem _actions = default!;
    [Dependency] private readonly ActionContainerSystem _actionContainer = default!;
    [Dependency] private readonly ItemSystem _item = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ReplicatorNestComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<ReplicatorNestComponent, EntRemovedFromContainerMessage>(OnEntRemoved);
        SubscribeLocalEvent<ReplicatorNestComponent, StepTriggerAttemptEvent>(OnStepTriggerAttempt);
        SubscribeLocalEvent<ReplicatorNestFallingComponent, UpdateCanMoveEvent>(OnUpdateCanMove);
        SubscribeLocalEvent<ReplicatorNestComponent, DestructionEventArgs>(OnDestruction);
        SubscribeLocalEvent<ReplicatorNestComponent, ComponentRemove>(OnComponentRemove);
        SubscribeLocalEvent<RoundEndTextAppendEvent>(OnRoundEndTextAppend);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<ReplicatorNestFallingComponent>();
        while (query.MoveNext(out var uid, out var falling))
        {
            if (_timing.CurTime < falling.NextDeletionTime)
                continue;

            _containerSystem.Insert(uid, falling.FallingTarget.Comp.Hole);
            EnsureComp<StunnedComponent>(uid); // used stunned to prevent any funny being done inside the pit
            RemCompDeferred(uid, falling);
        }
    }

    private void OnEntRemoved(Entity<ReplicatorNestComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        RemCompDeferred<StunnedComponent>(args.Entity);
    }

    private void OnMapInit(Entity<ReplicatorNestComponent> ent, ref MapInitEvent args)
    {
        if (!Transform(ent).Coordinates.IsValid(EntityManager))
            QueueDel(ent);

        ent.Comp.Hole = _containerSystem.EnsureContainer<Container>(ent, "hole");
    }

    private void OnStepTriggerAttempt(Entity<ReplicatorNestComponent> ent, ref StepTriggerAttemptEvent args)
    {
        args.Continue = true;
    }

    private void OnUpdateCanMove(Entity<ReplicatorNestFallingComponent> ent, ref UpdateCanMoveEvent args)
    {
        args.Cancel();
    }

    private void OnComponentRemove(Entity<ReplicatorNestComponent> ent, ref ComponentRemove args)
    {
        HandleDestruction(ent);
    }

    private void OnDestruction(Entity<ReplicatorNestComponent> ent, ref DestructionEventArgs args)
    {
        HandleDestruction(ent);
    }

    private void HandleDestruction(Entity<ReplicatorNestComponent> ent)
    {
        if (ent.Comp.Hole != null) // hole should never be null, because hole is created when the component initializes
        {
            foreach (var uid in _containerSystem.EmptyContainer(ent.Comp.Hole))
            {
                RemCompDeferred<StunnedComponent>(uid);
                _stun.TryKnockdown(uid, TimeSpan.FromSeconds(2), false);
            }
        }

        // delete all unclaimed spawners
        foreach (var spawner in ent.Comp.UnclaimedSpawners)
            QueueDel(spawner);

        // remove the falling component from anyone currently falling into this nest
        var query = EntityQueryEnumerator<ReplicatorNestFallingComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (comp.FallingTarget == ent)
                RemCompDeferred<ReplicatorNestFallingComponent>(uid);
        }

        // Figure out who the queen is & which replicators belonging to this nest are still alive.
        EntityUid? queen = null;
        HashSet<Entity<ReplicatorComponent>> livingReplicators = [];
        foreach (var replicator in ent.Comp.SpawnedMinions)
        {
            if (!TryComp<ReplicatorComponent>(replicator, out var replicatorComp) || replicatorComp == null)
                continue;

            if (!_mobState.IsAlive(replicator))
                continue;

            if (replicatorComp.Queen)
                queen = replicator;

            livingReplicators.Add((replicator, replicatorComp));

            _popup.PopupEntity(Loc.GetString("replicator-nest-destroyed"), replicator, replicator);
        }
        // if there's no queen, pick a new queen at random from this nest's living replicators, and give them the action to make a new nest.
        if (queen == null && livingReplicators.Count > 0)
        {
            queen = _random.Pick(livingReplicators);
            var comp = EnsureComp<ReplicatorComponent>((EntityUid)queen);
            comp.Queen = true;
            comp.RelatedReplicators = livingReplicators;

            if (!TryComp<MindContainerComponent>(queen, out var mindContainer) || mindContainer.Mind == null)
                return;

            if (!mindContainer.HasMind)
                _actions.AddAction((EntityUid)queen, ent.Comp.SpawnNewNestAction);
            else
                _actionContainer.AddAction((EntityUid)mindContainer.Mind, ent.Comp.SpawnNewNestAction);
        }
    }

    private void OnRoundEndTextAppend(RoundEndTextAppendEvent args)
    {
        List<Entity<ReplicatorNestComponent>> nests = [];

        // get all the nests that have existed this round in a list
        var query = AllEntityQuery<ReplicatorNestComponent>();
        while (query.MoveNext(out var uid, out var comp))
            nests.Add((uid, comp));

        if (nests.Count == 0)
            return;

        // linebreak
        args.AddLine("");

        // add a bit for every nest showing their location, level at the end of the round, and points. 
        foreach (var ent in nests)
        {
            var location = "Unknown";
            var mapCoords = _transform.ToMapCoordinates(Transform(ent).Coordinates);
            if (_navMap.TryGetNearestBeacon(mapCoords, out var beacon, out _) && beacon?.Comp.Text != null)
                location = beacon?.Comp.Text!;

            var points = ent.Comp.TotalPoints;

            var replicators = ent.Comp.SpawnedMinions.Count;

            args.AddLine(Loc.GetString("replicator-end-of-round", ("location", location), ("level", ent.Comp.CurrentLevel), ("points", points), ("replicators", replicators)));
        }

        // linebreak
        args.AddLine("");
    }
}
