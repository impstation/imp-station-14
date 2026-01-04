using Robust.Shared.GameStates;

namespace Content.Shared.Flash.Components;

/// <summary>
/// Makes the entity always vulnerable to being flashed.
/// </summary>
[RegisterComponent]
[Access(typeof(SharedFlashSystem))]
public sealed partial class FlashWeaknessComponent : Component
{

}
