namespace Content.Server.Ghost.Components
{
    /// <summary>
    /// Entities with this component will be added to the ghost warp menu.
    /// Intended for non-players and non-locations.
    /// </summary>
    [RegisterComponent]
    public sealed partial class MiscGhostWarpComponent : Component
    {
        /// <summary>
        /// Optional datafield to specify a name for the warp menu.
        /// </summary>
        [DataField("displayName")] public string? DisplayName;
    }
}
