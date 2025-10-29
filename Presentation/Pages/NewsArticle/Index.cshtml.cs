using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Assignment2.BusinessLogic;
using Assignment2.DataAccess.Models;

namespace Presentation.Pages.NewsArticle
{
    public class IndexModel : PageModel
    {
        private readonly INewsArticleService _newsArticleService;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public IndexModel(INewsArticleService newsArticleService, IHttpContextAccessor httpContextAccessor)
        {
            _newsArticleService = newsArticleService;
            _httpContextAccessor = httpContextAccessor;
        }

        //public IList<Assignment2.DataAccess.Models.NewsArticle> Articles { get; set; } = new List<Assignment2.DataAccess.Models.NewsArticle>();
        
        //public bool IsAdmin => _httpContextAccessor.HttpContext?.Session.GetInt32("Role") == 0;

        //public void OnGet()
        //{
        //    Articles = _newsArticleService.GetAll().ToList();
        //}

        //public IActionResult OnPostDelete(int id)
        //{
        //    if (!IsAdmin)
        //    {
        //        return Unauthorized();
        //    }

        //    var article = _newsArticleService.Get(id);
        //    if (article == null)
        //    {
        //        return NotFound();
        //    }

        //    _newsArticleService.Delete(id);
        //    TempData["SuccessMessage"] = "Article deleted successfully.";
        //    return RedirectToPage();
        //}
    }
}