using Content.Server._Impstation.Heretic.EntitySystems;
using Content.Server.Popups;
using Content.Shared._Impstation.Heretic;
using Content.Shared._Impstation.Heretic.Components;
using Content.Shared.Heretic.Prototypes;
using Content.Shared.Popups;
using Content.Shared.Speech.Muting;
using Robust.Shared.Prototypes;

namespace Content.Server.Heretic.Ritual;

public sealed partial class MuteGhoulifyBehavior : SacrificeBehavior
{
    [Dependency] private readonly MinionSystem _minion = default!;
    [Dependency] private readonly PopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();
    }

    public void DoMuteGhoulifyRitualEffect(EntityUid performer, EntityUid platform, ProtoId<HereticRitualPrototype> ritualId)
    {
        // Make ghoul.
        foreach (var uid in Uids)
        {
            var ghoul = new GhoulComponent()
            {
                HealthDivisor = 1.60
            };
            EntityManager.AddComponent(uid, ghoul, overwrite: true);
            EntityManager.EnsureComponent<MutedComponent>(uid);

            // Convert the entity into a ghoul.
            var minion = EntityManager.EnsureComponent<MinionComponent>(uid);
            minion.BoundOwner = performer;
            _minion.ConvertEntityToMinion((uid, minion), true);

            // Show a big popup to everyone in the vicinity.
            var popupOthers = Loc.GetString("heretic-flesh-revive-finish");
            _popup.PopupEntity(popupOthers, uid, PopupType.LargeCaution);
        }

        Uids = [];
    }
}
