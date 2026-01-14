using Content.Shared.EntityTable.EntitySelectors;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared.RatKing.Components;

/// <summary>
/// This is used for entities that can be
/// rummaged through by the rat king to get loot.
/// </summary>
[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState]
[AutoGenerateComponentPause] // imp
public sealed partial class RummageableComponent : Component
{
    /// <summary>
    /// Whether or not this entity has been rummaged through already.
    /// </summary>
    [DataField("looted"), ViewVariables(VVAccess.ReadWrite)]
    [AutoNetworkedField]
    public bool Looted;

    // imp add start
    /// <summary>
    /// Whether or not this entity can be rummaged through multiple times.
    /// </summary>
    [DataField]
    public bool Relootable;

    [DataField, AutoPausedField]
    public TimeSpan RelootableCooldown = TimeSpan.FromSeconds(60);
    public TimeSpan NextRelootable;
    // imp end

    /// <summary>
    /// How long it takes to rummage through a rummageable container.
    /// </summary>
    [DataField("rummageDuration"), ViewVariables(VVAccess.ReadWrite)]
    [AutoNetworkedField]
    public float RummageDuration = 3f;

    /// <summary>
    /// The entity table to select loot from.
    /// </summary>
    [DataField(required: true)]
    public EntityTableSelector Table = default!;

    /// <summary>
    /// Sound played on rummage completion.
    /// </summary>
    [DataField("sound")]
    public SoundSpecifier? Sound = new SoundCollectionSpecifier("storageRustle");

    // imp add start
    /// <summary>
    /// The context menu verb used for the rummage action.
    /// </summary>
    [DataField]
    public LocId RummageVerb = "verb-rummage-text";

    /// <summary>
    /// Rummage speed multiplier.
    /// </summary>
    [DataField]
    public float RummageModifier = 1f;
    // imp add end
}
