using System.Linq;
using Content.Shared.Clothing;
using Content.Shared.Gravity;
using Robust.Shared.GameStates;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Events;

namespace Content.Server._Impstation.Gravity;

public sealed partial class ZeroGravityAreaSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ZeroGravityAreaComponent, StartCollideEvent>(OnStartCollision);
        SubscribeLocalEvent<ZeroGravityAreaComponent, EndCollideEvent>(OnEndCollision);
        SubscribeLocalEvent<ZeroGravityAreaComponent, ComponentShutdown>(OnShutdown);

        SubscribeLocalEvent<IsInZeroGravityAreaComponent, IsWeightlessEvent>(OnCheckWeightless, after: [typeof(SharedMagbootsSystem)]);
        SubscribeLocalEvent<IsInZeroGravityAreaComponent, ComponentGetState>(OnGetEntityState);
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

    private void OnStartCollision(EntityUid uid, ZeroGravityAreaComponent comp, StartCollideEvent args)
    {
        if (args.OurFixtureId != comp.Fixture)
            return;

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

    private void OnShutdown(EntityUid uid, ZeroGravityAreaComponent comp, ComponentShutdown args)
    {
        foreach (var ent in comp.AffectedEntities)
        {
            ent.Comp.AffectingAreas.Remove((uid, comp));
            Dirty(ent);
        }
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
        args.State = new IsInZeroGravityAreaState(EntityIsWeightless(comp));
    }
}
