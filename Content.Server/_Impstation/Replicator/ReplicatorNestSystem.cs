// these are HEAVILY based on the Bingle free-agent ghostrole from GoobStation, but reflavored and reprogrammed to make them more Robust (and less of a meme.)
// all credit for the core gameplay concepts and a lot of the core functionality of the code goes to the folks over at Goob, but I re-wrote enough of it to justify putting it in our filestructure.
// the original Bingle PR can be found here: https://github.com/Goob-Station/Goob-Station/pull/1519

using Content.Shared._Impstation.Replicator;
using Content.Shared.StepTrigger.Systems;
using Content.Shared.Stunnable;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Humanoid;
using Robust.Server.Containers;
using Robust.Shared.Containers;
using Robust.Shared.Timing;
using Content.Shared.Movement.Pulling.Systems;

namespace Content.Server._Impstation.Replicator;

public sealed class ReplicatorNestSystem : EntitySystem
{
    [Dependency] private readonly ContainerSystem _containerSystem = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly PullingSystem _pulling = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ReplicatorNestComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<ReplicatorNestComponent, EntRemovedFromContainerMessage>(OnEntRemoved);
        SubscribeLocalEvent<ReplicatorNestComponent, StepTriggeredOffEvent>(OnStepTriggered);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<ReplicatorNestFallingComponent>();
        while (query.MoveNext(out var uid, out var falling))
        {
            if (_timing.CurTime < falling.NextDeletionTime)
                continue;

            _containerSystem.Insert(uid, falling);
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

    private void OnStepTriggered(Entity<ReplicatorNestComponent> ent, ref StepTriggeredOffEvent args)
    {
        // dont accept if they are already falling
        if (HasComp<ReplicatorNestFallingComponent>(args.Tripper))
            return;

        // Allow dead replicators regardless of current level. 
        if (TryComp<MobStateComponent>(args.Tripper, out var mobState) && HasComp<ReplicatorComponent>(args.Tripper) && mobState.CurrentState != MobState.Dead)
            return;

        // Only allow consuming living beings if the AllowLivingThreshold has been surpassed.
        if (mobState != null && ent.Comp.CurrentLevel < ent.Comp.AllowLivingThreshold)
            return;

        StartFalling(ent, args.Tripper);

        if (ent.Comp.TotalPoints >= ent.Comp.SpawnNewAt * ent.Comp.CurrentLevel)
        {
            SpawnNew(ent, ent.Comp);
            ent.Comp.TotalPoints -= ent.Comp.SpawnNewAt * ent.Comp.CurrentLevel;
        }
    }

    private void StartFalling(Entity<ReplicatorNestComponent> ent, EntityUid tripper, bool playSound = true)
    {
        // regardless of what falls in, you get a point.
        ent.Comp.TotalPoints++;

        // you get bonus points if it was alive.
        if (TryComp<MobStateComponent>(tripper, out var mobState) && mobState.CurrentState != MobState.Dead)
            ent.Comp.TotalPoints += ent.Comp.BonusPointsAlive;

        // you get bonus points if it was a humanoid
        if (HasComp<HumanoidAppearanceComponent>(tripper) && mobState != null)
        {
            if (mobState.CurrentState == MobState.Alive)
                ent.Comp.TotalPoints += ent.Comp.SpawnNewAt * ent.Comp.CurrentLevel; // if the humanoid was alive, you get enough bonus points to spawn a new replicator.
            else
                ent.Comp.TotalPoints += ent.Comp.BonusPointsHumanoid * ent.Comp.CurrentLevel; // otherwise, you just get a few bonus points.
        }

        if (HasComp<ReplicatorComponent>(tripper))
            ent.Comp.TotalPoints += ent.Comp.SpawnNewAt * ent.Comp.CurrentLevel / 4; // recycling four dead replicators nets you one new replicator.

        if (TryComp<PullableComponent>(tripper, out var pullable) && pullable.BeingPulled)
            _pulling.TryStopPull(tripper, pullable, ignoreGrab: true);

        var fall = EnsureComp<BinglePitFallingComponent>(tripper);
        fall.Pit = ent.Comp;
        fall.NextDeletionTime = _timing.CurTime + fall.DeletionTime;
        _stun.TryKnockdown(tripper, fall.DeletionTime, false);

        if (playSound)
            _audio.PlayPvs(ent.Comp.FallingSound, uid);

    }
}
