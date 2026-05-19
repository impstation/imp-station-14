using Content.Server._Impstation.Ghost;
using Content.Server.EUI;
using Content.Server.Ghost;
using Content.Shared.Mind;
using Content.Shared.Roles;
using Content.Shared.Silicons.Borgs;
using Content.Shared.Silicons.Borgs.Components;
using Microsoft.Extensions.DependencyModel;
using Robust.Shared.Player;

namespace Content.Server._Impstation.Borgs;

public sealed class MMIHandlerSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly EntityManager _entityManager = default!;
    [Dependency] private readonly EuiManager _euiManager = default!;
    [Dependency] private readonly GrammarSystem _grammar = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly ISharedPlayerManager _player = default!;
    [Dependency] private readonly SharedRoleSystem _roles = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MMIComponent, MMIInsertionSuccessEvent>(OnSuccessfulInsertion);
    }

    private void OnSuccessfulInsertion(EntityUid uid, MMIComponent component, MMIInsertionSuccessEvent args)
    {
        if (_mind.TryGetMind(args.BrainInserted, out _, out var mind) &&
            _player.TryGetSessionById(mind.UserId, out var playerSession))
        {
            var session = playerSession;
            // notify them they're being revived.
            _euiManager.OpenEui(new ReturnToBrainEui(uid, args.BrainInserted, _appearance, _entityManager, _grammar, _mind, _roles), session);
        }
    }
}
