using Content.Shared.InteractionVerbs;
using Content.Shared.Standing;

namespace Content.Server.InteractionVerbs.Actions;

[Serializable]
public sealed partial class ChangeStandingStateAction : InteractionAction
{
    [DataField]
    public bool MakeStanding, MakeLaying;

    public override bool CanPerform(InteractionArgs args, InteractionVerbPrototype proto, bool isBefore, VerbDependencies deps)
    {
        if (!deps.EntMan.TryGetComponent<StandingStateComponent>(args.Target, out var state))
            return false;

        if (isBefore)
            args.Blackboard["standing"] = state.Standing;

        return state.Standing && MakeLaying
               || !state.Standing && MakeStanding;
    }

    public override bool Perform(InteractionArgs args, InteractionVerbPrototype proto, VerbDependencies deps)
    {
        var stateSystem = deps.EntMan.System<StandingStateSystem>();

        if (!deps.EntMan.TryGetComponent<StandingStateComponent>(args.Target, out var state)
            || args.TryGetBlackboard("standing", out bool standing) && standing != state.Standing)
            return false;

        // Note: these will get cancelled if the target is forced to stand/lay, e.g. due to a buckle or stun or something else.
        if (!state.Standing && MakeStanding)
            return stateSystem.Stand(args.Target);
        else if (state.Standing && MakeLaying)
            return stateSystem.Down(args.Target);

        return false;
    }
}
