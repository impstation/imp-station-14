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
using Content.Server.EUI;
using Robust.Shared.Random;
using Content.Server.Announcements.Systems;
using Robust.Server.Audio;
using Content.Shared.Coordinates;
using Content.Shared.Parallax;
using Robust.Shared.Map.Components;

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
    [Dependency] private readonly EuiManager _euiMan = default!;
    [Dependency] private readonly IRobustRandom _rand = default!;
    [Dependency] private readonly AnnouncerSystem _announce = default!;
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    [ValidatePrototypeId<NpcFactionPrototype>] public readonly ProtoId<NpcFactionPrototype> NanoTrasenFactionId = "NanoTrasen";
    [ValidatePrototypeId<NpcFactionPrototype>] public readonly ProtoId<NpcFactionPrototype> CosmicFactionId = "CosmicCultFaction";
    public readonly SoundSpecifier BriefingSound = new SoundPathSpecifier("/Audio/_Impstation/CosmicCult/antag_cosmic_briefing.ogg");

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CosmicCultRuleComponent, AfterAntagEntitySelectedEvent>(OnAntagSelect);
        SubscribeLocalEvent<CosmicCultLeadComponent, DamageChangedEvent>(DebugFunction); // TODO: This is a placeholder function to call other functions for testing & debugging.
        SubscribeLocalEvent<CosmicMonumentComponent, ComponentInit>(Tier3);
    }
    private void OnAntagSelect(Entity<CosmicCultRuleComponent> ent, ref AfterAntagEntitySelectedEvent args) =>
        TryStartCult(args.EntityUid, ent);

    private void Tier2(Entity<CosmicCultRuleComponent> uid)
    {
        var sender = Loc.GetString("cosmiccult-announcement-sender");
        _announce.SendAnnouncementMessage(_announce.GetAnnouncementId("SpawnAnnounceCaptain"), Loc.GetString("cosmiccult-announce-tier2-progress"), sender, Color.FromHex("#cae8e8"));
        _audio.PlayGlobal("/Audio/_Impstation/CosmicCult/ability_blank.ogg", Filter.Broadcast(), false, AudioParams.Default); //TODO: Replace audio.
        for (int i = 0; i < _rand.Next(8, 16); i++)
            if (TryFindRandomTile(out var _, out var _, out var _, out var coords))
                Spawn("CosmicMalignRift", coords);
    }

    private void Tier3(Entity<CosmicMonumentComponent> uid, ref ComponentInit args)
    {
        var sender = Loc.GetString("cosmiccult-announcement-sender");
        var map = _transform.GetMapId(uid.Owner.ToCoordinates());
        var mapData = _map.GetMap(map);
        _announce.SendAnnouncementMessage(_announce.GetAnnouncementId("SpawnAnnounceCaptain"), Loc.GetString("cosmiccult-announce-tier3-progress"), sender, Color.FromHex("#cae8e8"));
        _audio.PlayGlobal("/Audio/_Impstation/CosmicCult/ability_blank.ogg", Filter.Broadcast(), false, AudioParams.Default); //TODO: Replace audio.
        EnsureComp<ParallaxComponent>(mapData, out var parallax);
        parallax.Parallax = "CosmicFinaleParallax";
        Dirty(mapData, parallax);
        EnsureComp<MapLightComponent>(mapData, out var mapLight);
        mapLight.AmbientLightColor = Color.FromHex("#210746");
        Dirty(mapData, mapLight);
    }

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
        if (_mind.TryGetSession(mindId, out var session))
        {
            _euiMan.OpenEui(new CosmicRoundStartEui(), session);
        }
        Tier2(rule);
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
        if (_mind.TryGetSession(mindId, out var session))
        {
            _euiMan.OpenEui(new CosmicConvertedEui(), session);
        }
    }

    private void DebugFunction(EntityUid uid, CosmicCultLeadComponent comp, ref DamageChangedEvent args) // TODO: This is a placeholder function to call other functions for testing & debugging.
    {
    }

}
