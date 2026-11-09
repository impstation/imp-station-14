using Content.Shared.EntityTable.EntitySelectors;

namespace Content.Server.StationEvents.Components;

public sealed partial class RandomEntityStorageSpawnRuleComponent
{
    /// <summary>
    ///     A table of possible entities to spawn in the target storage.
    ///     This overrides <seealso cref="Prototype"/> if defined.
    /// </summary>
    [DataField]
    public EntityTableSelector? Table = null;
}
