using Content.Server._Impstation.Colonid.EntitySystems;
using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Serialization;

namespace Content.Server._Impstation.Colonid.Components;

/// <summary>
///     This component triggers an explosion on an entity if it is not wearing clothing with the suitEVA or airtight tags (airtight was made specifically for this component)
///     AND the entity is in an atmosphere containing the specified gas.
/// </summary>
[RegisterComponent, Access(typeof(ExplodeFromGasSystem))]
public sealed partial class ExplodeFromGasComponent : Component
{
    /// <summary>
    ///     How long the delay until the explosion after the entity is exposed to the gas.
    /// </summary>
    [DataField("explosionDelayLength"), ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan? ExplosionDelayLength;

    [DataField("triggeringGas"), ViewVariables(VVAccess.ReadWrite)]
    public string TriggeringGas;

    /// <summary>
    /// Individual gas entry data for populating the UI
    /// </summary>
    [Serializable, NetSerializable]
    public struct GasEntry
    {
        public readonly string Name;
        public readonly float Amount;
        public readonly string Color;

        public GasEntry(string name, float amount, string color)
        {
            Name = name;
            Amount = amount;
            Color = color;
        }

        public override string ToString()
        {
            // e.g. "Plasma: 2000 mol"
            return Loc.GetString(
                "gas-entry-info",
                 ("gasName", Name),
                 ("gasAmount", Amount));
        }
    }
}
