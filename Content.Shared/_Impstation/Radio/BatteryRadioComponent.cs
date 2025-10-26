namespace Content.Shared._Impstation.Radio;

/// <summary>
/// When active, will drain the power of attached battery-powered radio, and shut off when it runs out.
/// </summary>
[RegisterComponent]
public sealed partial class BatteryRadioComponent : Component
{
    /// <summary>
    /// Battery usage per second when enabled.
    /// </summary>
    [DataField]
    public float Wattage = 0.15f;
}

/// <summary>
/// Drains battery of attached battery-powered radio.
/// </summary>
[RegisterComponent]
public sealed partial class ActiveBatteryRadioComponent : Component
{
    [DataField]
    public bool UsingMicrophone = false;
    [DataField]
    public bool UsingSpeaker;
};
