using Content.Shared._Impstation.Xenoarchaeology.Artifact.XAE.Systems;
using Robust.Shared.GameStates;

namespace Content.Shared._Impstation.Xenoarchaeology.Artifact.XAE.Components;

[RegisterComponent, Access(typeof(XAEScrambleDNASystem)), NetworkedComponent, AutoGenerateComponentState]
public sealed partial class XAEScrambleDNAComponent : Component
{
    [DataField, AutoNetworkedField]
    public float Radius = 6f;

    [DataField, AutoNetworkedField]
    public int Count = 1;
}
