using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared._Impstation.Heretic.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class MinionComponent : Component
{
    /// <summary>
    /// Indicates who the entity serves.
    /// </summary>
    [DataField, AutoNetworkedField] public EntityUid? BoundOwner;

    /// <summary>
    /// The faction that the minion should be added to.
    /// </summary>
    [DataField] public string MinionFaction = "Heretic";

    /// <summary>
    /// The name of the ghost role.
    /// </summary>
    /// <remarks>
    /// Keeping these around for whenever we have more minion types.
    /// </remarks>
    [DataField] public string GhostRoleName = "ghostrole-ghoul-name";

    /// <summary>
    /// The description of the ghost role.
    /// </summary>
    [DataField] public string GhostRoleDescription = "ghostrole-ghoul-desc";

    /// <summary>
    /// The rules for the ghost role.
    /// </summary>
    [DataField] public string GhostRoleRules = "ghostrole-ghoul-rules";

    /// <summary>
    /// The sound to play on briefing.
    /// </summary>
    [DataField] public SoundPathSpecifier BriefingSound = new("/Audio/_Goobstation/Heretic/Ambience/Antag/Heretic/heretic_gain.ogg");
}
