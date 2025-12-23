using Content.Shared._Impstation.Xenoarchaeology.Artifact.XAT.Systems;
using Robust.Shared.GameStates;

namespace Content.Shared._Impstation.Xenoarchaeology.Artifact.XAT.Components;

/// <summary>
/// This is used for a xenoarch trigger that activates when something is resurrected nearby.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(XATResurrectionSystem)), AutoGenerateComponentState]
public sealed partial class XATResurrectionComponent : Component
{
    /// <summary>
    /// Range within which artifact going to listen to resurrection event.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float Range = 15;
}
