using Content.Server._Goobstation.Changeling;
using Content.Server.Objectives.Systems;

namespace Content.Server._Goobstation.Objectives.Components;

[RegisterComponent, Access(typeof(GoobChangelingObjectiveSystem), typeof(GoobChangelingSystem))]
public sealed partial class GoobAbsorbConditionComponent : Component
{
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float Absorbed = 0f;
}
