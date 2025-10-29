using Microsoft.AspNetCore.SignalR;

namespace Presentation.Hubs
{
    public class NotificationHub : Hub
    {
        public async Task NotifyNewAccount(string message)
        {
            await Clients.All.SendAsync("ReceiveNewAccountNotification", message);
        }

        public async Task NotifyAccountDeactivated(string accountId)
        {
            await Clients.All.SendAsync("AccountDeactivated", accountId);
        }

        public async Task RegisterConnection(string accountId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"account_{accountId}");
        }
    }
}