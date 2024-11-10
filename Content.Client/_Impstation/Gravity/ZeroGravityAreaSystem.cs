using Content.Shared.Gravity;
using Robust.Shared.GameStates;

namespace Content.Client.Gravity;

public sealed partial class ZeroGravityAreaSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<IsInZeroGravityAreaComponent, IsWeightlessEvent>(OnCheckWeightless);
        SubscribeLocalEvent<IsInZeroGravityAreaComponent, ComponentHandleState>(OnHandleState);
    }

    private void OnHandleState(EntityUid uid, IsInZeroGravityAreaComponent comp, ComponentHandleState args)
    {
        if (args.Current is not IsInZeroGravityAreaState state)
            return;

        comp.IsWeightless = state.IsWeightless;
    }

    private void OnCheckWeightless(EntityUid uid, IsInZeroGravityAreaComponent comp, ref IsWeightlessEvent args)
    {
        if (args.Handled)
            return;

        args.IsWeightless = comp.IsWeightless;
        args.Handled = comp.IsWeightless;
    }
}
