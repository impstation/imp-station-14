using Content.Server.EUI;
using Content.Shared.Eui;
using Content.Shared.Mind;
using Content.Shared.Roles.Components;
using Content.Shared.Silicons.Borgs.Components;
using Content.Shared._Impstation.Ghost;
using Content.Shared.Roles;
using Robust.Shared.GameObjects.Components.Localization;
using Robust.Shared.Prototypes;

namespace Content.Server._Impstation.Ghost;

public sealed class ReturnToBrainEui : BaseEui
{
    private readonly EntityManager _entityManager;
    private readonly GrammarSystem _grammar;
    private readonly SharedAppearanceSystem _appearance;
    private readonly SharedMindSystem _mind;
    private readonly SharedRoleSystem _roles;

    private readonly EntityUid _brainEnt;
    private readonly EntityUid _mmiEnt;

    private static readonly EntProtoId SiliconBrainRole = "MindRoleSiliconBrain";

    public ReturnToBrainEui(EntityUid mmi,EntityUid brain, SharedAppearanceSystem appearanceSystem, EntityManager entityManager, GrammarSystem grammarSystem, SharedMindSystem mindSystem, SharedRoleSystem roleSystem)
    {
        _appearance = appearanceSystem;
        _entityManager = entityManager;
        _grammar = grammarSystem;
        _mind = mindSystem;
        _roles = roleSystem;

        _mmiEnt = mmi;
        _brainEnt = brain;
    }

    public override void HandleMessage(EuiMessageBase msg)
    {
        base.HandleMessage(msg);

        if (msg is not ReturnToBrainMessage choice || !choice.Accepted)
        {
            Close();
            return;
        }

        var grammar = _entityManager.EnsureComponent<GrammarComponent>(_mmiEnt);
        if (_entityManager.TryGetComponent<GrammarComponent>(_brainEnt, out var formerSelf))
        {
            _grammar.SetGender((_mmiEnt, grammar), formerSelf.Gender);
        }

        if (_mind.TryGetMind(_brainEnt, out var mindId, out var mindComp))
        {
            _mind.TransferTo(mindId, _mmiEnt, true, mind: mindComp);

            if (!_roles.MindHasRole<SiliconBrainRoleComponent>(mindId))
                _roles.MindAddRole(mindId, SiliconBrainRole, silent: true);
        }

        _appearance.SetData(_mmiEnt, MMIVisuals.BrainPresent, true);

        Close();
    }
}
