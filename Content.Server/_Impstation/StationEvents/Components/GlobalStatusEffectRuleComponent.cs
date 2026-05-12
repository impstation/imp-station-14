using Content.Server._Impstation.StationEvents.Events;
using Robust.Shared.Prototypes;

namespace Content.Server._Impstation.StationEvents.Components;

[RegisterComponent, Access(typeof(GlobalStatusEffectRule))]
public sealed partial class GlobalStatusEffectRuleComponent : Component
{
    [DataField(required: true)]
    public EntProtoId StatusEffect;

    [DataField]
    public float? MinEffectDuration = null;

    [DataField]
    public float? MaxEffectDuration = null;

    [DataField]
    public LocId? Announcement = null;
}
