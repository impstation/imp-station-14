using Content.Server.Administration.Logs;
using Content.Server.Antag;
using Content.Server.Mind;
using Content.Server.GameTicking.Rules;
using Content.Server._Impstation.CosmicCult.Components;
using Content.Server.Roles;
using Content.Shared.Humanoid;
using Content.Shared.Mobs.Systems;
using Content.Shared.NPC.Prototypes;
using Content.Shared.NPC.Systems;
using Content.Shared._Impstation.CosmicCult.Components;
using Content.Shared.Zombies;
using Content.Shared.Roles;
using Robust.Shared.Prototypes;
using Robust.Shared.Audio;
using Content.Server.Radio.Components;
using Content.Shared.Heretic;
using Content.Shared.Damage;
using Content.Shared.Objectives.Systems;
using SixLabors.ImageSharp.Formats.Tga;
using Robust.Shared.Player;
using Content.Server.Antag.Components;
using System.Linq;
using Content.Shared.NPC.Components;

namespace Content.Server._Impstation.CosmicCult;

/// <summary>
/// Where all the main stuff for Cosmic Cultists happens.
/// </summary>
public sealed class CosmicCultRuleSystem : GameRuleSystem<CosmicCultRuleComponent>
{
    [Dependency] private readonly IAdminLogManager _adminLogManager = default!; // TODO: add logs for Cosmic Cult
    [Dependency] private readonly AntagSelectionSystem _antag = default!;
    [Dependency] private readonly SharedRoleSystem _role = default!;
    [Dependency] private readonly NpcFactionSystem _npcFaction = default!;
    [Dependency] private readonly MindSystem _mind = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedObjectivesSystem _objectives = default!;

    [ValidatePrototypeId<NpcFactionPrototype>] public readonly ProtoId<NpcFactionPrototype> NanoTrasenFactionId = "NanoTrasen";
    [ValidatePrototypeId<NpcFactionPrototype>] public readonly ProtoId<NpcFactionPrototype> CosmicFactionId = "CosmicCultFaction";
    public readonly SoundSpecifier BriefingSound = new SoundPathSpecifier("/Audio/_Impstation/CosmicCult/antag_cosmic_briefing.ogg");

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CosmicCultRuleComponent, AfterAntagEntitySelectedEvent>(OnAntagSelect);

        SubscribeLocalEvent<CosmicCultLeadComponent, DamageChangedEvent>(DebugFunction); // TODO: This is a placeholder function to call other functions for testing & debugging.
    }
    private void OnAntagSelect(Entity<CosmicCultRuleComponent> ent, ref AfterAntagEntitySelectedEvent args) =>
        TryStartCult(args.EntityUid, ent);

    public void TryStartCult(EntityUid uid, Entity<CosmicCultRuleComponent> rule)
    {
        if (!_mind.TryGetMind(uid, out var mindId, out var mind))
            return;
        _role.MindAddRole(mindId, "MindRoleCosmicCult", mind, true);
        _role.MindHasRole<CosmicCultRoleComponent>(mindId, out var cosmicRole);
        if (cosmicRole is not null)
        {
            EnsureComp<RoleBriefingComponent>(cosmicRole.Value.Owner);
            Comp<RoleBriefingComponent>(cosmicRole.Value.Owner).Briefing = Loc.GetString("objective-cosmiccult-charactermenu");
        }

        _antag.SendBriefing(uid, Loc.GetString("cosmiccult-role-roundstart-fluff"), Color.FromHex("#4cabb3"), BriefingSound);
        _antag.SendBriefing(uid, Loc.GetString("cosmiccult-role-short-briefing"), Color.FromHex("#cae8e8"), null);

        EnsureComp<CosmicCultComponent>(uid);
        EnsureComp<IntrinsicRadioReceiverComponent>(uid);
        var transmitter = EnsureComp<IntrinsicRadioTransmitterComponent>(uid);
        var radio = EnsureComp<ActiveRadioComponent>(uid);
        radio.Channels = new() { "CosmicRadio" };
        transmitter.Channels = new() { "CosmicRadio" };

        _npcFaction.RemoveFaction(uid, NanoTrasenFactionId);
        _npcFaction.AddFaction(uid, CosmicFactionId);

        _mind.TryAddObjective(mindId, mind, "CosmicEntropyObjective");
    }

    public void CosmicConversion(EntityUid uid)
    {
        if (!_mind.TryGetMind(uid, out var mindId, out var mind))
            return;
        _role.MindAddRole(mindId, "MindRoleCosmicCult", mind, true);
        _role.MindHasRole<CosmicCultRoleComponent>(mindId, out var cosmicRole);
        if (cosmicRole is not null)
        {
            EnsureComp<RoleBriefingComponent>(cosmicRole.Value.Owner);
            Comp<RoleBriefingComponent>(cosmicRole.Value.Owner).Briefing = Loc.GetString("objective-cosmiccult-charactermenu");
        }
        _antag.SendBriefing(mind.Session, Loc.GetString("cosmiccult-role-conversion-fluff"), Color.FromHex("#4cabb3"), BriefingSound);
        _antag.SendBriefing(uid, Loc.GetString("cosmiccult-role-short-briefing"), Color.FromHex("#cae8e8"), null);

        EnsureComp<CosmicCultComponent>(uid);
        EnsureComp<IntrinsicRadioReceiverComponent>(uid);
        var transmitter = EnsureComp<IntrinsicRadioTransmitterComponent>(uid);
        var radio = EnsureComp<ActiveRadioComponent>(uid);
        radio.Channels = new() { "CosmicRadio" };
        transmitter.Channels = new() { "CosmicRadio" };

        _npcFaction.RemoveFaction((uid, null), NanoTrasenFactionId);
        _npcFaction.AddFaction((uid, null), CosmicFactionId);

        _mind.TryAddObjective(mindId, mind, "CosmicFinalityObjective");
        _mind.TryAddObjective(mindId, mind, "CosmicMonumentObjective");
        _mind.TryAddObjective(mindId, mind, "CosmicEntropyObjective");
    }

        // var tgt = ent.Owner;
        // if (!_mind.TryGetMind(tgt, out var mindId, out var mind))
        //     return;

        // if (HasComp<CosmicCultComponent>(tgt) || HasComp<HereticComponent>(tgt) || !HasComp<HumanoidAppearanceComponent>(tgt) || !_mobState.IsAlive(tgt) || HasComp<ZombieComponent>(tgt))
        //     return;

        // EnsureComp<RoleBriefingComponent>(tgt);
        // EnsureComp<CosmicCultComponent>(tgt);
        // EnsureComp<IntrinsicRadioReceiverComponent>(tgt);
        // var transmitter = EnsureComp<IntrinsicRadioTransmitterComponent>(tgt);
        // var radio = EnsureComp<ActiveRadioComponent>(tgt);
        // radio.Channels = new() { "CosmicRadio" };
        // transmitter.Channels = new() { "CosmicRadio" };

        // if (mindId == default || !_role.MindHasRole<CosmicCultRoleComponent>(mindId))
        // {
        //     _role.MindAddRole(mindId, "MindRoleCosmicCult", mind, true);
        // }

        // if (mind?.Session != null)
        // {
        //     _antag.SendBriefing(mind.Session, Loc.GetString("cosmiccult-role-conversion-fluff"), Color.FromHex("#4cabb3"), BriefingSound);
        //     _antag.SendBriefing(mind.Session, Loc.GetString("cosmiccult-role-short-briefing"), Color.FromHex("#cae8e8"), null);
        //     Comp<RoleBriefingComponent>(tgt).Briefing = Loc.GetString("objective-cosmiccult-description", ("name", mind.CharacterName ?? "Unknown"));
        //     _objectives.TryCreateObjective(mindId, mind, "CosmicFinalityObjective");
        //     _objectives.TryCreateObjective(mindId, mind, "CosmicMonumentObjective");
        //     _objectives.TryCreateObjective(mindId, mind, "CosmicEntropyObjective");
        // }

    private void DebugFunction(EntityUid uid, CosmicCultLeadComponent comp, ref DamageChangedEvent args) // TODO: This is a placeholder function to call other functions for testing & debugging.
    {

    }

}
