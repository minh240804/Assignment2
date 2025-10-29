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

        public async Task JoinArticleGroup(string articleId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"article_{articleId}");
        }

        public async Task LeaveArticleGroup(string articleId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"article_{articleId}");
        }

        public async Task NotifyArticleUpdate(string articleId, string title, string content)
        {
            await Clients.Group($"article_{articleId}").SendAsync("ArticleUpdated", articleId, title, content);
        }

        public async Task NotifyArticleDeleted(string articleId, string title)
        {
            await Clients.All.SendAsync("ArticleDeleted", articleId, title);
        }

        public async Task NotifyNewArticle(string authorName, string articleTitle)
        {
            await Clients.All.SendAsync("NewArticlePublished", authorName, articleTitle);
        }

        public async Task UpdateDashboardCounts()
        {
            await Clients.All.SendAsync("UpdateDashboardCounts");
        }
    }
}