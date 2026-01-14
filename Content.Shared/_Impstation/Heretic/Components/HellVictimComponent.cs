using Content.Shared.Heretic.Prototypes;
using Content.Shared.StatusIcon;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Impstation.Heretic.Components;

//for making them look like they went through some shit & adding debuffs
[RegisterComponent, NetworkedComponent]
public sealed partial class HellVictimComponent : Component
{
    /// <summary>
    /// Contains the component to add and message to send upon hell exit
    /// </summary>
    [ViewVariables]
    [DataField]
    public HereticSacrificeEffectPrototype? Effect;

    /// <summary>
    /// the icon to add
    /// </summary>
    [DataField]
    public ProtoId<FactionIconPrototype> StatusIcon { get; set; } = "SacrificedFaction";
}
