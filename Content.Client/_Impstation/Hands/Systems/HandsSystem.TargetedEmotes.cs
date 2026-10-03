using Content.Shared.Chat;

namespace Content.Client._Impstation.Hands.Systems
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
