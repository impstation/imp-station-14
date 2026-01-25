using Content.Shared.Chemistry.Components;
using Robust.Shared.Audio;

namespace Content.Shared._Impstation.CleansDecals;

[RegisterComponent]
public sealed partial class CleansDecalsComponent : Component
{
    /// <summary>
    /// How long it takes to clean decals.
    /// </summary>
    [DataField]
    public float CleanDelay = 3.0f;

    /// <summary>
    /// The size of the area that will be cleaned.
    /// </summary>
    [DataField]
    public float CleanRadius = 0.5f;

    /// <summary>
    /// The sound to be played when completed.
    /// </summary>
    [DataField]
    public SoundSpecifier CleanSound = new SoundPathSpecifier("/Audio/Items/wirebrushing.ogg");

    /// <summary>
    /// The solution container that. Contains the solution to be used up.
    /// </summary>
    [DataField("solution", required: true)]
    public string SolutionContainer;

    /// <summary>
    /// How many units will be removed from the solution for each decal.
    /// </summary>
    [DataField]
    public int SolutionUsage = 5;
}
