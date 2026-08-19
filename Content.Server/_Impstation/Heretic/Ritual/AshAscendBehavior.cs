using Content.Shared.Atmos.Components;
using Content.Shared.Heretic.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Server.Heretic.Ritual;

public sealed partial class AshAscendBehavior : SacrificeBehavior
{
    private List<EntityUid> _usableUids = new();

    public override void Initialize()
    {
        base.Initialize();
    }

    public bool DoAshAscendRitual(EntityUid performer, EntityUid platform, ProtoId<HereticRitualPrototype> ritualId)
    {
        // Check for burning corpses.
        for (int i = 0; i < Max; i++)
        {
            if (EntityManager.TryGetComponent<FlammableComponent>(Uids[i], out var flame))
                if (flame.OnFire)
                    _usableUids.Add(Uids[i]);
        }

        // If the amount of burning corpses is less than the minimum required to execute the ritual, tell the performer.
        if (_usableUids.Count < Min)
        {
            Popup.PopupEntity(Loc.GetString("heretic-ritual-fail-sacrifice-ash"), platform, performer);
            return false;
        }

        // Reset _usableUids.
        _usableUids = [];
        return true;
    }
}
