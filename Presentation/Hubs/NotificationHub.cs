using System.Security.Claims;
using Assignment2.DataAccess.Models;
using Microsoft.AspNetCore.SignalR;

namespace Presentation.Hubs
{
    public class NotificationHub : Hub
    {
        private const string Group_Admin = "Admin";
        private const string Group_Staff = "Staff";
        private const string Group_Lecturer = "Lecturer";

        private static string? MapRole(int role) => role switch
        {
            0 => Group_Admin,
            1 => Group_Staff,
            2 => Group_Lecturer,
            _ => null
        };

        private static string? GetRoleGroupFromContext(HubCallerContext ctx)
        {
            var roleStr = ctx.User?.FindFirst(ClaimTypes.Role)?.Value
                          ?? ctx.User?.FindFirst("Role")?.Value;
            if (!int.TryParse(roleStr, out var role)) return null;
            return MapRole(role);
        }

        public override async Task OnConnectedAsync()
        {
            var roleGroup = GetRoleGroupFromContext(Context);
            if (!string.IsNullOrEmpty(roleGroup))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, roleGroup);
            }
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var roleGroup = GetRoleGroupFromContext(Context);
            if (!string.IsNullOrEmpty(roleGroup))
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, roleGroup);
            }
            await base.OnDisconnectedAsync(exception);
        }

        public async Task<string> RegisterUserRole(int role)
        {
            var grp = MapRole(role);
            if (!string.IsNullOrEmpty(grp))
                await Groups.AddToGroupAsync(Context.ConnectionId, grp);
            return grp ?? string.Empty; 
        }
        public async Task UnregisterUserRole(int role)
        {
            var grp = MapRole(role);
            if (!string.IsNullOrEmpty(grp))
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, grp);
        }

        public async Task<string> RegisterConnection(string accountId)
        {
            var grp = $"account_{accountId}";
            await Groups.AddToGroupAsync(Context.ConnectionId, grp);
            return grp; 
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
