using Content.Shared._Impstation.Xenoarchaeology.Artifact.XAT.Systems;
using Robust.Shared.GameStates;

namespace Content.Shared._Impstation.Xenoarchaeology.Artifact.XAT.Components;

/// <summary>
/// This is used for a xenoarch trigger that activates when something is stunned nearby.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(XATStunSystem)), AutoGenerateComponentState]
public sealed partial class XATStunComponent : Component
{
    /// <summary>
    /// Range within which artifact going to listen to stunned event.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float Range = 6;
}
