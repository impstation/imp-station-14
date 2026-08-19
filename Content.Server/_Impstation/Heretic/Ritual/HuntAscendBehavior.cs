using Content.Shared.Cuffs.Components;
using Content.Shared.Heretic.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Server.Heretic.Ritual;

public sealed partial class HuntAscendBehavior : SacrificeBehavior
{
    /// <summary>
    /// List of entities which meet the requirements for the ritual.
    /// </summary>
    private List<EntityUid> _usableUids = new();

    public override void Initialize()
    {
        base.Initialize();
    }

    public bool DoHuntAscendRitual(EntityUid performer, EntityUid platform, ProtoId<HereticRitualPrototype> ritualId)
    {
        // This is effectively just the ash ascencion but with cuffed corpses.
        // Check for cuffed corpses.
        for (int i = 0; i < Max; i++)
        {
            if (EntityManager.TryGetComponent<CuffableComponent>(Uids[i], out var cuff))
                if (!cuff.CanStillInteract)
                    _usableUids.Add(Uids[i]);
        }

        // If the amount of cuffed corpses is less than the minimum required to execute the ritual, tell the performer.
        if (_usableUids.Count < Min)
        {
            Popup.PopupEntity(Loc.GetString("heretic-ritual-fail-sacrifice-hunt"), platform, performer);
            return false;
        }

        // Reset _usableUids.
        _usableUids = [];
        return true;
    }
}
