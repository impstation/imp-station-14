using Robust.Shared.Audio;

namespace Content.Server._Impstation.Slasher.Events;

/// <summary>
/// Configuration for the global revenant spook pulse rule.
/// </summary>
[RegisterComponent, Access(typeof(SlasherGameRuleRevenantSpookSystem))]
public sealed partial class SlasherGameRuleRevenantSpookComponent : Component
{
    /// <summary>Duration of the flash applied to affected crew.</summary>
    [DataField]
    public TimeSpan FlashDuration { get; set; } = TimeSpan.FromSeconds(2);

    /// <summary>Sound played when the pulse triggers.</summary>
    [DataField]
    public SoundSpecifier? HauntSound { get; set; } = new SoundCollectionSpecifier("RevenantHaunt");
}
