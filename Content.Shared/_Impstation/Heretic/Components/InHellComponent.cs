using Content.Shared.Cloning;
using Content.Shared.Heretic.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Impstation.Heretic.Components;

/// <summary>
///     for tracking the subject's trip through hell.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentPause]
public sealed partial class InHellComponent : Component
{
    /// <summary>
    /// when will they leave hell?
    /// </summary>
    [ViewVariables]
    [AutoPausedField]
    public TimeSpan ExitHellTime = default!;

    /// <summary>
    /// where do we put the mind when we're done?
    /// </summary>
    [DataField]
    public EntityUid OriginalBody;

    [DataField]
    public EntityUid? Mind;

    /// <summary>
    /// how long are they there?
    /// </summary>
    [DataField]
    public TimeSpan HellDuration = TimeSpan.FromSeconds(15);

    [DataField]
    public ProtoId<CloningSettingsPrototype> CloneSettings = "HellClone";

    /// <summary>
    /// contains the sacrifice debuff and related message
    /// </summary>
    [DataField]
    public HereticSacrificeEffectPrototype? Effect = null;
}
