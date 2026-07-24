namespace Content.Server._Impstation.Slasher.Components;

/// <summary>
/// Applied to Slashers once their shared effigy is destroyed.
/// While present, Slasher death no longer teleports and instead gibs.
/// </summary>
[RegisterComponent]
public sealed partial class SlasherEffigyFailureComponent : Component
{
}
