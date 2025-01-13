using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server._Impstation.CosmicCult.Components;

/// <summary>
/// After scanning, retrieves the target Uid to use with its related UI.
/// </summary>
/// <remarks>
/// Requires <c>ItemToggleComponent</c>.
/// </remarks>
[RegisterComponent, AutoGenerateComponentState]
[Access(typeof(CleanseDeconversionSystem))]
public sealed partial class CleanseOnUseComponent : Component
{
    [DataField]
    public TimeSpan UseTime = TimeSpan.FromSeconds(25);

    [DataField]
    public EntityUid? ScannedEntity;

    [DataField]
    public float MaxScanRange = 1.5f;

    [DataField]
    public SoundSpecifier? ScanningBeginSound;

    [DataField]
    public SoundSpecifier CleanseSound = new SoundPathSpecifier("/Audio/Items/Medical/healthscanner.ogg");

    [DataField]
    public EntProtoId CleanseVFX = "CleanseEffectVFX";
}
