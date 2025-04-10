using Content.Shared.Damage;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Utility;

namespace Content.Shared._Impstation.Fishing;

// I have tried to make this as generic as possible but "delete joint on cycle / right-click reels in" is very specific behavior.
[RegisterComponent]
//Imp : Basically a copy of GrapplingGunComponent
public sealed partial class FishingTackleComponent : Component
{
    /// <summary>
    ///     A modifier to the amount of damage the fishing rod will do.
    /// </summary>
    [DataField(required: true)]
    [ViewVariables(VVAccess.ReadWrite)]
    public DamageSpecifier Damage = new();
}
