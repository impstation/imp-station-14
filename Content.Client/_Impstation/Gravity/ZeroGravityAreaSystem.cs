using System.Linq;
using Content.Client._Impstation.Gravity;
using Content.Shared._Impstation.Gravity;
using Content.Shared.Clothing;
using Content.Shared.Gravity;
using Robust.Client.Physics;
using Robust.Shared.GameStates;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Events;

namespace Content.Client.Gravity;

public sealed partial class ZeroGravityAreaSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<IsInZeroGravityAreaComponent, IsWeightlessEvent>(OnCheckWeightless, after: [typeof(SharedMagbootsSystem)]);
        SubscribeLocalEvent<IsInZeroGravityAreaComponent, ComponentHandleState>(OnHandleEntityState);
        SubscribeLocalEvent<ZeroGravityAreaComponent, ComponentHandleState>(OnHandleAreaState);
        SubscribeLocalEvent<ZeroGravityAreaComponent, StartCollideEvent>(OnStartCollide);
        SubscribeLocalEvent<ZeroGravityAreaComponent, EndCollideEvent>(OnEndCollide);
    }

    public bool IsEnabled(EntityUid uid, ZeroGravityAreaComponent? comp = null)
    {
        if (!Resolve(uid, ref comp))
            return false;

        return comp.Enabled;
    }

    private void OnHandleEntityState(EntityUid uid, IsInZeroGravityAreaComponent comp, ComponentHandleState args)
    {
        if (args.Current is not IsInZeroGravityAreaState state)
            return;

        comp.AreaFingerprint = state.AreaFingerprint;
    }

    private void OnHandleAreaState(EntityUid uid, ZeroGravityAreaComponent comp, ComponentHandleState args)
    {
        if (args.Current is not ZeroGravityAreaState state)
            return;

        comp.Enabled = state.Enabled;
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

        Log.Debug($"Predicting that {args.OtherEntity} enters anti-grav area {uid}");

        var antiGrav = EnsureComp<IsInZeroGravityAreaComponent>(other);
        antiGrav.AreaFingerprint |= other.Id;
        Dirty(other, antiGrav);
    }

    private void OnEndCollide(EntityUid uid, ZeroGravityAreaComponent comp, EndCollideEvent args)
    {
        var other = args.OtherEntity;

        if (args.OurFixtureId != comp.Fixture)
            return;

        if (!TryComp<IsInZeroGravityAreaComponent>(other, out var antiGrav))
            return;

        antiGrav.AreaFingerprint &= ~GetNetEntity(uid).Id;
        Dirty(other, antiGrav);
    }

    private void OnCheckWeightless(EntityUid uid, IsInZeroGravityAreaComponent comp, ref IsWeightlessEvent args)
    {
        if (args.Handled)
            return;

        args.IsWeightless = comp.AreaFingerprint != 0;
        args.Handled = args.IsWeightless;
    }
}
