using System.Linq;
using Content.Shared._Impstation.Gravity;
using Content.Shared.Clothing;
using Content.Shared.GameTicking;
using Content.Shared.Gravity;
using Robust.Shared.GameStates;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Events;

namespace Content.Server._Impstation.Gravity;

public sealed partial class ZeroGravityAreaSystem : EntitySystem
{
    private ulong _nextPredictIndex = 0;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnReset);

        SubscribeLocalEvent<ZeroGravityAreaComponent, StartCollideEvent>(OnStartCollision);
        SubscribeLocalEvent<ZeroGravityAreaComponent, EndCollideEvent>(OnEndCollision);
        SubscribeLocalEvent<ZeroGravityAreaComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<ZeroGravityAreaComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<ZeroGravityAreaComponent, ComponentGetState>(OnGetAreaState);

        SubscribeLocalEvent<IsInZeroGravityAreaComponent, IsWeightlessEvent>(OnCheckWeightless, after: [typeof(SharedMagbootsSystem)]);
        SubscribeLocalEvent<IsInZeroGravityAreaComponent, ComponentGetState>(OnGetEntityState);
    }

    private void OnReset(RoundRestartCleanupEvent args)
    {
        _nextPredictIndex = 0;
    }

    public bool IsEnabled(EntityUid uid, ZeroGravityAreaComponent? comp = null)
    {
        if (!Resolve(uid, ref comp))
            return false;

        return comp.Enabled;
    }

    public void SetEnabled(EntityUid uid, bool enabled, ZeroGravityAreaComponent? comp = null)
    {
        if (!Resolve(uid, ref comp))
            return;

        comp.Enabled = enabled;
        Dirty(uid, comp);
        foreach (var ent in comp.AffectedEntities)
        {
            // Update entity states to see if they're no longer weightless
            Dirty(ent);
        }
    }

    private void StartAffecting(Entity<ZeroGravityAreaComponent> area, Entity<IsInZeroGravityAreaComponent> entity)
    {
        area.Comp.AffectedEntities.Add(entity);
        entity.Comp.AffectingAreas.Add(area);
        Dirty(entity);
    }

    private void StopAffecting(Entity<ZeroGravityAreaComponent> area, Entity<IsInZeroGravityAreaComponent> entity)
    {
        area.Comp.AffectedEntities.Remove(entity);
        entity.Comp.AffectingAreas.Remove(area);
        Dirty(entity);
    }

    /// <summary>
    /// Check if two colliding ZeroGravityAreaComponents have the same PredictIndex. If so,
    /// set the smaller one to one above the largest one.
    /// </summary>
    private void CheckPredictIndices(Entity<ZeroGravityAreaComponent> self, Entity<ZeroGravityAreaComponent> other)
    {
        Log.Debug($"Checking predict ({self.Comp.PredictIndex}, {other.Comp.PredictIndex})");
        if (self.Comp.PredictIndex != other.Comp.PredictIndex)
            return;

        var smaller = (self.Comp.PredictIndex > other.Comp.PredictIndex) ? other : self;
        smaller.Comp.PredictIndex = (byte)((Math.Max(self.Comp.PredictIndex, other.Comp.PredictIndex) + 1) % (sizeof(ulong) * 8));
        Dirty(smaller);
        foreach (var ent in smaller.Comp.AffectedEntities)
            Dirty(ent);
    }

    private void OnStartCollision(EntityUid uid, ZeroGravityAreaComponent comp, StartCollideEvent args)
    {
        if (args.OurFixtureId != comp.Fixture)
            return;

        if (TryComp<ZeroGravityAreaComponent>(args.OtherEntity, out var area))
            CheckPredictIndices((uid, comp), (args.OtherEntity, area));

        if (!TryComp<PhysicsComponent>(args.OtherEntity, out var physics))
            return;

        if ((physics.BodyType & (BodyType.Kinematic | BodyType.Static)) != 0)
            return;

        var antiGrav = EnsureComp<IsInZeroGravityAreaComponent>(args.OtherEntity);
        StartAffecting((uid, comp), (args.OtherEntity, antiGrav));
    }

    private void OnEndCollision(EntityUid uid, ZeroGravityAreaComponent comp, EndCollideEvent args)
    {
        if (args.OurFixtureId != comp.Fixture)
            return;

        if (!TryComp<IsInZeroGravityAreaComponent>(args.OtherEntity, out var antiGrav))
            return;

        StopAffecting((uid, comp), (args.OtherEntity, antiGrav));
    }

    private void OnStartup(EntityUid uid, ZeroGravityAreaComponent comp, ComponentStartup args)
    {
        const byte maxIndex = sizeof(ulong) * 8;

        if (_nextPredictIndex == maxIndex)
            Log.Warning($"There are more than {maxIndex} ZeroGravityArea components. Prediction errors may start occurring in overlapping ZeroGravityAreas.");

        comp.PredictIndex = (byte)(_nextPredictIndex % maxIndex);
        _nextPredictIndex += 1;

        Dirty(uid, comp);
    }

    private void OnShutdown(EntityUid uid, ZeroGravityAreaComponent comp, ComponentShutdown args)
    {
        foreach (var ent in comp.AffectedEntities)
        {
            ent.Comp.AffectingAreas.Remove((uid, comp));
            Dirty(ent);
        }
    }

    private void OnGetAreaState(Entity<ZeroGravityAreaComponent> ent, ref ComponentGetState args)
    {
        args.State = new ZeroGravityAreaState(ent.Comp);
    }

    private bool EntityIsWeightless(IsInZeroGravityAreaComponent ent)
    {
        return ent.AffectingAreas.Any(area => area.Comp.Enabled);
    }

    private void OnCheckWeightless(EntityUid uid, IsInZeroGravityAreaComponent comp, ref IsWeightlessEvent args)
    {
        if (args.Handled)
            return;

        if (EntityIsWeightless(comp))
        {
            args.IsWeightless = true;
            args.Handled = true;
        }
    }

    private void OnGetEntityState(EntityUid uid, IsInZeroGravityAreaComponent comp, ref ComponentGetState args)
    {
        // Update the fingerprint of the affecting areas
        args.State = new IsInZeroGravityAreaState(comp.AffectingAreas.Aggregate(0ul, (fingerprint, area) =>
        {
            if (area.Comp.Enabled)
                fingerprint |= 1ul << area.Comp.PredictIndex;
            return fingerprint;
        }));
    }
}
