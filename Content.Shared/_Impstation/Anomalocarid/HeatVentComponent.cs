using Content.Shared.Alert;
using Content.Shared.Atmos;
using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Robust.Shared.Audio;
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
    ///     True when the entity gains a mind.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool MindActive;

    /// <summary>
    ///     How much heat this entity has stored up.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float HeatStored;

    /// <summary>
    ///     Amount of heat that can be stored.
    ///     At max value the entity starts taking damage.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float HeatDamageThreshold = 60f * 20f; // 20 minutes (1200)

    /// <summary>
    ///     How much heat should be added per cycle.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float HeatAdded = 5f;

    /// <summary>
    ///     Heat stored is multiplied by this number to get gas temperature.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float GasTempHeatMultiplier = 0.15f;

    /// <summary>
    ///     Gas temperature base.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float GasTempBase = 293.15f;

    /// <summary>
    ///     Gas temp minimum after being added to the base
    /// </summary>
    [DataField, AutoNetworkedField]
    public float GasTempMin = 293.15f;

    /// <summary>
    ///     Gas temp maximum after being added to the base
    /// </summary>
    [DataField, AutoNetworkedField]
    public float GasTempMax = 473.15f;

    public TimeSpan UpdateTimer = TimeSpan.Zero;

    /// <summary>
    ///     In seconds, time between cycles.
    /// </summary>
    public TimeSpan UpdateCooldown = TimeSpan.FromSeconds(5);

    /// <summary>
    ///     In seconds, minimum doafter length.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float VentLengthMin = 1.5f;

    /// <summary>
    ///     In seconds, maximum doafter length.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float VentLengthMax = 10f;

    /// <summary>
    ///     Damage taken per cycle at maximum heat capacity.
    /// </summary>
    [DataField]
    public DamageSpecifier HeatDamage = new()
    {
        DamageDict = new Dictionary<string, FixedPoint2>
        {
            {"Heat", 3},
            {"Blunt", 1.5},
        },
    };

    /// <summary>
    ///     Action used to vent heat.
    /// </summary>
    public EntProtoId VentAction = "ActionVentHeat";

    /// <summary>
    ///     Coefficient used to determine length of doafter.
    ///     This value is multiplied by HeatStored.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float VentLengthMultiplier = 0.006f;

    /// <summary>
    ///     Gas to vent.
    /// </summary>
    [DataField]
    public Gas VentGas = Gas.WaterVapor;

    /// <summary>
    ///     How many moles of gas are released per amount of heat stored.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float MolesPerHeatStored = 0.02f;

    /// <summary>
    ///     Popup when using the vent heat action.
    /// </summary>
    [DataField]
    public LocId VentStartPopup = "anomalocarid-vent-start";

    /// <summary>
    ///     Popup when the vent heat doafter completes.
    /// </summary>
    [DataField]
    public LocId VentDoAfterPopup = "anomalocarid-vent-doafter";

    /// <summary>
    ///     Sound to play when vent heat doafter completes.
    /// </summary>
    [DataField]
    public SoundSpecifier VentSound = new SoundPathSpecifier("/Audio/_Impstation/Anomalocarids/pressure_release.ogg");

    /// <summary>
    ///     Sound to play when vent heat doafter completes.
    /// </summary>
    [DataField]
    public ProtoId<AlertPrototype> Alert = "InternalPressure";

    /// <summary>
    ///     Locids of text that pops up when you're far too hot.
    /// </summary>
    [DataField]
    public List<LocId>? TooHotPopups = [];

    /// <summary>
    ///     Chance toohotpopups occur every update over heat threshold.
    /// </summary>
    [DataField]
    public float TooHotPopupChance = 0.50f;
}
