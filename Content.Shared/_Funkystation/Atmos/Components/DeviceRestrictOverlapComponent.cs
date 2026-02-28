using Content.Shared.Atmos.EntitySystems;

namespace Content.Shared._Funkystation.Atmos.Components; // Imp, moved to _Funkystation

/// <summary>
/// This is used for restricting anchoring devices so that they do not overlap.
/// </summary>
[RegisterComponent, Access(typeof(PipeRestrictOverlapSystem))]
public sealed partial class DeviceRestrictOverlapComponent : Component;
