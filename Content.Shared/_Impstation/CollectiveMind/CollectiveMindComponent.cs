using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Set;


namespace Content.Shared._Impstation.CollectiveMind
{
    [RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
    public sealed partial class CollectiveMindComponent : Component
    {
        [DataField(customTypeSerializer: typeof(PrototypeIdHashSetSerializer<CollectiveMindPrototype>)), AutoNetworkedField]
        public HashSet<string> Minds;
    }
}
