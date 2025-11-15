using Content.Shared.NPC.Prototypes;
using Content.Shared.Weapons.Reflect;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Impstation.Weapons.Redirect;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ProjectileRedirectorComponent : Component
{
    /// <summary>
    /// What we reflect.
    /// </summary>
    [DataField]
    public ReflectType Reflects = ReflectType.Energy | ReflectType.NonEnergy;

    /// faction that redirected bullets won't be redirected towards.
    /// so you don't just shoot yourself every time.
    /// </summary>
    [DataField]
    public ProtoId<NpcFactionPrototype> IgnoreFaction = "Heretic";

    /// <summary>
    /// The sound to play when redirecting.
    /// </summary>
    [DataField]
    public SoundSpecifier? SoundOnRedirect = new SoundPathSpecifier("/Audio/Weapons/Guns/Hits/laser_sear_wall.ogg", AudioParams.Default.WithVariation(0.05f));

    /// <summary>
    /// Probability for a projectile to be redirected.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float RedirectProb = 1.0f;

    /// <summary>
    /// Radius in which the redirector will look for targets.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float RedirectRadius = 10.0f;
}
