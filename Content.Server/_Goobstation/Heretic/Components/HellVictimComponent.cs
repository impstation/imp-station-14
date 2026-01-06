using Content.Shared.Traits;
using Robust.Shared.Prototypes;

namespace Content.Server.Heretic.Components;

//for making them look like they went through some shit & adding debuffs
[RegisterComponent]
public sealed partial class HellVictimComponent : Component
{
    //list of possible traits to add
    public List<ProtoId<TraitPrototype>> Traits = ["Blindness", "Scrambled", "Hemophilia", "Muted", "PainNumbness", "Pacifist", "ImpairedMobility", "Narcolepsy"];
}
