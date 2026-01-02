using Robust.Shared.GameStates;
using Content.Shared.Damage;
using Content.Shared.FixedPoint;

namespace Content.Shared._Impstation.Administration.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class SwordDamoclesComponent : Component
{
    /// <summary>
    ///     How many times has this smite been applied.
    /// </summary>
    [DataField]
    public int TimesApplied = 0;

    // <summary>
    //      How much damage this smite should do.
    // </summary>
    [DataField]
    public DamageSpecifier Damage = new()
    {
        DamageDict = new Dictionary<string, FixedPoint2>
        {
            {"Pierce", 30},
        },
    };
}
