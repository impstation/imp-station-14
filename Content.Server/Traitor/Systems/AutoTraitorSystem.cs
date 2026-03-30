using Content.Server.Antag;
using Content.Server.Traitor.Components;
using Content.Shared.Mind.Components;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server.Traitor.Systems;

/// <summary>
/// Makes entities with <see cref="AutoTraitorComponent"/> a traitor either immediately if they have a mind or when a mind is added.
/// </summary>
public sealed class AutoTraitorSystem : EntitySystem
{
    [Dependency] private readonly AntagSelectionSystem _antag = default!;
    [Dependency] private readonly ISharedPlayerManager _player = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AutoTraitorComponent, MindAddedMessage>(OnMindAdded);
    }

    private void OnMindAdded(EntityUid uid, AutoTraitorComponent comp, MindAddedMessage args)
    {
        if (!_player.TryGetSessionById(args.Mind.Comp.UserId, out var session))
            return;

        //#IMP limit number of times this can activate.
        if (comp.MaxActivations > 0 && comp.NumActivations >= comp.MaxActivations)
            return;

        comp.NumActivations ++; //IMP

        _antag.ForceMakeAntag<AutoTraitorComponent>(session, comp.Profile);
    }
}
