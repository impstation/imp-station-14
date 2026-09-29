using Content.Server.Chemistry.EntitySystems;
using Content.Shared.Random;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Content.Shared.Chemistry.Reagent; // imp
using Robust.Shared.Prototypes; // imp

namespace Content.Server.Chemistry.Components;

/// <summary>
///     Fills a solution container randomly using a weighted random prototype
/// </summary>
[RegisterComponent, Access(typeof(SolutionRandomFillSystem))]
public sealed partial class RandomFillSolutionComponent : Component
{
    /// <summary>
    ///     Solution name which to add reagents to.
    /// </summary>
    [DataField("solution")]
    public string Solution { get; set; } = "default";

    /// <summary>
    ///     Weighted random fill prototype Id. Used to pick reagent and quantity.
    /// </summary>
    [DataField("weightedRandomId", /*required: true, */customTypeSerializer: typeof(PrototypeIdSerializer<WeightedRandomFillSolutionPrototype>))] // imp edit, technically not required anymore
    public string? WeightedRandomId;

    // imp edit start
    /// <summary>
    ///     Pick ANY reagent (scary)
    /// </summary>
    [DataField]
    public bool TrueRandomMode;

    /// <summary>
    ///     Blacklist for reagents for True Random Mode.
    /// </summary>
    [DataField]
    public List<ProtoId<ReagentPrototype>> BlacklistedReagents = new();

    /// <summary>
    ///     Whitelist for reagents for True Random Mode (for horseradish pills even if foods are in the blacklisted groups)
    /// </summary>
    [DataField]
    public List<ProtoId<ReagentPrototype>> WhitelistedReagents = new();

    /// <summary>
    ///     Reagent groups blacklisted for True Random Mode.
    /// </summary>
    [DataField]
    public List<string> BlacklistedGroups = new();

    /// <summary>
    ///     The minimum amount of a reagent there can be in True Random Mode.
    /// </summary>
    [DataField]
    public int RandomAmountMin = 5;

    /// <summary>
    ///     The maximum amount of a reagent there can be in True Random Mode.
    /// </summary>
    [DataField]
    public int RandomAmountMax = 20;

    /// <summary>
    ///     The amount the quantity steps up in True Random Mode.
    /// </summary>
    [DataField]
    public int RandomAmountStep = 5;
    // imp edit end
}
