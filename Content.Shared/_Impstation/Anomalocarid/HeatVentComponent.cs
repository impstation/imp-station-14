using Content.Shared.Atmos;
using Content.Shared.Damage;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Impstation.Anomalocarid;

/// <summary>
///     Component which allows an entity to accumulate heat over time.
///     If enough heat is accumulated, the entity will begin to take damage.
///     Heat can be vented through use of an action.
/// </summary>
[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState]
public sealed partial class HeatVentComponent : Component
{
    /// <summary>
    ///     How much heat this entity has stored up.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float HeatStored = 0f;

    /// <summary>
    ///     Amount of heat that can be stored.
    ///     At max value the entity starts taking damage.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float MaxHeat = 30f;

    /// <summary>
    ///     How much heat should be added per cycle.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float HeatAdded = 1f;

    public TimeSpan UpdateTimer = TimeSpan.Zero;

    /// <summary>
    ///     In seconds, time between cycles.
    /// </summary>
    public float UpdateCooldown = 1f;

    /// <summary>
    ///     Damage taken per cycle at maximum heat capacity.
    /// </summary>
    [DataField]
    public DamageSpecifier HeatDamage = new()
    {
        DamageDict = new()
        {
            {"Heat", 3},
        }
    };

    /// <summary>
    ///     Action used to vent heat.
    /// </summary>
    public EntProtoId VentAction = "ActionVentHeat";

    /// <summary>
    ///     Coefficient used to determine length of doafter.
    ///     This value is multiplied by HeatStored.
    /// </summary>
    public float VentLengthMultiplier = 0.2f;

    /// <summary>
    ///     Gas to vent.
    /// </summary>
    public Gas VentGas = Gas.WaterVapor;

    /// <summary>
    ///     How many moles of gas are released per amount of heat stored.
    /// </summary>
    public float MolesPerHeatStored = 7f;

    public LocId VentStartPopup = "anomalocarid-vent-start";

    public LocId VentDoAfterPopup = "anomalocarid-vent-doafter";
}
