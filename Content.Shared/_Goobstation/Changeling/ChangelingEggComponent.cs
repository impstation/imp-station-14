using Content.Shared.Store.Components;
using Robust.Shared.GameStates;

namespace Content.Shared._Goobstation.Changeling;

[RegisterComponent, NetworkedComponent]

public sealed partial class GoobChangelingEggComponent : Component
{
    public GoobChangelingComponent lingComp;
    public EntityUid lingMind;
    public StoreComponent lingStore;

    /// <summary>
    ///     Countdown before spawning monkey.
    /// </summary>
    public TimeSpan UpdateTimer = TimeSpan.Zero;
    public float UpdateCooldown = 60f;
    public bool active = false;
}
