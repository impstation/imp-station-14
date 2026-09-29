using Content.Server._Impstation.Speech.EntitySystems;

namespace Content.Server._Impstation.Speech.Components;

/// <summary>
/// French accent replaces spoken letters. "th" becomes "z" and "H" at the start of a word becomes "'".
/// </summary>
[RegisterComponent]
[Access(typeof(BasicFrenchAccentSystem))]
public sealed partial class BasicFrenchAccentComponent : Component {}
