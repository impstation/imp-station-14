using Content.Shared.IdentityManagement;
using Robust.Shared.GameStates;

namespace Content.Shared._Impstation.Examine;

/// <summary>
/// Alters the way that this object is referred to by examination.
/// For entities whose names on inspection should change slightly depending on how many there are.
/// Also for those whose names don't quite make sense: i.e. "a glass", "a grapes", "a spesos"
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(PluralNameSystem))]
public sealed partial class PluralNameComponent : Component
{
    /// <summary>
    /// How to count one of these.
    /// i.e. "a sheet of [glass]", "a pair of [glasses]".
    /// Defaults to the indefinite article. Can be empty.
    /// </summary>
    [DataField, AutoNetworkedField]
    public LocId OneOf = "plural-name-base";

    /// <summary>
    /// How to count multiple of these.
    /// Only necessary for stackable objects.
    /// i.e. "some panes of [glass]", "some [spesos]".
    /// defaults to "some".
    /// </summary>
    [DataField, AutoNetworkedField]
    public LocId SomeOf = "plural-name-some";

    /// <summary>
    /// Internal name set to one of the below.
    /// Overrides the name that appears when inspecting someone holding this.
    /// </summary>
    [DataField, AutoNetworkedField]
    public string OverrideName = string.Empty;

    /// <summary>
    /// What this object's name is when by itself. Highlighted.
    /// Compare it to what you'd say if you referred to "the" [object].
    /// i.e. "[a pane of] glass", "[a pair of] glasses", "[a] speso".
    /// defaults to the item's name.
    /// </summary>
    [DataField, AutoNetworkedField]
    public string NameOneOf = string.Empty;

    /// <summary>
    /// What this object's name is when multiple stack together. Highlighted.
    /// Compare it to what you'd call it if you referred to "these" [objects].
    /// Only necessary for stackable objects.
    /// i.e. "[some panes of] glass", "[some] spesos".
    /// defaults to the item's name.
    /// </summary>
    [DataField, AutoNetworkedField]
    public string NameSomeOf = string.Empty;


}
