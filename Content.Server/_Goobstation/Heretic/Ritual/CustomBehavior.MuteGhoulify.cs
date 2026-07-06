using Content.Shared.Heretic.Prototypes;
using Content.Shared.Speech.Muting;
// imp start
using Content.Server.Heretic.EntitySystems;
using Content.Server.Popups;
using Content.Server._Goobstation.Heretic.Components;
using Content.Shared.NPC.Prototypes;
using Content.Shared.Popups;
using Content.Shared._Impstation.Heretic;
using Robust.Shared.Prototypes;
// imp end

namespace Content.Server.Heretic.Ritual;

public sealed partial class RitualMuteGhoulifyBehavior : RitualSacrificeBehavior
{

    // imp start
    private MinionSystem _minion = default!;
    private PopupSystem _popup = default!;
    private readonly ProtoId<NpcFactionPrototype> _hereticFaction = "Heretic";
    // imp end

    public override bool Execute(RitualData args, out string? outstr)
    {
        return base.Execute(args, out outstr);
    }

    public override void Finalize(RitualData args)
    {
        foreach (var uid in Uids)
        {
            var ghoul = new GhoulComponent()
            {
                HealthDivisor = 1.60, // imp edit
            };
            args.EntityManager.AddComponent(uid, ghoul, overwrite: true);
            args.EntityManager.EnsureComponent<MutedComponent>(uid);

            // imp. setup minion/faction comps
            var minion = args.EntityManager.EnsureComponent<MinionComponent>(uid);
            minion.BoundOwner = args.Performer;
            minion.FactionsToAdd.Add(_hereticFaction);
            _minion.ConvertEntityToMinion((uid, minion), true, true, true);
            var popupOthers = Loc.GetString("heretic-flesh-revive-finish");
            _popup.PopupEntity(popupOthers, uid, PopupType.LargeCaution);
        }
    }
}
