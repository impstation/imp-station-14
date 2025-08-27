using Content.Server._Goobstation.Heretic.Components;
using Content.Server._Goobstation.Heretic.UI;
using Content.Server.Antag;
using Content.Server.EUI;
using Content.Server.Ghost.Roles.Components;
using Content.Server.Roles;
using Content.Shared.Ghost.Roles.Components;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction.Events;
using Content.Shared.Mind;
using Content.Shared.NPC.Systems;
using Content.Shared.Roles.Components;
using Robust.Shared.Player;

namespace Content.Server.Heretic.EntitySystems;

public sealed class MinionSystem : EntitySystem
{
    [Dependency] private readonly AntagSelectionSystem _antag = default!;
    [Dependency] private readonly EuiManager _euiMan = default!;
    [Dependency] private readonly ISharedPlayerManager _playerManager = default!;
    [Dependency] private readonly NpcFactionSystem _faction = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;

    public void ConvertEntityToMinion(EntityUid minion, MinionComponent comp, bool? createGhostRole, bool? sendBriefing, bool? removeBaseFactions)
    {
        var hasMind = _mind.TryGetMind(minion, out var mindId, out _);

        if (hasMind && sendBriefing == true)
        {
            if (comp.BoundOwner != null)
                SendBriefing(minion, comp, mindId, comp.BoundOwner.Value);

            if (_playerManager.TryGetSessionByEntity(mindId, out var session))
                _euiMan.OpenEui(new MinionNotifEui(), session);
        }

        _mind.MakeSentient(minion);

        if (!HasComp<GhostRoleComponent>(minion) && !hasMind && createGhostRole == true)
        {
            var ghostRole = EnsureComp<GhostRoleComponent>(minion);
            ghostRole.RoleName = Loc.GetString(comp.GhostRoleName);
            ghostRole.RoleDescription = Loc.GetString(comp.GhostRoleDescription);
            ghostRole.RoleRules = Loc.GetString(comp.GhostRoleRules);
        }

        if (!HasComp<GhostRoleMobSpawnerComponent>(minion) && !hasMind)
            EnsureComp<GhostTakeoverAvailableComponent>(minion);

        if (removeBaseFactions == true)
            _faction.ClearFactions((minion, null));

        foreach (var faction in comp.FactionsToAdd)
        {
            _faction.AddFaction((minion, null), faction);
        }
    }

    private void SendBriefing(EntityUid minion, MinionComponent comp, EntityUid owner, EntityUid mindId)
    {
        var brief = Loc.GetString(comp.Briefing, ("ent", Identity.Entity(owner, EntityManager)));
        _antag.SendBriefing(minion, brief, Color.MediumPurple, comp.BriefingSound);

        if (!TryComp<GhoulRoleComponent>(minion, out _))
            AddComp(mindId, new GhoulRoleComponent(), overwrite: true);

        if (!TryComp<RoleBriefingComponent>(minion, out var rolebrief))
            AddComp(mindId, new RoleBriefingComponent { Briefing = brief }, overwrite: true);
        else
            rolebrief.Briefing += $"\n{brief}";
    }

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MinionComponent, AttackAttemptEvent>(OnTryAttack);
        SubscribeLocalEvent<MinionComponent, TakeGhostRoleEvent>(OnTakeGhostRole);
    }

    private void OnTakeGhostRole(EntityUid minion, MinionComponent comp, ref TakeGhostRoleEvent args)
    {
        var hasMind = _mind.TryGetMind(minion, out var mindId, out _);
        if (hasMind && comp.BoundOwner != null)
            SendBriefing(minion, comp, mindId, comp.BoundOwner.Value);
    }

    private static void OnTryAttack(Entity<MinionComponent> ent, ref AttackAttemptEvent args)
    {
        // prevent attacking owner
        if (args.Target == ent.Owner)
            args.Cancel();
    }
}
