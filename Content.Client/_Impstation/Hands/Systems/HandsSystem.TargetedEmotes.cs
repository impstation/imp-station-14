using Content.Shared.Chat;

namespace Content.Client.Hands.Systems // Has to be upstream namespaced due to being sealed partial
{
    public sealed partial class HandsSystem
    {
        public void UIInventoryEmote(string handName)
        {
            if (!TryGetPlayerHands(out var hands) ||
                !TryGetHeldItem(hands.Value.AsNullable(), handName, out var heldEntity))
            {
                return;
            }

            RaiseLocalEvent(new EmoteInventorySlotEvent(heldEntity.Value));
        }
    }
}
