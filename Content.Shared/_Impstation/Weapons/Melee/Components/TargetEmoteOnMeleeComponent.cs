using Content.Shared.Chat.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Shared._Impstation.Weapons.Melee.Components;

/// <summary>
/// Forces a living entity to automatically emote when hit with melee.
/// Different from EmoteOnDamage because it is applied to the Entity that hits, instead of the Entity that is damaged.
/// </summary>
[RegisterComponent]
public sealed partial class TargetEmoteOnMeleeComponent : Component
{
    [DataField]
    public ProtoId<EmotePrototype>? Emote;
}
