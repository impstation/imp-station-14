using Content.Shared.Damage;
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
    public SoundSpecifier SizzleSound = new SoundPathSpecifier("/Audio/Effects/lightburn.ogg");

    [DataField]
    public SoundSpecifier CleanseSound = new SoundPathSpecifier("/Audio/_Impstation/CosmicCult/cleanse_deconversion.ogg");

    [DataField]
    public EntProtoId CleanseVFX = "CleanseEffectVFX";

    [DataField, AutoNetworkedField]
    public DamageSpecifier SelfDamage = new()
    {
        DamageDict = new() {
            { "Caustic", 15 }
        }
    };

}
