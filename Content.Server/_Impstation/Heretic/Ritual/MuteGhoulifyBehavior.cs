using Content.Server._Impstation.Heretic.EntitySystems;
using Content.Server.Popups;
using Content.Shared._Impstation.Heretic;
using Content.Shared._Impstation.Heretic.Components;
using Content.Shared.Popups;
using Content.Shared.Speech.Muting;
using Robust.Shared.Serialization;

namespace Content.Server.Heretic.Ritual;

/// <summary>
/// Behavior for creating mute ghouls. Inherits from <see cref="SacrificeBehavior"/>
/// </summary>
[Serializable]
public sealed partial class MuteGhoulifyBehavior : SacrificeBehavior
{
    [Dependency] private readonly MinionSystem _minion = default!;
    [Dependency] private readonly PopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();
    }

    /// <summary>
    /// Turns the entity being sacrificed into a mute ghoul.
    /// </summary>
    /// <param name="performer">Entity performing the ritual.</param>
    public void DoMuteGhoulifyRitualEffect(EntityUid performer)
    {
        // Make ghoul.
        foreach (var uid in Uids)
        {
            var ghoul = new GhoulComponent()
            {
                // 125 health, for mobs with 200 health baseline.
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

        // Reset the list of UIDs.
        Uids = [];
    }
}
