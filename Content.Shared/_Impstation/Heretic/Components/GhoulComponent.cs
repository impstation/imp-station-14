using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Content.Shared.Heretic;

[RegisterComponent, NetworkedComponent]
public sealed partial class GhoulComponent : Component
{
    /// <summary>
    ///     What a ghoul's health is divided by.
    /// </summary>
    [DataField] public FixedPoint2 HealthDivisor = 4;

    /// <summary>
    ///     Maximum health used for ghoul health calculations.
    /// </summary>
    [DataField] public FixedPoint2 MaxHealth = 200;

    /// <summary>
    ///     Fallback for if dead threshold is null.
    /// </summary>
    [DataField] public FixedPoint2 FallbackHealth = 50;
}
