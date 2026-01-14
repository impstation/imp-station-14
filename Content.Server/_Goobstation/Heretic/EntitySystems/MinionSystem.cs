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
using Content.Shared.Roles;
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
    [Dependency] private readonly SharedRoleSystem _role = default!;

    public void ConvertEntityToMinion(Entity<MinionComponent> ent, bool? createGhostRole, bool? sendBriefing, bool? removeBaseFactions)
    {
        var hasMind = _mind.TryGetMind(ent, out var mindId, out _);

        if (hasMind && sendBriefing == true)
        {
            if (ent.Comp.BoundOwner != null)
                SendBriefing((ent, ent.Comp), mindId, ent.Comp.BoundOwner.Value);

            if (_playerManager.TryGetSessionByEntity(mindId, out var session))
                _euiMan.OpenEui(new GhoulNotifEui(), session);
        }

        _mind.MakeSentient(ent);
        _role.MindAddRole(mindId, "MindRoleGhostRoleFamiliar");

        if (!HasComp<GhostRoleComponent>(ent) && !hasMind && createGhostRole == true)
        {
            var ghostRole = EnsureComp<GhostRoleComponent>(ent);
            ghostRole.RoleName = Loc.GetString(ent.Comp.GhostRoleName);
            ghostRole.RoleDescription = Loc.GetString(ent.Comp.GhostRoleDescription);
            ghostRole.RoleRules = Loc.GetString(ent.Comp.GhostRoleRules);
        }

        if (!HasComp<GhostRoleMobSpawnerComponent>(ent) && !hasMind)
            EnsureComp<GhostTakeoverAvailableComponent>(ent);

        if (removeBaseFactions == true)
            _faction.ClearFactions((ent, null));

        foreach (var faction in ent.Comp.FactionsToAdd)
        {
            _faction.AddFaction((ent, null), faction);
        }
    }

    private void SendBriefing(Entity<MinionComponent> ent, EntityUid owner, EntityUid mindId)
    {
        var brief = Loc.GetString(ent.Comp.Briefing, ("ent", Identity.Entity(owner, EntityManager)));
        _antag.SendBriefing(ent, brief, Color.MediumPurple, ent.Comp.BriefingSound);

        if (!TryComp<GhoulRoleComponent>(ent, out _))
            AddComp(mindId, new GhoulRoleComponent(), overwrite: true);

        if (!TryComp<RoleBriefingComponent>(ent, out var rolebrief))
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

    private void OnTakeGhostRole(Entity<MinionComponent> ent, ref TakeGhostRoleEvent args)
    {
        var hasMind = _mind.TryGetMind(ent, out var mindId, out var mind);

        if (ent.Comp.BoundOwner == null)
            return;

        if (hasMind)
            SendBriefing(ent, ent.Comp.BoundOwner.Value, mindId);
    }

    private static void OnTryAttack(Entity<MinionComponent> ent, ref AttackAttemptEvent args)
    {
        // prevent attacking owner
        if (ent.Comp.BoundOwner != null && args.Target == ent.Comp.BoundOwner)
            args.Cancel();
    }
}
