using Robust.Shared.GameStates;

namespace Content.Shared.Weapons.Ranged.Components;

/// <summary>
/// Add/remove projectiles for guns that use cartridge-based projectiles.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class GunProjectileCountModifierComponent : Component
{
    /// <summary>
    /// The default projectile count modifier.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int ProjCount = 0;

    /// <summary>
    /// Checks if the projectile has a tag, if it does then it adds whatever count is defined instead of the default.
    /// Use this for different ammo types.
    /// </summary>
    [DataField, AutoNetworkedField]
    public Dictionary<string, int> ProjCountSpecific = new();
}
