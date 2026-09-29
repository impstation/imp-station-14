namespace Content.Server._Impstation.Pointing;

[RegisterComponent]
public sealed partial class PointingModifierComponent : Component
{
    /// <summary>
    ///     Verb shown in the popup to the pointer.
    ///     eg. "You POINT at it".
    /// </summary>
    [DataField]
    public LocId TextSelf = "point";

    /// <summary>
    ///     Verb shown in the popup to viewers.
    ///     eg. "The person POINTS at it".
    /// </summary>
    [DataField]
    public LocId TextOther = "points";

    /// <summary>
    ///     Name of the entity to spawn at point location.
    /// </summary>
    [DataField]
    public string PointingArrow = "PointingArrow";
}
