using Content.Shared.Radio;
using Robust.Shared.GameStates;

namespace Content.Shared._Impstation.CollectiveMind
{
    [RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
    public sealed partial class CollectiveMindComponent : Component
    {
        [DataField, AutoNetworkedField]
        public List<CollectiveMindPrototype> Minds;
    }
}
