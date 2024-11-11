using Content.Client._Impstation.Gravity;
using Content.Shared._Impstation.Gravity;
using Content.Shared.Clothing;
using Content.Shared.Gravity;
using Robust.Shared.GameStates;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Events;

namespace Content.Client.Gravity;

public sealed partial class ZeroGravityAreaSystem : SharedZeroGravityAreaSystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<IsInZeroGravityAreaComponent, IsWeightlessEvent>(OnCheckWeightless, after: [typeof(SharedMagbootsSystem)]);
        SubscribeLocalEvent<IsInZeroGravityAreaComponent, ComponentHandleState>(OnHandleEntityState);
        SubscribeLocalEvent<ZeroGravityAreaComponent, StartCollideEvent>(OnStartCollide);
        SubscribeLocalEvent<ZeroGravityAreaComponent, EndCollideEvent>(OnEndCollide);
    }

    private void StartAffecting(Entity<ZeroGravityAreaComponent> area, Entity<IsInZeroGravityAreaComponent> entity)
    {
        UpdateFingerprint(area.Comp, entity.Comp);
        area.Comp.AffectedEntities.Add(GetNetEntity(entity));
        Dirty(area);
        Dirty(entity);
    }

    private void StopAffecting(Entity<ZeroGravityAreaComponent> area, Entity<IsInZeroGravityAreaComponent> entity)
    {
        entity.Comp.AreaFingerprint &= ~(1ul << area.Comp.PredictIndex);
        area.Comp.AffectedEntities.Add(GetNetEntity(entity));
        Dirty(area);
        Dirty(entity);
    }

    private void UpdateFingerprint(ZeroGravityAreaComponent area, IsInZeroGravityAreaComponent entity)
    {
        if (area.Enabled)
            entity.AreaFingerprint |= 1ul << area.PredictIndex;
        else
            entity.AreaFingerprint &= ~(1ul << area.PredictIndex);
    }

    public void DirtyAffected(EntityUid uid, ZeroGravityAreaComponent? comp = null)
    {
        if (!Resolve(uid, ref comp))
            return;

        foreach (var netEnt in comp.AffectedEntities)
        {
            if (!TryGetEntity(netEnt, out var ent))
                continue;

            if (!TryComp<IsInZeroGravityAreaComponent>(ent, out var antiGrav))
                continue;

            UpdateFingerprint(comp, antiGrav);
            Dirty(ent.Value, antiGrav);
        }
    }

    public override void SetEnabled(EntityUid uid, bool enabled, ZeroGravityAreaComponent? comp = null)
    {
        if (!Resolve(uid, ref comp))
            return;

        if (enabled == comp.Enabled)
            return;

        comp.Enabled = enabled;
        Dirty(uid, comp);
        DirtyAffected(uid, comp);
    }

    private void OnHandleEntityState(EntityUid uid, IsInZeroGravityAreaComponent comp, ComponentHandleState args)
    {
        if (args.Current is not IsInZeroGravityAreaState state)
            return;

        comp.AreaFingerprint = state.AreaFingerprint;
    }

    private void OnStartCollide(EntityUid uid, ZeroGravityAreaComponent comp, StartCollideEvent args)
    {
        var other = args.OtherEntity;

        if (args.OurFixtureId != comp.Fixture)
            return;

        if (!TryComp<PhysicsComponent>(other, out var physics))
            return;

        if (!physics.Predict || (physics.BodyType & (BodyType.Static | BodyType.Kinematic)) != 0)
            return;

        var antiGrav = EnsureComp<IsInZeroGravityAreaComponent>(other);
        StartAffecting((uid, comp), (other, antiGrav));
    }

    private void OnEndCollide(EntityUid uid, ZeroGravityAreaComponent comp, EndCollideEvent args)
    {
        var other = args.OtherEntity;

        if (args.OurFixtureId != comp.Fixture)
            return;

        if (!TryComp<IsInZeroGravityAreaComponent>(other, out var antiGrav))
            return;

        StopAffecting((uid, comp), (other, antiGrav));
    }

    private void OnCheckWeightless(EntityUid uid, IsInZeroGravityAreaComponent comp, ref IsWeightlessEvent args)
    {
        if (args.Handled)
            return;

        args.IsWeightless = comp.AreaFingerprint != 0;
        args.Handled = args.IsWeightless;
    }
}
