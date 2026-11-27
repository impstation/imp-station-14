namespace Content.Server._Impstation.Anomaly.Components;

/// <summary>
/// Entities with this component cannot be injected by reagent anomalies. Used with <see cref="InjectionAnomalySystem"/>
/// </summary>
[RegisterComponent]
public sealed partial class AnomalyReagentInjectionImmuneComponent : Component { }
