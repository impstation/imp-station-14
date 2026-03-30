using Content.Server.Traitor.Systems;
using Robust.Shared.Prototypes;

namespace Content.Server.Traitor.Components;

/// <summary>
/// Makes the entity a traitor either instantly if it has a mind or when a mind is added.
/// </summary>
[RegisterComponent, Access(typeof(AutoTraitorSystem))]
public sealed partial class AutoTraitorComponent : Component
{
    /// <summary>
    /// The traitor profile to use
    /// </summary>
    [DataField]
    public EntProtoId Profile = "Traitor";

    /// <summary>
    /// #IMP Maximum number of times this can activate. Zero or less makes this infinite.
    /// This should usually be one, otherwise mindswap & polymorph causes objective issues (& other issues with things added on becoming antag)
    /// </summary>
    [DataField]
    public int MaxActivations = 1;

    /// <summary>
    /// #IMP total times activated
    /// If upstream ever touches AutoTraitor, we can probably let them clobber these, because they probably won't let anything past if it isn't a major rework.
    /// </summary>
    [DataField]
    public int NumActivations = 0;
}
