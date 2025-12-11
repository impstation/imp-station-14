using Robust.Shared.Prototypes;

namespace Content.Server.GameTicking.Rules.Components;

[RegisterComponent, Access(typeof(SlasherRuleSystem))]
public sealed partial class SlasherRuleComponent : Component
{
    public readonly List<EntityUid> Minds = new();
}
