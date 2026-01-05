using Content.Shared.Atmos;

namespace Content.Server._Impstation.Atmos.Components;
/// <summary>
/// Generic version of the gas miner component. this is mainly for entities that produce gas but don't want the ui part of the gas miners
/// </summary>
public sealed partial class GasProducerComponent : Component
{
    /// <summary>
    /// if gas is being produced
    /// </summary>
    [DataField]
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// gas to spawn
    /// </summary>
    [DataField(required: true)]
    public Gas SpawnGas;

    /// <summary>
    /// temperature to spawn the gas at
    /// </summary>
    [DataField]
    public float SpawnTemperature = Atmospherics.T20C;

    /// <summary>
    /// amount of moles to spawn
    /// </summary>
    [DataField]
    public float SpawnAmount = Atmospherics.MolesCellStandard * 20f;

    /// <summary>
    ///      maximum pressure in the environment before production stops
    /// </summary>
    [DataField]
    public float MaxExternalPressure = Atmospherics.HazardHighPressure;

    /// <summary>
    ///      maximum amount of mols in the environment before production stops
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField]
    public float MaxExternalAmount = float.PositiveInfinity;
}
