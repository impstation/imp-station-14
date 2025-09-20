using Content.Server.Antag;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.Mind;
using Content.Server.Objectives;
using Content.Server.Roles;
using Content.Shared.Arcfiend;
using Content.Shared.NPC.Prototypes;
using Content.Shared.NPC.Systems;
using Content.Shared.Roles;
using Content.Shared.Roles.Components;
using Content.Shared.Humanoid;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Content.Shared.Electrocution;

namespace Content.Server.GameTicking.Rules;

public sealed class ArcfiendRuleSystem : GameRuleSystem<ArcfiendRuleComponent>
{
    [Dependency] private readonly MindSystem _mind = default!;
    [Dependency] private readonly AntagSelectionSystem _antag = default!;
    [Dependency] private readonly SharedRoleSystem _role = default!;
    [Dependency] private readonly NpcFactionSystem _npcFaction = default!;
    [Dependency] private readonly ObjectivesSystem _objective = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _userInterfaceSystem = default!;
    public readonly SoundSpecifier BriefingSound = new SoundPathSpecifier("/Audio/_Goobstation/Ambience/Antag/changeling_start.ogg"); //replace this sound

    public readonly ProtoId<AntagPrototype> ArcfiendPrototypeId = "Arcfiend";

    public readonly ProtoId<NpcFactionPrototype> ArcfiendFactionId = "Arcfiend";

    public readonly ProtoId<NpcFactionPrototype> NanotrasenFactionId = "NanoTrasen";

    [ValidatePrototypeId<EntityPrototype>] EntProtoId _mindRole = "MindRoleArcfiend";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ArcfiendRuleComponent, AfterAntagEntitySelectedEvent>(OnSelectAntag);
        SubscribeLocalEvent<ArcfiendRuleComponent, ObjectivesTextPrependEvent>(OnTextPrepend);
    }

    private void OnSelectAntag(EntityUid uid, ArcfiendRuleComponent comp, ref AfterAntagEntitySelectedEvent args)
    {
        MakeArcfiend(args.EntityUid, comp);
    }

    public bool MakeArcfiend(EntityUid target, ArcfiendRuleComponent rule)
    {
        if (!_mind.TryGetMind(target, out var mindId, out var mind))
            return false;

        _role.MindAddRole(mindId, _mindRole.Id, mind, true);

        // briefing
        var metaData = MetaData(target);
        var briefing = Loc.GetString("arcfiend-role-greeting", ("name", metaData?.EntityName ?? "Unknown"));
        var briefingShort = Loc.GetString("arcfiend-role-greeting-short", ("name", metaData?.EntityName ?? "Unknown"));

        _antag.SendBriefing(target, briefing, Color.Yellow, BriefingSound);

        if (_role.MindHasRole<ArcfiendRoleComponent>(mindId, out var mr))
            AddComp(mr.Value, new RoleBriefingComponent { Briefing = briefingShort }, overwrite: true);

        _npcFaction.RemoveFaction(target, NanotrasenFactionId, false);
        _npcFaction.AddFaction(target, ArcfiendFactionId);

        // var arcfiendComp = EnsureComp<ArcfiendComponent>(target); not using this anywhere? i just copied it originally
        EnsureComp<ArcfiendComponent>(target);

        rule.ArcfiendMinds.Add(mindId);

        return true;
    }

    private void OnTextPrepend(EntityUid uid, ArcfiendRuleComponent comp, ref ObjectivesTextPrependEvent args)
    {
        //this is for round end screen ill do this later
    }
}
