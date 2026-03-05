using Content.Shared.Chat.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._ES.StatusEffects.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class ESEmoteOnAppliedStatusEffectComponent : Component
{
    /// <summary>
    ///     The emote to play when this status effect is applied.
    /// </summary>
    [DataField(required: true)]
    public ProtoId<EmotePrototype> Emote = default!;
}
