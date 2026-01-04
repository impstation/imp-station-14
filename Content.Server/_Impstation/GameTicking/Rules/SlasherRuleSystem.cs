using Content.Server.Antag;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.Mind;
using Content.Server.Objectives;
using Content.Server.Objectives.Components;
using Content.Server.Roles;
using Content.Shared.NPC.Prototypes;
using Content.Shared.NPC.Systems;
using Content.Shared.Roles;
using Robust.Shared.Audio;
using Robust.Server.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Robust.Shared.Random;
using System.Text;
// removed Heretic-specific dependencies
using Content.Shared.Roles.Components;
using Content.Shared.Slasher;
using Content.Server.Actions;

namespace Content.Server.GameTicking.Rules;

public sealed partial class SlasherRuleSystem : GameRuleSystem<SlasherRuleComponent>
{
    [Dependency] private readonly MindSystem _mind = default!;
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly AntagSelectionSystem _antag = default!;
    [Dependency] private readonly SharedRoleSystem _role = default!;
    [Dependency] private readonly ActionsSystem _actions = default!;
    [Dependency] private readonly NpcFactionSystem _npcFaction = default!;
    [Dependency] private readonly ObjectivesSystem _objective = default!;
    [Dependency] private readonly IRobustRandom _rand = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public readonly SoundSpecifier BriefingSound = new SoundPathSpecifier("/Audio/_Goobstation/Heretic/Ambience/Antag/Heretic/heretic_gain.ogg");

    [ValidatePrototypeId<NpcFactionPrototype>] public readonly ProtoId<NpcFactionPrototype> SlasherFactionId = "Slasher";
    [ValidatePrototypeId<EntityPrototype>] static EntProtoId _mindRole = "MindRoleSlasher";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SlasherRuleComponent, AfterAntagEntitySelectedEvent>(OnAntagSelect);
        SubscribeLocalEvent<SlasherRuleComponent, ObjectivesTextPrependEvent>(OnTextPrepend);
    }

    private void OnAntagSelect(Entity<SlasherRuleComponent> ent, ref AfterAntagEntitySelectedEvent args)
    {
        TryMakeSlasher(args.EntityUid, ent.Comp);
    }

    public bool TryMakeSlasher(EntityUid target, SlasherRuleComponent rule)
    {
        if (!_mind.TryGetMind(target, out var mindId, out var mind))
            return false;

        _role.MindAddRole(mindId, _mindRole.Id, mind, true);

        // briefing
        if (HasComp<MetaDataComponent>(target))
        {
            var briefingShort = Loc.GetString("slasher-role-greeting-short");

            _antag.SendBriefing(target, Loc.GetString("slasher-role-greeting-fluff"), Color.MediumPurple, null);
            _antag.SendBriefing(target, Loc.GetString("slasher-role-greeting"), Color.Red, BriefingSound);

            if (_role.MindHasRole<SlasherRoleComponent>(mindId, out var mr))
                AddComp(mr.Value, new RoleBriefingComponent { Briefing = briefingShort }, overwrite: true);
        }
        _npcFaction.AddFaction(target, SlasherFactionId);

        EnsureComp<SlasherComponent>(target);
        if (TryComp<SlasherComponent>(target, out var slasherComp))
        {
            _actions.AddAction(target, ref slasherComp.DestroyAction, slasherComp.DestroyActionPrototype);
        }
        rule.Minds.Add(mindId);

        return true;
    }

    public void OnTextPrepend(Entity<SlasherRuleComponent> ent, ref ObjectivesTextPrependEvent args)
    {
        var sb = new StringBuilder();

        var query = EntityQueryEnumerator<SlasherComponent>();
        while (query.MoveNext(out var uid, out var slasher))
        {
            if (!_mind.TryGetMind(uid, out var mindId, out var mind))
                continue;

            var name = _objective.GetTitle((mindId, mind), Name(uid));
            var str = Loc.GetString("roundend-prepend-slasher-success", ("name", name));
            sb.AppendLine(str);
        }

        args.Text = sb.ToString();
    }
}
