using Content.Shared.Damage;
using Content.Shared.Humanoid;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._Impstation.Gastropoids.SnailShell;

/// <summary>
/// Grants the entity with this component the ability to curl up in its shell, applying a temporary (breakable) damage barrier.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(SnailShellSystem)), AutoGenerateComponentState]
public sealed partial class SnailShellComponent : Component
{
    /// <summary>
    /// The entity needed to perform the action. Granted upon the creation of the entity.
    /// </summary>
    [DataField(required: true), AutoNetworkedField]
    public EntProtoId Action;

    [DataField, AutoNetworkedField]
    public EntityUid? ActionEntity;

    /// <summary>
    /// Damage modifiers granted to this entity when shell is activated.
    /// </summary>
    [DataField, AutoNetworkedField]
    public DamageModifierSet DamageModifier = default!;
    public bool Active = false;
    public bool Broken = false;

    /// <summary>
    /// Layers on the entity to be declared as 'shell', these will not be hidden when the entity goes into the shell
    /// </summary>
    public HashSet<HumanoidVisualLayers> ShellLayers = [HumanoidVisualLayers.Tail, HumanoidVisualLayers.TailBehind, HumanoidVisualLayers.TailBehindBackpack, HumanoidVisualLayers.TailOversuit, HumanoidVisualLayers.TailUnderlay]; // hoo boy

    /// <summary>
    /// Popup text for when the action fails due to the shell being broken.
    /// </summary>
    public LocId BrokenPopup = "snailshell-failure-broken";

    /// <summary>
    /// Popup text for when the shell breaks.
    /// </summary>
    public LocId BreakPopup = "snailshell-break";

    /// <summary>
    /// Sound the shell makes when you go in it.
    /// </summary>
    [DataField, AutoNetworkedField]
    public SoundSpecifier ShellActivateSound = new SoundPathSpecifier("/Audio/Weapons/block_metal1.ogg"); // FIX LATER

    /// <summary>
    /// Sound the shell makes when hit.
    /// </summary>
    [DataField, AutoNetworkedField]
    public SoundSpecifier ShellHitSound = new SoundPathSpecifier("/Audio/Weapons/block_metal1.ogg"); // FIX LATER

    /// <summary>
    /// Sound the shell makes when broken.
    /// </summary>
    [DataField, AutoNetworkedField]
    public SoundSpecifier ShellBreakSound = new SoundPathSpecifier("/Audio/Effects/metal_break1.ogg"); // FIX LATER
}

[Serializable, NetSerializable]
public enum ShellVisuals : byte
{
    Base,
    On
}
