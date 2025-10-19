using Robust.Shared.GameStates;
using Robust.Shared.Prototypes; //imp
using Content.Shared.Chat.TypingIndicator; //imp

namespace Content.Shared.Speech.Components;
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState] //imp autogeneratecomponentstate
public sealed partial class RatvarianLanguageComponent : Component
{

    [DataField("proto"), AutoNetworkedField] //imp
    public ProtoId<TypingIndicatorPrototype> OldIndicator = "default"; //imp
}
