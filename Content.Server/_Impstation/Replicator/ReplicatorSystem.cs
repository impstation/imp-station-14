// these are HEAVILY based on the Bingle free-agent ghostrole from GoobStation, but reflavored and reprogrammed to make them more Robust (and less of a meme.)
// all credit for the core gameplay concepts and a lot of the core functionality of the code goes to the folks over at Goob, but I re-wrote enough of it to justify putting it in our filestructure.
// the original Bingle PR can be found here: https://github.com/Goob-Station/Goob-Station/pull/1519

using Content.Server.Polymorph.Components;
using Content.Server.Polymorph.Systems;
using Content.Server.Popups;
using Content.Shared.CombatMode;
using Content.Shared.Interaction.Events;
using Content.Shared.Popups;
using Robust.Server.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Player;

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
    }

    public readonly int MaxUpgradeStage = 1;

    private void OnMapInit(Entity<ReplicatorComponent> ent, ref MapInitEvent args)
    {
        if (ent.Comp.MyNest != null)
            return;

        var xform = Transform(ent);
        var coords = xform.Coordinates;

        if (!coords.IsValid(EntityManager) || xform.MapID == MapId.Nullspace)
            return;

        if (ent.Comp.Queen)
            ent.Comp.MyNest = Spawn("ReplicatorNest", xform.Coordinates);
        else // TODO: make this significantly less fuckstupid. this is GARBAGE code holy shit
        {
            var query = EntityQueryEnumerator<ReplicatorComponent>();
            while (query.MoveNext(out var queryUid, out var _))
            {
                if (xform.Coordinates == Transform(queryUid).Coordinates)
                    ent.Comp.MyNest = queryUid;
            }
        }
    }

    public void UpgradeReplicator(Entity<ReplicatorComponent> ent)
    {
        if (ent.Comp.UpgradeStage == MaxUpgradeStage)
            return;

        var polyComp = EnsureComp<PolymorphableComponent>(ent);

        switch (ent.Comp.UpgradeStage)
        {
            case 0:
                _polymorph.CreatePolymorphAction("ReplicatorUpgrade1", (ent, polyComp));
                _popup.PopupEntity(Loc.GetString("replicator-upgrade-t1-self"), ent, ent);
                _popup.PopupEntity(Loc.GetString("replicator-upgrade-t1-others", ("replicator", Name(ent))), ent, Filter.Pvs(ent).RemovePlayersByAttachedEntity(ent), true, PopupType.MediumCaution);
                break;
            case 1:
                break;
        }
    }

    public void OnAttackAttempt(Entity<ReplicatorComponent> ent, ref AttackAttemptEvent args)
    {
        if (HasComp<ReplicatorComponent>(args.Target))
        {
            _popup.PopupEntity(Loc.GetString("replicator-on-replicator-attack-fail"), ent, ent, PopupType.Medium);
            args.Cancel();
        }

/*         if (HasComp<ReplicatorNestComponent>(args.Target))
        {
            _popup.PopupClient(Loc.GetString("replicator-on-nest-attack-fail"), ent);
            args.Cancel();
        } */
    }

    private void OnCombatToggle(Entity<ReplicatorComponent> ent, ref ToggleCombatActionEvent args)
    {
        if (!TryComp<CombatModeComponent>(ent, out var combat))
            return;
        _appearance.SetData(ent, ReplicatorVisuals.Combat, combat.IsInCombatMode);
    }
}
