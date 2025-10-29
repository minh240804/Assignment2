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

        public List<NewsArticle> Articles { get; set; } = new List<NewsArticle>();
        
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
                await _hubContext.Clients.All.SendAsync("ArticleDeleted", id, articleToDelete.NewsTitle);
                await _hubContext.Clients.All.SendAsync("UpdateDashboardCounts");
                _newsArticleService.Delete(id);
                TempData["SuccessMessage"] = "Article deleted successfully.";
            }
            return RedirectToPage();
        }
    }
}