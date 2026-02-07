using Content.Shared.Atmos.EntitySystems; // Funky RPD, moved from server to shared

namespace Content.Shared.Atmos.Components; // Funky RPD, moved from server to shared

/// <summary>
/// This is used for restricting anchoring pipes so that they do not overlap.
/// </summary>
[RegisterComponent, Access(typeof(PipeRestrictOverlapSystem))]
public sealed partial class PipeRestrictOverlapComponent : Component;
