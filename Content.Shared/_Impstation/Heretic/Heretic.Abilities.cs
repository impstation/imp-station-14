using Content.Shared.Actions;
using Robust.Shared.GameStates;

namespace Content.Shared.Heretic;

[RegisterComponent, NetworkedComponent]

#region Abilities

//Hunt
public sealed partial class HereticSpawnWatchtowerEvent : InstantActionEvent { }
public sealed partial class HereticSerpentsFocusEvent : InstantActionEvent { }

#endregion
