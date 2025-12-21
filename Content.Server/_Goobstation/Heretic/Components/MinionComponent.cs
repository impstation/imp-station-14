using Content.Shared.NPC.Prototypes;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Server._Goobstation.Heretic.Components;

[RegisterComponent]
public sealed partial class MinionComponent : Component
{
    /// <summary>
    ///     Indicates who the entity serves.
    /// </summary>
    [DataField] public EntityUid? BoundOwner;

    [DataField] public string GhostRoleName = "ghostrole-ghoul-name";

    [DataField] public string GhostRoleDescription = "ghostrole-ghoul-desc";

    [DataField] public string GhostRoleRules = "ghostrole-ghoul-rules";

    /// <summary>
    ///     List of factions to add when converting to minion
    /// </summary>
    [DataField] public List<ProtoId<NpcFactionPrototype>> FactionsToAdd = [];

    [DataField] public string Briefing = "heretic-ghoul-greeting";

    [DataField] public SoundPathSpecifier BriefingSound = new("/Audio/_Goobstation/Heretic/Ambience/Antag/Heretic/heretic_gain.ogg");
}
