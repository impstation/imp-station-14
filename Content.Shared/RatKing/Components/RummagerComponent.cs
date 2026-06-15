using Robust.Shared.GameStates;
using Content.Shared.EntityTable.EntitySelectors; // imp

namespace Content.Shared.RatKing.Components;

/// <summary>
/// This is used for entities that can rummage through entities
/// with the <see cref="RummageableComponent"/>
/// </summary>
///
[RegisterComponent, NetworkedComponent]
public sealed partial class RummagerComponent : Component
{

    // imp add
    /// <summary>
    /// A weighted loot table.
    /// Defining this on the rummager so different things can get different stuff out of the same container type.
    /// Can be overridden on the entity with Rummageable.
    /// </summary>
    [DataField(required: true)]
    public EntityTableSelector? RummageLoot;
}
