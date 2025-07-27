using Robust.Shared.GameStates;
using Content.Server.Xenoarchaeology.Equipment.Systems;

namespace Content.Server.Xenoarchaeology.Equipment.Components;
//#IMP: Moved from Shared to Server for reason of allowing it to access old Xenoarch.

/// <summary>
///     Suppress artifact activation, when entity is placed inside this container.
/// </summary>
[RegisterComponent] //#IMP can't have network as this isn't in shared
public sealed partial class SuppressArtifactContainerComponent : Component;
