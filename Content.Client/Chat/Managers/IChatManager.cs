using Content.Shared.Chat;

namespace Content.Client.Chat.Managers
{
    public interface IChatManager : ISharedChatManager
    {
        event Action PermissionsUpdated; // Starlight - Collective Mind


        public void SendMessage(string text, ChatSelectChannel channel);
        public void UpdatePermissions(); // Starlight - Collective Mind
    }
}
