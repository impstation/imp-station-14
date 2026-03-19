using Robust.Shared.Prototypes;

namespace Content.Shared._Impstation.Mime;

// you may be wondering why i'm not just using SpellbookComponent. that's because it isn't actually made for learning actions permanently
// even though it has a datafield for that (it removes the actions from the spellbook and just leaves a blank book). so instead of refactoring
// it i'm making a new component. heart emoji
[RegisterComponent]
public sealed partial class MimeSpellbookComponent : Component
{
    /// <summary>
    /// The action to give to the user's mind.
    /// </summary>
    [DataField]
    [ViewVariables(VVAccess.ReadWrite)]
    public EntProtoId? Action;

    /// <summary>
    /// The length of the doafter.
    /// </summary>
    [DataField]
    [ViewVariables(VVAccess.ReadWrite)]
    public float LearnTime = .75f;

    /// <summary>
    /// Whether it will give a vow if the user is vowless.
    /// </summary>
    [DataField]
    public bool GivesVow;

    /// <summary>
    /// Whether it will delete itself after one use.
    /// </summary>
    [DataField]
    public bool OneUse;
}
