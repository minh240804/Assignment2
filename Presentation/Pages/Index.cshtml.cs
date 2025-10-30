using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Assignment2.BusinessLogic;
using Assignment2.DataAccess.Models;
using NewsArticleModel = Assignment2.DataAccess.Models.NewsArticle;

namespace Assignment2.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;
        private readonly INewsArticleService _newsArticleService;

        public IndexModel(ILogger<IndexModel> logger, INewsArticleService newsArticleService)
        {
            _logger = logger;
            _newsArticleService = newsArticleService;
        }

        public List<NewsArticleModel> Articles { get; set; } = new List<NewsArticleModel>();

        [BindProperty(SupportsGet = true)]
        public string? Search { get; set; }

        [BindProperty(SupportsGet = true)]
        public int CurrentPage { get; set; } = 1;

        public int TotalPages { get; set; }
        private const int PageSize = 6;

        public void OnGet()
        {
            // Get only published articles (NewsStatus = true)
            var allArticles = _newsArticleService.GetAll(status: true);

            // Apply search filter if provided
            if (!string.IsNullOrWhiteSpace(Search))
            {
                allArticles = allArticles.Where(a =>
                    (a.NewsTitle ?? "").Contains(Search, StringComparison.OrdinalIgnoreCase) ||
                    (a.Headline ?? "").Contains(Search, StringComparison.OrdinalIgnoreCase) ||
                    (a.NewsContent ?? "").Contains(Search, StringComparison.OrdinalIgnoreCase));
            }

            // Calculate pagination
            var total = allArticles.Count();
            TotalPages = (int)Math.Ceiling((double)total / PageSize);

            // Get articles for current page
            Articles = allArticles
                .Skip((CurrentPage - 1) * PageSize)
                .Take(PageSize)
                .ToList();
        }
    }
}
