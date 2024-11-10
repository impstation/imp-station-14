using System.Linq;
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

        SubscribeLocalEvent<IsInZeroGravityAreaComponent, IsWeightlessEvent>(OnCheckWeightless);
        SubscribeLocalEvent<IsInZeroGravityAreaComponent, ComponentGetState>(OnGetState);
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
        if (
            args.OurFixtureId == comp.Fixture &&
            TryComp<PhysicsComponent>(args.OtherEntity, out var physics) &&
            (physics.BodyType & (BodyType.Kinematic | BodyType.Static)) == 0
        )
        {
            var antiGrav = EnsureComp<IsInZeroGravityAreaComponent>(args.OtherEntity);
            StartAffecting((uid, comp), (args.OtherEntity, antiGrav));
        }
    }

    private void OnEndCollision(EntityUid uid, ZeroGravityAreaComponent comp, EndCollideEvent args)
    {
        if (args.OurFixtureId == comp.Fixture)
        {
            if (!TryComp<IsInZeroGravityAreaComponent>(args.OtherEntity, out var antiGrav))
                return;

            StopAffecting((uid, comp), (args.OtherEntity, antiGrav));
        }
    }

    private void OnShutdown(EntityUid uid, ZeroGravityAreaComponent comp, ComponentShutdown args)
    {
        foreach (var ent in comp.AffectedEntities)
        {
            ent.Comp.AffectingAreas.Remove((uid, comp));
            Dirty(ent);
        }
    }

    private bool EntityIsWeightless(Entity<IsInZeroGravityAreaComponent> ent)
    {
        return ent.Comp.AffectingAreas.Any(area => area.Comp.Enabled);
    }

    private void OnCheckWeightless(EntityUid uid, IsInZeroGravityAreaComponent comp, ref IsWeightlessEvent args)
    {
        if (args.Handled)
            return;

        if (EntityIsWeightless((uid, comp)))
        {
            args.IsWeightless = true;
            args.Handled = true;
        }
    }

    private void OnGetState(EntityUid uid, IsInZeroGravityAreaComponent comp, ref ComponentGetState args)
    {
        args.State = new IsInZeroGravityAreaState(EntityIsWeightless((uid, comp)));
    }
}
