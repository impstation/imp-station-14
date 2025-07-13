using Content.Shared.DoAfter;
using Content.Shared.FixedPoint;
using Content.Shared.Storage;
using Robust.Shared.Audio;

namespace Content.Server._Impstation.Storage.Components;
[RegisterComponent]
public sealed partial class DelayedSpawnItemsOnUseComponent : Component
{
    /// <summary>
    ///     The list of entities to spawn, with amounts and orGroups.
    /// </summary>
    [DataField("items", required: true)]
    public List<EntitySpawnEntry> Items = new();

    /// <summary>
    ///     A sound to play when the items are spawned. For example, gift boxes being unwrapped.
    /// </summary>
    [DataField("sound")]
    public SoundSpecifier? Sound = null;

    /// <summary>
    ///     How many uses before the item should delete itself.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("uses")]
    public int Uses = 1;

    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("delay")]
    public float Delay = 1;
    /// <summary>
    /// if there is a popup message when trying to use the item
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("popupEnabled")]
    public bool PopUpEnabled = false;
    /// <summary>
    /// popup string.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("popupMessage")]
    public string PopUpMessage = "";

    // the following is lifted from doafterargs, this allows yaml users to have granular control over do after behaviour
    // defaults have been added

    #region Break/Cancellation Options
    /// <summary>
    ///     does the user need hands to drop the item
    /// </summary>
    [DataField]
    public bool NeedHand=true;

    /// <summary>
    ///     does the user stop if they change hands. also stops if they dont have a free hand
    /// </summary>
    [DataField]
    public bool BreakOnHandChange = true;

    /// <summary>
    ///     Does the user stop if they drop the item
    /// </summary>
    [DataField]
    public bool BreakOnDropItem = true;

    /// <summary>
    ///     does the user stop if they move
    /// </summary>
    [DataField]
    public bool BreakOnMove = true;


    /// <summary>
    ///     does the user stop if they are hurt
    /// </summary>
    [DataField]
    public bool BreakOnDamage = true;


    /// <summary>
    ///     does the user need to interact to use the item?
    /// </summary>
    [DataField]
    public bool RequireCanInteract = true;
    #endregion

    #region Duplicates
    /// <summary>
    ///     If true, this will prevent duplicate DoAfters from being started See also <see cref="DuplicateConditions"/>.
    /// </summary>
    /// <remarks>
    ///     Note that this will block even if the duplicate is cancelled because either DoAfter had
    ///     <see cref="CancelDuplicate"/> enabled.
    /// </remarks>
    [DataField]
    public bool BlockDuplicate = true;

    /// <summary>
    ///     If true, this will cancel any duplicate DoAfters when attempting to add a new DoAfter. See also
    ///     <see cref="DuplicateConditions"/>.
    /// </summary>
    [DataField]
    public bool CancelDuplicate = true;
    #endregion
}
