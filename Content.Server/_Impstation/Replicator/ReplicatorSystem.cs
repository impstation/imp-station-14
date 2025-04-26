// these are HEAVILY based on the Bingle free-agent ghostrole from GoobStation, but reflavored and reprogrammed to make them more Robust (and less of a meme.)
// all credit for the core gameplay concepts and a lot of the core functionality of the code goes to the folks over at Goob, but I re-wrote enough of it to justify putting it in our filestructure.
// the original Bingle PR can be found here: https://github.com/Goob-Station/Goob-Station/pull/1519

using Content.Server.Polymorph.Components;
using Content.Server.Polymorph.Systems;
using Content.Server.Popups;
using Content.Shared.CombatMode;
using Content.Shared.Interaction.Events;
using Content.Shared.Popups;
using Content.Shared._Impstation.Replicator;
using Robust.Server.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Content.Server.Ghost.Roles.Events;
using Content.Shared._Impstation.SpawnedFromTracker;



namespace Content.Server._Impstation.Replicator;

public sealed class ReplicatorSystem : EntitySystem
{
    [Dependency] private readonly PolymorphSystem _polymorph = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly AppearanceSystem _appearance = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ReplicatorComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<ReplicatorComponent, AttackAttemptEvent>(OnAttackAttempt);
        SubscribeLocalEvent<ReplicatorComponent, ToggleCombatActionEvent>(OnCombatToggle);
        SubscribeLocalEvent<ReplicatorComponent, GhostRoleSpawnerUsedEvent>(OnGhostRoleSpawnerUsed);
        SubscribeLocalEvent<ReplicatorComponent, ReplicatorSpawnNestActionEvent>(OnSpawnNestAction);
    }

    public readonly int MaxUpgradeStage = 2;

    private void OnMapInit(Entity<ReplicatorComponent> ent, ref MapInitEvent args)
    {
        var xform = Transform(ent);
        var coords = xform.Coordinates;

        if (!coords.IsValid(EntityManager) || xform.MapID == MapId.Nullspace)
            return;

        if (ent.Comp.Queen) // if you're the queen,
        {
            // spawn a nest, then make sure it has ReplicatorNestComponent
            var myNest = EnsureComp<ReplicatorNestComponent>(Spawn("ReplicatorNest", xform.Coordinates));
            // then add yourself to its list of replicators.
            myNest.SpawnedMinions.Add(ent);
        }
    }

    private void OnSpawnNestAction(Entity<ReplicatorComponent> ent, ref ReplicatorSpawnNestActionEvent args)
    {
        var xform = Transform(ent);
        var coords = xform.Coordinates;

        if (!coords.IsValid(EntityManager) || xform.MapID == MapId.Nullspace)
            return;

        // spawn a nest, then make sure it has ReplicatorNestComponent
        var myNest = EnsureComp<ReplicatorNestComponent>(Spawn("ReplicatorNest", xform.Coordinates));

        // then set that nest's spawned minions to our saved list of related replicators. Unfortunately first we need to deconstruct them, so:
        HashSet<EntityUid> newMinions = [];
        foreach (var (uid, _) in ent.Comp.RelatedReplicators)
        {
            newMinions.Add(uid);
        }
        myNest.SpawnedMinions = newMinions;
        // and we don't need the RelatedReplicators list anymore, so,
        ent.Comp.RelatedReplicators.Clear();

        // then we need to remove the action, to ensure it can't be used infinitely. Currently this is done in yml via temporary: true but i'm not sure that works.
    }

    private void OnGhostRoleSpawnerUsed(Entity<ReplicatorComponent> ent, ref GhostRoleSpawnerUsedEvent args)
    {
        if (!TryComp<SpawnedFromTrackerComponent>(args.Spawner, out var tracker) || !TryComp<ReplicatorNestComponent>(tracker.SpawnedFrom, out var nestComp))
            return;
        // add the spawned replicator to the nest's list when someone takes the ghostrole.
        nestComp.SpawnedMinions.Add(ent);
        // then remove the spawner from the nest's list of unclaimed spawners.
        nestComp.UnclaimedSpawners.Remove(args.Spawner);
    }

    public void UpgradeReplicator(Entity<ReplicatorComponent> ent)
    {
        if (ent.Comp.UpgradeStage == MaxUpgradeStage)
            return;

        var polyComp = EnsureComp<PolymorphableComponent>(ent);

        var upgradePolymorph = "ReplicatorUpgrade1";
        if (ent.Comp.UpgradeStage == 1)
            upgradePolymorph = "ReplicatorUpgrade2";

        _polymorph.CreatePolymorphAction(upgradePolymorph, (ent, polyComp));
        _popup.PopupEntity(Loc.GetString($"replicator-upgrade-t{ent.Comp.UpgradeStage + 1}-self"), ent, ent);
        _popup.PopupEntity(Loc.GetString($"replicator-upgrade-t{ent.Comp.UpgradeStage + 1}-others", ("replicator", Name(ent))), ent, Filter.Pvs(ent).RemovePlayersByAttachedEntity(ent), true, PopupType.MediumCaution);
    }

    public void OnAttackAttempt(Entity<ReplicatorComponent> ent, ref AttackAttemptEvent args)
    {
        // Can't attack your friends.
        if (HasComp<ReplicatorComponent>(args.Target))
        {
            _popup.PopupEntity(Loc.GetString("replicator-on-replicator-attack-fail"), ent, ent, PopupType.Medium);
            args.Cancel();
        }

        // Can't attack the nest.
        if (HasComp<ReplicatorNestComponent>(args.Target))
        {
            _popup.PopupClient(Loc.GetString("replicator-on-nest-attack-fail"), ent);
            args.Cancel();
        }
    }

    private void OnCombatToggle(Entity<ReplicatorComponent> ent, ref ToggleCombatActionEvent args)
    {
        if (!TryComp<CombatModeComponent>(ent, out var combat))
            return;

        // visual indicator that the replicator is aggressive. 
        _appearance.SetData(ent, ReplicatorVisuals.Combat, combat.IsInCombatMode);
    }
}
