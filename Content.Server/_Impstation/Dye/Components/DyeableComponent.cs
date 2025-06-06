using Robust.Shared.Prototypes;

namespace Content.Server._Impstation.Dye.Components;

[RegisterComponent]
/// <summary>
/// Allows an entity with this component to be dyed in a washing machine.
/// </summary>
public sealed partial class DyeableComponent : Component
{
    [DataField]
    /// <summary>
    /// Registers special dye recipes that can be used to convert the dyeable item into a new entity. Colour here is stored as a string.
    /// </summary>
    public Dictionary<string, EntProtoId> Recipes;

    [DataField]
    /// <summary>
    /// Set to false to only allow special recipes.
    /// </summary>
    public bool AcceptAnyColor = true;
}
