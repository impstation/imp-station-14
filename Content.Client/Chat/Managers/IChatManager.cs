using Content.Shared.Chat;

namespace Content.Client.Chat.Managers
{
    public interface IChatManager : ISharedChatManager
    {
        // imp add
        /// <summary>
        /// Will refresh permissions.
        /// </summary>
        event Action PermissionsUpdated;

        public void UpdatePermissions(); // imp add

        public void SendMessage(string text, ChatSelectChannel channel);
    }
}
