using Robust.Shared.GameStates;

/// <summary>
/// This component is for items that can have their name and description changed when cleaned by soap.
/// </summary>
namespace Content.Shared.Forensics.Components
{
    [RegisterComponent, NetworkedComponent]
    public sealed partial class CleanableInfoComponent : Component
    {
        [DataField]
        public string? CleanedName { get; set; }

        [DataField]
        public string? CleanedDescription { get; set; }
    }
}
