using Content.Shared.Chat;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Damage;
using Content.Shared.Humanoid;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Server._Impstation.CosmicCult.Components;

[RegisterComponent]
public sealed partial class CosmicGlyphComponent : Component
{

    [DataField] public int RequiredCultists = 1;

    [DataField] public float ActivationRange = 1f;

    /// <summary>
    ///     Damage dealt on glyph activation.
    /// </summary>
    [DataField] public DamageSpecifier? ActivationDamage;

    [DataField] public bool CanBeErased = true;

    public ProtoId<ReagentPrototype> HolyWater = "Holywater";

    [DataField] public EntProtoId GylphVFX = "CosmicGlyphVFX";

    [DataField] public SoundSpecifier GylphSFX = new SoundPathSpecifier("/Audio/_Impstation/CosmicCult/glyph_trigger.ogg");
}

public sealed class TryActivateGlyphEvent(EntityUid user, HashSet<EntityUid> cultists) : CancellableEntityEventArgs
{
    public EntityUid User = user;
    public HashSet<EntityUid> Cultists = cultists;
}

public sealed class AfterGlyphPlaced(EntityUid user)
{
    public EntityUid User = user;
}
