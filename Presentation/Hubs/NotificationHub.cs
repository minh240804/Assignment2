using Assignment2.DataAccess.Models;
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

        public async Task SendComment(string articleId, string user, string message, DateTime timestamp)
        {
            await Clients.Group($"article_{articleId}").SendAsync("ReceiveComment", new
            {
                user = user,
                message = message,
                timestamp = timestamp.ToString("HH:mm:ss dd/MM/yyyy")
            });
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

        public async Task ForceLogoutAccount(string accountId, string? reason = null)
        {
            await Clients.Group($"account_{accountId}")
                .SendAsync("ForceLogout", new { reason = reason ?? "account_deleted" });
                }
        public async Task NotifyCreateCategory(string message)
        {
            await Clients.All.SendAsync("ReceiveCreateCategoryNotification", message);
        }

        public async Task NotifyCategoryReload()
        {
            await Clients.All.SendAsync("ReloadCategoryList");
        }

        private static string BuildArticleUpdatedMsg(NewsArticle art, string updaterName)
        {
            var title = string.IsNullOrWhiteSpace(art.NewsTitle) ? (art.Headline ?? "(No title)") : art.NewsTitle;
            return $" Article updated: {title} — by {updaterName} at {DateTime.Now:HH:mm dd/MM}.";
        }

        private static string BuildArticleDeletedMsg(NewsArticle art, string actorName)
        {
            var title = string.IsNullOrWhiteSpace(art.NewsTitle) ? (art.Headline ?? "(No title)") : art.NewsTitle;
            return $" Article deleted: {title} — by {actorName} at {DateTime.Now:HH:mm dd/MM}.";
        }

    }
}