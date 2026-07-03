namespace Content.Server._Impstation.Objectives.Components;

/// <summary>
/// Objective condition: place a minimum number of meathooks this round.
/// Progress is rule.MeathookCount / Required.
/// </summary>
[RegisterComponent]
public sealed partial class SlasherMeathookConditionComponent : Component
{
    /// <summary>How many meathooks must be placed to complete this objective.</summary>
    [DataField] public int Required { get; set; } = 2;
}

/// <summary>
/// Objective condition: place the effigy at least once this round.
/// Progress is 1.0 once the rule considers the effigy placed.
/// </summary>
[RegisterComponent]
public sealed partial class SlasherEffigyConditionComponent : Component { }

/// <summary>
/// Objective condition: feed the required number of soul fragments into the effigy.
/// Progress is rule.FragmentInsertions / rule.TargetInsertions.
/// </summary>
[RegisterComponent]
public sealed partial class SlasherFeedEffigyConditionComponent : Component { }

/// <summary>
/// Flavor objective marker used to localize the "do not kill" objective text at assignment time.
/// </summary>
[RegisterComponent]
public sealed partial class SlasherDoNotKillFlavorConditionComponent : Component { }
