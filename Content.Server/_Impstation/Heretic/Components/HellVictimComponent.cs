using Content.Shared.Heretic.Prototypes;

namespace Content.Server._Impstation.Heretic.Components;

//for making them look like they went through some shit & adding debuffs
[RegisterComponent]
public sealed partial class HellVictimComponent : Component
{
    /// <summary>
    /// Contains the component to add and message to send upon hell exit
    /// </summary>
    [ViewVariables]
    [DataField]
    public HereticSacrificeEffectPrototype? Effect;
}
