using Content.Server._CD.Body.Systems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server._CD.Body.Components;

/// <summary>
/// Causes an allergic reaction when exposed to a reagent.
/// </summary>
[RegisterComponent, Access(typeof(AllergySystem))]
[AutoGenerateComponentPause]
public sealed partial class AllergyComponent : Component
{
    /// <summary>
    /// The next time that reagents will be metabolized.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoPausedField]
    public TimeSpan NextUpdate;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public Dictionary<string, FixedPoint2> Reagents = new();

    /// <summary>
    /// The reagent to generate on exposure to allergens.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public ProtoId<ReagentPrototype> ReactionReagent { get; private set; } = new("Histamine");
}
