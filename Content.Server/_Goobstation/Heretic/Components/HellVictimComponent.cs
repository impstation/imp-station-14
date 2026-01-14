using Content.Shared.Heretic.Prototypes;

namespace Content.Server.Heretic.Components;

//for making them look like they went through some shit & adding debuffs
[RegisterComponent]
public sealed partial class HellVictimComponent : Component
{
    //components to add & message to send upon exit
    [ViewVariables]
    [DataField]
    public HereticSacrificeEffectPrototype Effect;
}
