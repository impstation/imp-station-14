using Robust.Shared.Prototypes;

namespace Content.Server._Impstation.Spawners.Components;

/// <summary>
/// A spawner that will spawn an entity once at a random spawner.
/// </summary>
[RegisterComponent, EntityCategory("Spawner")]
public sealed partial class LinkedSpawnerComponent : Component
{
    /// <summary>
    /// The entity that will be spawned.
    /// </summary>
    [DataField(required: true)]
    public EntProtoId Prototype;
}
