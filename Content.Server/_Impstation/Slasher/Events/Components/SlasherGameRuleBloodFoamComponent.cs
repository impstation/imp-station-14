using Content.Shared.Chemistry.Reagent;
using Robust.Shared.Prototypes;

namespace Content.Server._Impstation.Slasher.Events;

/// <summary>
/// Configuration for the blood foam effigy pulse rule.
/// </summary>
[RegisterComponent, Access(typeof(SlasherGameRuleBloodFoamSystem))]
public sealed partial class SlasherGameRuleBloodFoamComponent : Component
{
    /// <summary>Fallback reagent used if no whitelist entries are provided.</summary>
    [DataField]
    public ProtoId<ReagentPrototype> ReagentId { get; set; } = "Blood";

    /// <summary>Chance per vent to emit blood foam.</summary>
    [DataField]
    public float VentProcChance { get; set; } = 0.1f;

    /// <summary>Possible blood reagents to randomize between for each vent.</summary>
    [DataField]
    public List<ProtoId<ReagentPrototype>> BloodReagentWhitelist { get; set; } = new()
    {
        "Blood",
        "CopperBlood",
        "InsectBlood",
        "GrayBlood",
        "BloodKodepiia",
        "ShimmeringBlood",
    };

    /// <summary>Units of reagent per vent.</summary>
    [DataField]
    public int ReagentQuantity { get; set; } = 50;

    /// <summary>Tile spread radius for each foam cloud.</summary>
    [DataField]
    public int Spread { get; set; } = 8;

    /// <summary>How long each foam cloud persists, in seconds.</summary>
    [DataField]
    public float FoamTime { get; set; } = 15f;
}
