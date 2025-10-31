using System.Security.Claims;
using Assignment2.DataAccess.Models;
using Microsoft.AspNetCore.SignalR;

namespace Presentation.Hubs
{
    public class NotificationHub : Hub
    {
        // Tên group theo vai trò
        private const string Group_Admin = "Admin";
        private const string Group_Staff = "Staff";
        private const string Group_Lecturer = "Lecturer";

        // Map số role -> tên group
        private static string? MapRole(int role) => role switch
        {
            0 => Group_Admin,
            1 => Group_Staff,
            2 => Group_Lecturer,
            _ => null
        };

        // Lấy role từ Claims (ưu tiên ClaimTypes.Role; fallback "Role")
        private static string? GetRoleGroupFromContext(HubCallerContext ctx)
        {
            var roleStr = ctx.User?.FindFirst(ClaimTypes.Role)?.Value
                          ?? ctx.User?.FindFirst("Role")?.Value; // nếu bạn tự lưu claim "Role"
            if (!int.TryParse(roleStr, out var role)) return null;
            return MapRole(role);
        }

        // Tự join group theo claim nếu có
        public override async Task OnConnectedAsync()
        {
            var roleGroup = GetRoleGroupFromContext(Context);
            if (!string.IsNullOrEmpty(roleGroup))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, roleGroup);
            }
            await base.OnConnectedAsync();
        }

        // Tự rời group theo claim nếu có
        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var roleGroup = GetRoleGroupFromContext(Context);
            if (!string.IsNullOrEmpty(roleGroup))
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, roleGroup);
            }
            await base.OnDisconnectedAsync(exception);
        }

        // ====== APIs client sẽ invoke sau login/logout ======

        // Join group theo ROLE (được client gọi trong registerRoleAndAccount)
        public async Task<string> RegisterUserRole(int role)
        {
            var grp = MapRole(role);
            if (!string.IsNullOrEmpty(grp))
                await Groups.AddToGroupAsync(Context.ConnectionId, grp);
            return grp ?? string.Empty; // ACK cho client log
        }

        // Rời group theo ROLE (được client gọi trước khi logout)
        public async Task UnregisterUserRole(int role)
        {
            var grp = MapRole(role);
            if (!string.IsNullOrEmpty(grp))
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, grp);
        }

        // Join group theo ACCOUNT (để force logout đúng người dùng)
        public async Task<string> RegisterConnection(string accountId)
        {
            var grp = $"account_{accountId}";
            await Groups.AddToGroupAsync(Context.ConnectionId, grp);
            return grp; // ACK cho client log
        }

        // Join/Leave group theo ARTICLE (realtime viewers)
        public Task JoinArticleGroup(string articleId)
            => Groups.AddToGroupAsync(Context.ConnectionId, $"article_{articleId}");

        public Task LeaveArticleGroup(string articleId)
            => Groups.RemoveFromGroupAsync(Context.ConnectionId, $"article_{articleId}");

        // ====== Broadcast helpers cho nghiệp vụ ======

        // Accounts
        public Task NotifyNewAccount(string accName)
            => Clients.Group(Group_Admin)
                      .SendAsync("ReceiveNewAccountNotification", $"Admin has added a new account: {accName}");

        public Task AccountDeactivated(string accountId)
            => Clients.Groups(Group_Admin, Group_Staff, Group_Lecturer)
                      .SendAsync("AccountDeactivated", accountId);

        public Task ForceLogoutAccount(string accountId, string? reason = null)
            => Clients.Group($"account_{accountId}")
                      .SendAsync("ForceLogout", new { reason = reason ?? "account_deleted" });

        // Categories
        public Task NotifyCreateCategory(string message)
            => Clients.Groups(Group_Admin, Group_Staff)
                      .SendAsync("ReceiveCreateCategoryNotification", message);

        // Articles
        public Task NotifyNewArticle(string authorName, string articleTitle)
            => Clients.Groups(Group_Admin, Group_Staff, Group_Lecturer)
                      .SendAsync("NewArticlePublished", authorName, articleTitle);

        public Task NotifyArticleUpdate(string articleId, string title, string content)
            => Task.WhenAll(
                Clients.Group($"article_{articleId}")
                       .SendAsync("ArticleUpdated", articleId, title, content),
                Clients.Group(Group_Admin).SendAsync("UpdateNewsArticle", articleId),
                Clients.Group(Group_Staff).SendAsync("UpdateNewsArticle", articleId)
            );

        public Task NotifyArticleDeleted(string articleId, string title)
            => Clients.Groups(Group_Admin, Group_Staff, Group_Lecturer)
                      .SendAsync("ArticleDeleted", articleId, title);
        public Task TagCreated(string articleId, string title, string note)
            => Clients.Groups(Group_Admin, Group_Staff, Group_Lecturer)
                      .SendAsync("TagCreated", title);

        public Task TagDeleted(string articleId, string title)
            => Clients.Groups(Group_Admin, Group_Staff, Group_Lecturer)
                      .SendAsync("TagDeleted", articleId, title);

        public Task TagUpdated(string articleId, string title, string note)
            => Clients.Groups(Group_Admin, Group_Staff, Group_Lecturer)
                      .SendAsync("TagUpdated", title);
        public Task UpdateDashboardCounts()
            => Clients.Group(Group_Admin).SendAsync("UpdateDashboardCounts");

        // (Optional) formatters - dùng khi cần tạo message đẹp
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
