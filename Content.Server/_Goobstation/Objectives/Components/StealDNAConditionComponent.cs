using Content.Server._Goobstation.Changeling;

namespace Content.Server._Goobstation.Objectives.Components;

[RegisterComponent, Access(typeof(GoobChangelingObjectiveSystem), typeof(GoobChangelingSystem))]
public sealed partial class GoobStealDNAConditionComponent : Component
{
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float DNAStolen = 0f;
}

public class GoobChangelingObjectiveSystem
{
}
