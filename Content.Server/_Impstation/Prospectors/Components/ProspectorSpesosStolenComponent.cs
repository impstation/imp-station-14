namespace Content.Server.Objectives.Components;

[RegisterComponent]
public sealed partial class ProspectorSpesosStolenComponent : Component
{
    [DataField] public int Stolen = 0;
}
