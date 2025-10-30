using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.SignalR;
using Assignment2.BusinessLogic;
using Assignment2.DataAccess.Models;
using Presentation.Hubs;

namespace Presentation.Pages.NewsArticleManagement
{
    public class IndexModel : PageModel
    {
        private readonly INewsArticleService _newsArticleService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IHubContext<NotificationHub> _hubContext;

        public IndexModel(
            INewsArticleService newsArticleService, 
            IHttpContextAccessor httpContextAccessor,
            IHubContext<NotificationHub> hubContext)
        {
            _newsArticleService = newsArticleService;
            _httpContextAccessor = httpContextAccessor;
            _hubContext = hubContext;
        }

        public List<Assignment2.DataAccess.Models.NewsArticle> Articles { get; set; } = new List<Assignment2.DataAccess.Models.NewsArticle>();
        
        public bool IsStaff => _httpContextAccessor.HttpContext?.Session.GetInt32("Role") == 1;

        public void OnGet()
        {
            Articles = _newsArticleService.GetAll().ToList();
        }

        public async Task<IActionResult> OnPostDelete(string id)
        {
            if (!IsStaff)
            {
                return Unauthorized();
            }

            var article = _newsArticleService.Get(id);
            if (article == null)
            {
                return NotFound();
            }

            var articleToDelete = _newsArticleService.Get(id);
            if (articleToDelete != null)
            {
                // Notify about article deletion
                await _hubContext.Clients.All.SendAsync("ArticleDeleted", id, articleToDelete.NewsTitle);
                
                // Notify dashboard about the deletion
                await _hubContext.Clients.Group("admin_dashboard").SendAsync("DashboardUpdate", new
                {
                    eventType = "delete",
                    entityType = "article",
                    message = $"Article deleted: \"{articleToDelete.NewsTitle}\"",
                    timestamp = DateTime.Now
                });
                
                _newsArticleService.Delete(id);
                TempData["SuccessMessage"] = "Article deleted successfully.";
            }
            return RedirectToPage();
        }
    }
}