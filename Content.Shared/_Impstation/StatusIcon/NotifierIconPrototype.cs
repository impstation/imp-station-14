using Content.Shared.StatusIcon;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Array;


namespace Content.Shared._Impstation.StatusIcon;

[Prototype]
public sealed partial class NotifierIconPrototype : StatusIconPrototype, IInheritingPrototype
{
    /// <inheritdoc />
    [ParentDataField(typeof(AbstractPrototypeIdArraySerializer<NotifierIconPrototype>))]
    public string[]? Parents { get; private set; }

    /// <inheritdoc />
    [NeverPushInheritance]
    [AbstractDataField]
    public bool Abstract { get; private set; }
}
