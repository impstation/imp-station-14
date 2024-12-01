using Robust.Shared.GameStates;

namespace Content.Shared._Impstation.Cosmiccult.Components;

/// <summary>
/// Component used for allowing non-humans to be converted. (Mainly monkeys)
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(SharedCosmicCultSystem))]
public sealed partial class AlwaysCosmicCultConvertibleComponent : Component
{

}
