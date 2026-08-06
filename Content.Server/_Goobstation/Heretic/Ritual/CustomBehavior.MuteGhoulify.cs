using Content.Shared._Impstation.Heretic;
using Content.Shared.Heretic.Prototypes;
using Content.Shared.Speech.Muting;
using Content.Server.Popups;
using Content.Shared.Popups;
using Content.Shared._Impstation.Heretic.Components;
using Content.Server._Impstation.Heretic.EntitySystems;

namespace Content.Server.Heretic.Ritual;

public sealed partial class RitualMuteGhoulifyBehavior : RitualSacrificeBehavior
{
    private MinionSystem _minion = default!;
    private PopupSystem _popup = default!;

    public override bool Execute(RitualData args, out string? outstr)
    {
        // Why do I have to do it like this.
        // Why do I have to do it like this.
        // Why do I have to do it like this.
        // - Salamander.
        _minion = args.EntityManager.System<MinionSystem>();
        _popup = args.EntityManager.System<PopupSystem>();
        return base.Execute(args, out outstr);
    }

    public override void Finalize(RitualData args)
    {
        foreach (var uid in Uids)
        {
            var ghoul = new GhoulComponent()
            {
                HealthDivisor = 1.60
            };
            args.EntityManager.AddComponent(uid, ghoul, overwrite: true);
            args.EntityManager.EnsureComponent<MutedComponent>(uid);

            // Convert the entity into a ghoul.
            var minion = args.EntityManager.EnsureComponent<MinionComponent>(uid);
            minion.BoundOwner = args.Performer;
            _minion.ConvertEntityToMinion((uid, minion), true);

            // Show a big popup to everyone in the vicinity.
            var popupOthers = Loc.GetString("heretic-flesh-revive-finish");
            _popup.PopupEntity(popupOthers, uid, PopupType.LargeCaution);
        }
    }
}
