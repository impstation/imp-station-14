using Content.Server.Ghost;
using Content.Server._Impstation.Ghost;
using Content.Server.EUI;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Roles;
using Content.Shared.Roles.Components;
using Content.Shared.Silicons.Borgs;
using Content.Shared.Silicons.Borgs.Components;
using Robust.Shared.GameObjects.Components.Localization;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server._Impstation.Borgs;

public sealed class MMIHandlerSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly EntityManager _entityManager = default!;
    [Dependency] private readonly EuiManager _euiManager = default!;
    [Dependency] private readonly GhostSystem _ghost = default!;
    [Dependency] private readonly GrammarSystem _grammar = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly ISharedPlayerManager _player = default!;
    [Dependency] private readonly SharedRoleSystem _roles = default!;

    private static readonly EntProtoId SiliconBrainRole = "MindRoleSiliconBrain";
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MMIComponent, MMIInsertionSuccessEvent>(OnSuccessfulInsertion);
        SubscribeLocalEvent<BorgBrainComponent, BorgBrainHibernationEvent>(OnHibernationAttempt);

    }

    private void OnSuccessfulInsertion(EntityUid uid, MMIComponent component, MMIInsertionSuccessEvent args)
    {
        var mmi = uid;
        var brain = args.BrainInserted;

        if (_mind.TryGetMind(brain, out _, out var mind) &&
            _player.TryGetSessionById(mind.UserId, out var playerSession))
        {
            if (mind.CurrentEntity != brain)
            {
                var session = playerSession;

                // notify them they're being revived.
                _euiManager.OpenEui(new ReturnToBrainEui(uid, brain, _entityManager, _grammar, _mind, _roles), session);
            }
            else
            {
                var grammar = _entityManager.EnsureComponent<GrammarComponent>(mmi);
                if (_entityManager.TryGetComponent<GrammarComponent>(brain, out var formerSelf))
                {
                    _grammar.SetGender((mmi, grammar), formerSelf.Gender);
                }

                if (_mind.TryGetMind(brain, out var mindId, out var mindComp))
                {
                    _mind.TransferTo(mindId, mmi, true, mind: mindComp);

                    if (!_roles.MindHasRole<SiliconBrainRoleComponent>(mindId))
                        _roles.MindAddRole(mindId, SiliconBrainRole, silent: true);
                }
            }

            _appearance.SetData(mmi, MMIVisuals.BrainPresent, true);

        }
    }

    private void OnHibernationAttempt(EntityUid brain, BorgBrainComponent comp, ref BorgBrainHibernationEvent args)
    {
        if (HasComp<VisitingMindComponent>(brain))
            return;

        if (!_mind.TryGetMind(brain, out var mindId, out var mind) || mind.IsVisitingEntity)
            return;

        _ghost.OnGhostAttempt(mindId, false, mind: mind);
    }
}
