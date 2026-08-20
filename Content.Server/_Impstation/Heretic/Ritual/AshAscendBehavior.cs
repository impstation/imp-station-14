using Content.Shared.Atmos.Components;

namespace Content.Server.Heretic.Ritual;

/// <summary>
/// Behavior for ash path ascensions. Inherits from <see cref="SacrificeBehavior"/>
/// </summary>
public sealed partial class AshAscendBehavior : SacrificeBehavior
{
    /// <summary>
    /// List of entities that meet the conditions for the ash ascension.
    /// </summary>
    private List<EntityUid> _usableUids = new();

    public override void Initialize()
    {
        base.Initialize();
    }

    /// <summary>
    /// Conditions check for the ash ascension.
    /// </summary>
    /// <param name="performer">Entity performing the ritual.</param>
    /// <param name="platform">The transmutation rune.</param>
    /// <returns>If the conditions succeed or not.</returns>
    public bool DoAshAscendRitual(EntityUid performer, EntityUid platform)
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
