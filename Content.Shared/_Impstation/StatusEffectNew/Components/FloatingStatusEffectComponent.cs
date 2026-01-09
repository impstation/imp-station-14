using Robust.Shared.GameStates;

namespace Content.Shared._Impstation.StatusEffectNew.Components;

/// <summary>
/// This is used for a status that makes an entity weightless when applied
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class FloatingStatusEffectComponent : Component;
