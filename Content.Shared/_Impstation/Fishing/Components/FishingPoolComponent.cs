using Content.Shared.EntityTable.EntitySelectors;

namespace Content.Shared._Impstation.Fishing.Components;

/// <summary>
/// Used to mark entities that are valid for fishing.
/// </summary>
[RegisterComponent]
public sealed partial class FishingPoolComponent : Component
{
    /// <summary>
    /// The entity table to select fishing pulls from.
    /// </summary>
    [DataField(required: true)]
    public EntityTableSelector Table = default!;
}
