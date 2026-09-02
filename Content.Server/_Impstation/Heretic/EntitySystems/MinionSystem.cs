using Content.Server._Goobstation.Heretic.UI;
using Content.Server.Antag;
using Content.Server.EUI;
using Content.Server.Ghost.Roles.Components;
using Content.Server.Roles;
using Content.Shared.Ghost.Roles.Components;
using Content.Shared.IdentityManagement;
using Content.Shared.Mind;
using Content.Shared.NPC.Systems;
using Content.Shared.Roles;
using Content.Shared.Roles.Components;
using Content.Shared._Impstation.Heretic.Components;
using Content.Shared._Impstation.Heretic.EntitySystems;
using Robust.Shared.Player;

namespace Content.Server._Impstation.Heretic.EntitySystems;

/// <summary>
/// Handles minions summoned by Heretics, such as ghouls. Used with <see cref"MinionComponent"/>
/// </summary>
public sealed partial class MinionSystem : SharedMinionSystem
{
    [Dependency] private readonly AntagSelectionSystem _antag = default!;
    [Dependency] private readonly EuiManager _euiMan = default!;
    [Dependency] private readonly ISharedPlayerManager _playerManager = default!;
    [Dependency] private readonly NpcFactionSystem _faction = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly SharedRoleSystem _role = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MinionComponent, TakeGhostRoleEvent>(OnTakeGhostRole);
    }

    /// <summary>
    /// Handles converting an entity to a minion.
    /// </summary>
    /// <param name="ent">The minion's MinionComp, and its associated entity.</param>
    /// <param name="createGhostRole">If the conversion should create a ghost role.</param>
    public void ConvertEntityToMinion(Entity<MinionComponent> ent, bool createGhostRole)
    {
        // Check if the entity has a mind.
        var hasMind = _mind.TryGetMind(ent, out var mindID, out _);

        // If the entity has a mind, send them briefing text and a popup.
        if (hasMind == true)
        {
            SendBriefing(ent);

            if (_playerManager.TryGetSessionByEntity(ent, out var session))
                _euiMan.OpenEui(new GhoulNotifEui(), session);
        }

        // In any case, make it sentient and a familiar.
        _mind.MakeSentient(ent);
        _role.MindAddRole(mindID, "MindRoleGhostRoleFamiliar");

        // If the entity doesn't have a mind, and we want it to become a ghost role, give it the necessary things to become a ghost role.
        if (!hasMind && createGhostRole == true)
        {
            var ghostRole = EnsureComp<GhostRoleComponent>(ent);
            ghostRole.RoleName = Loc.GetString(ent.Comp.GhostRoleName);
            ghostRole.RoleDescription = Loc.GetString(ent.Comp.GhostRoleDescription);
            ghostRole.RoleRules = Loc.GetString(ent.Comp.GhostRoleRules);
        }

        // If it doesn't have a mind, and isn't an entity that spawns another entity on ghost takeover, allow ghosts to take over the entity.
        if (!hasMind && !HasComp<GhostRoleMobSpawnerComponent>(ent))
            EnsureComp<GhostTakeoverAvailableComponent>(ent);

        // Clear the entity's factions and add the faction defined in MinionComponent (Heretic, by default)
        _faction.ClearFactions((ent, null));
        _faction.AddFaction((ent, null), ent.Comp.MinionFaction);
    }

    /// <summary>
    /// Handles sending the briefing text to the minion, as well as adding role components.
    /// </summary>
    /// <param name="ent">The minion's MinionComp, and its associated entity.</param>
    private void SendBriefing(Entity<MinionComponent> ent)
    {
        // String to be used as briefing text.
        string brief;

        // If the entity has no owner, then use the no-name greeting, otherwise address the summoner by name.
        if (ent.Comp.BoundOwner == null)
            brief = Loc.GetString("heretic-minion-greeting-noname");
        else
            brief = Loc.GetString("heretic-minion-greeting", ("ent", Identity.Entity((EntityUid)ent.Comp.BoundOwner, EntityManager)));

        _antag.SendBriefing(ent, brief, Color.MediumPurple, ent.Comp.BriefingSound);

        EnsureComp<GhoulRoleComponent>(ent);

        // Make sure the minion has RoleBriefingComp, and set its text to the briefing text.
        EnsureComp<RoleBriefingComponent>(ent, out var rolebrief);
        rolebrief.Briefing = brief;
    }

    /// <summary>
    /// Called when a ghost takes a ghost role minion.
    /// </summary>
    /// <param name="ent">The minion's MinionComp, and its associated entity</param>
    /// <param name="ev">Event called when a ghost takes a ghost role.</param>
    private void OnTakeGhostRole(Entity<MinionComponent> ent, ref TakeGhostRoleEvent ev)
    {
        // Make sure the ghost taking the entity gets these.
        _mind.MakeSentient(ent);
        _role.MindAddRole(ent, "MindRoleGhostRoleFamiliar");

        SendBriefing(ent);
    }
}
