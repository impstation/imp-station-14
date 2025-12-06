using Robust.Shared.GameStates;

namespace Content.Shared.Weapons.Ranged.Components;

/// <summary>
/// Add/remove projectiles for guns that use ammo
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class GunProjectileCountModifierComponent : Component
{
    [DataField, AutoNetworkedField]
    public int ProjCount = 0;
}
