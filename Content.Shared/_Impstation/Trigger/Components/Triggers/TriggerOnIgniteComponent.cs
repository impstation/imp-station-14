using Content.Shared.Trigger.Components.Triggers;

namespace Content.Shared._Impstation.Trigger.Components.Triggers;

public sealed partial class TriggerOnIgniteComponent :  BaseTriggerOnXComponent
{
    /// <summary>
    /// The key that will trigger once ignited
    /// </summary>
    [DataField, AutoNetworkedField]
    public string? KeyOut = "ignited";
}
