using Assignment2.BusinessLogic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Assignment2.DataAccess.Models;

namespace Presentation.Pages.Report
{
    public class IndexModel : PageModel
    {
        private readonly INewsArticleService _newsArticleService;

        public IndexModel(INewsArticleService newsArticleService)
        {
            _newsArticleService = newsArticleService;
        }

        public List<Assignment2.DataAccess.Models.NewsArticle> Articles { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public string? Start { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? End { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? GroupBy { get; set; }

        public int ActiveCount { get; set; }

        public int InactiveCount { get; set; }

        public void OnGet()
        {
            DateTime? startDate = null;
            DateTime? endDate = null;

            if (!string.IsNullOrEmpty(Start) && DateTime.TryParse(Start, out var parsedStart))
            {
                startDate = parsedStart;
            }

            if (!string.IsNullOrEmpty(End) && DateTime.TryParse(End, out var parsedEnd))
            {
                endDate = parsedEnd;
            }

            //Articles = _newsArticleService.Search("",startDate, endDate,0, true).ToList();
            Articles = _newsArticleService.GetAll().ToList();

            // Tính toán summary
            ActiveCount = Articles.Count(n => n.NewsStatus == true);
            InactiveCount = Articles.Count(n => n.NewsStatus == false);
        }

        public IActionResult OnGetExportToExcel(string? start, string? end)
        {
            DateTime? startDate = null;
            DateTime? endDate = null;

            if (!string.IsNullOrEmpty(start) && DateTime.TryParse(start, out var parsedStart))
            {
                startDate = parsedStart;
            }

            if (!string.IsNullOrEmpty(end) && DateTime.TryParse(end, out var parsedEnd))
            {
                endDate = parsedEnd;
            }

            var articles = _newsArticleService.Search("", startDate, endDate, 0, true).ToList();


            return Page();
        }
    }
}
