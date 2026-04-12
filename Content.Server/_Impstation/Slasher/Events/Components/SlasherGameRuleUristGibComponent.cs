using Robust.Shared.Prototypes;

namespace Content.Server._Impstation.Slasher.Events;

/// <summary>
/// Configuration for the Urist gib effigy pulse rule.
/// </summary>
[RegisterComponent, Access(typeof(SlasherGameRuleUristGibSystem))]
public sealed partial class SlasherGameRuleUristGibComponent : Component
{
    /// <summary>Mob prototypes that can be used for the jump-scare spawn.</summary>
    [DataField]
    public List<EntProtoId> UristPrototypes { get; set; } = new()
    {
        "MobHuman",
        "MobMoth",
        "MobReptilian",
        "MobSlimePerson",
        "MobVulpkanin",
        "MobDwarf",
        "MobArachnid",
        "MobGray",
        "MobKodepiia",
    };

    /// <summary>Maximum tile offset from the chosen crew member.</summary>
    [DataField]
    public int SpawnRange { get; set; } = 3;

    /// <summary>How long the spawned Urist stays alive before being gibbed.</summary>
    [DataField]
    public TimeSpan GibDelay { get; set; } = TimeSpan.FromMilliseconds(100);
}
