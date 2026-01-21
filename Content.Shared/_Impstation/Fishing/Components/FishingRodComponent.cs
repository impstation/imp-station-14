using Robust.Shared.GameStates;
using Robust.Shared.Utility;

namespace Content.Shared._Impstation.Fishing.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class FishingRodComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntityUid? Holder;

    [DataField, AutoNetworkedField]
    public EntityUid? TargetPool;

    [DataField, AutoNetworkedField]
    public EntityUid? Bobber;

    [DataField]
    public string BobberSlotId = "bobber_slot";

    [DataField]
    public bool HasBite;

    [DataField]
    public TimeSpan NextPullTime = TimeSpan.Zero;

    [DataField]
    public TimeSpan MinPullTime = TimeSpan.FromSeconds(3);

    [DataField]
    public TimeSpan MaxPullTime = TimeSpan.FromSeconds(6);

    [DataField]
    public TimeSpan PullWindow = TimeSpan.FromSeconds(2.5);

    [DataField]
    public float PullChance = 0.3f;

    [DataField]
    public float PullStrength = 0.6f;

    [DataField]
    public SpriteSpecifier LineSprite = new SpriteSpecifier.Rsi(new ResPath("_Impstation/Objects/Fishing/Rods/fishingrod.rsi"), "rope");
}
