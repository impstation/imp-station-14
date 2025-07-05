namespace Content.Shared._Impstation.BountyHunter;

/// <summary>
/// Component to prevent the parrot from attacking and allow them to pickpocket.
/// </summary>
[RegisterComponent, AutoGenerateComponentPause]
[Access(typeof(PacificationSystem))]
public sealed partial class BountyHunterParrotComponent : Component
{
}
