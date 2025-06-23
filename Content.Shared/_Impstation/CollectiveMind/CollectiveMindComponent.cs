using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared._Impstation.CollectiveMind
{
    [RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
    public sealed partial class CollectiveMindComponent : Component
    {
        [DataField(customTypeSerializer: typeof(PrototypeIdSerializer<CollectiveMindPrototype>)), AutoNetworkedField]
        public List<CollectiveMindPrototype> Minds;
    }
}
