using Content.Shared.Humanoid.Prototypes;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server._Impstation.Ghost.Components;

/// <summary>
/// A messed up fusion of GhostRoleMobSpawnerComponent and RandomHumanoidSpawnerComponent, letting ghost roles spawn as random humanoids.
/// </summary>
[RegisterComponent, EntityCategory("Spawner")]
public sealed partial class GhostRoleRandomHumanoidSpawnerComponent : Component
{
    /// <summary>
    /// The RandomHumanoidSettingsPrototype to use for this spawner.
    /// </summary>
    [DataField("settings", customTypeSerializer: typeof(PrototypeIdSerializer<RandomHumanoidSettingsPrototype>), required: true)]
    public string? SettingsPrototypeId;
}
