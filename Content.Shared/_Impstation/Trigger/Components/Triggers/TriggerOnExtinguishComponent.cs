using Content.Shared.Trigger.Components.Triggers;

namespace Content.Shared._Impstation.Trigger.Components.Triggers;

public sealed partial class TriggerOnExtinguishComponent : BaseTriggerOnXComponent
{
    /// <summary>
    /// The key that will trigger once extinguished
    /// </summary>
    [DataField, AutoNetworkedField]
    public string? KeyOut = "extinguished";
}
