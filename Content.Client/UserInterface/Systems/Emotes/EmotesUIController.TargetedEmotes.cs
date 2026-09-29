using Content.Shared.Chat;
using Content.Shared.Chat.Prototypes;
using Robust.Shared.Input.Binding;

namespace Content.Client._Impstation.UserInterface.Systems.Emotes;

public sealed partial class EmotesUIController
{
    struct EmoteInfo
    {
        public EmotePrototype prototype;
        public NetEntity? emoteTarget;
    }

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EmoteInventorySlotEvent>(HandleClientsideEmote);
    }

    private bool HandleEmote(in PointerInputCmdHandler.PointerInputCmdArgs args)
    { // handles emote events for clientside entities
        ToggleEmotesMenu(false, args.EntityUid);
        return true;
    }

    private void HandleClientsideEmote(EmoteInventorySlotEvent args) // for emoting at clientside items, e.g. in an inventory slot
    {
        ToggleEmotesMenu(false, args.TargetUid);
    }
    public void OpenEmotesMenu(bool centered, EntityUid? emoteTarget = null) //for emoting at clientside items inside other UIs, e.g. rightclick dropdown
    {
        ToggleEmotesMenu(centered, emoteTarget);
    }

}
