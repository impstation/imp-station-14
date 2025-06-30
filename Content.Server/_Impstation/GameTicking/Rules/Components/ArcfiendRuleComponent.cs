using Content.Shared.Random;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Server.GameTicking.Rules.Components;

/// <summary>
/// Stores data for <see cref="ArcfiendRuleSystem"/>.
/// </summary>
[RegisterComponent, Access(typeof(ArcfiendRuleSystem))]
public sealed partial class ArcfiendRuleComponent : Component
{
    public readonly List<EntityUid> ArcfiendMinds = new();
}
