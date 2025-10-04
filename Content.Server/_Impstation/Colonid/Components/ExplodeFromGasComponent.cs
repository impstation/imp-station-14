using Content.Server._Impstation.Colonid.EntitySystems;

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
}