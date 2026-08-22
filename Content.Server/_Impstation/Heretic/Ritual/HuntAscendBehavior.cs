using Content.Shared.Cuffs.Components;
using Robust.Shared.Serialization;

namespace Content.Server.Heretic.Ritual;

/// <summary>
/// Behavior for hunt path ascensions. Inherits from <see cref="SacrificeBehavior"/>
/// </summary>
[Serializable]
public sealed partial class HuntAscendBehavior : SacrificeBehavior
{
    /// <summary>
    /// List of entities that meet the conditions for the hunt ascension.
    /// </summary>
    private List<EntityUid> _usableUids = new();

    public override void Initialize()
    {
        base.Initialize();
    }

    /// <summary>
    /// Conditions check for the hunt ascension.
    /// </summary>
    /// <param name="performer">Entity performing the ritual.</param>
    /// <param name="platform">The transmutation rune.</param>
    /// <returns>If the conditions succeed or not.</returns>
    public bool DoHuntAscendRitual(EntityUid performer, EntityUid platform)
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
