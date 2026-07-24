namespace Content.Server._Impstation.Slasher.Components;

/// <summary>
/// Marks this pinpointer as tracking the Slasher's active effigy.
/// The <see cref="SlasherEffigySystem"/> keeps it pointed at the current effigy automatically.
/// </summary>
[RegisterComponent, Access(typeof(SlasherEffigySystem))]
public sealed partial class SlasherEffigyPinpointerComponent : Component { }
