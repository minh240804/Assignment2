using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.SignalR;
using Assignment2.BusinessLogic;
using Assignment2.DataAccess.Models;
using Presentation.Hubs;

namespace Presentation.Pages.NewsArticleManagement
{
    public class FormModel : PageModel
    {
        private readonly INewsArticleService _newsArticleService;
        private readonly ICategoryService _categoryService;
        private readonly ITagService _tagService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IHubContext<NotificationHub> _hubContext;

        public FormModel(
            INewsArticleService newsArticleService,
            ICategoryService categoryService,
            ITagService tagService,
            IHttpContextAccessor httpContextAccessor,
            IHubContext<NotificationHub> hubContext)
        {
            _newsArticleService = newsArticleService;
            _categoryService = categoryService;
            _tagService = tagService;
            _httpContextAccessor = httpContextAccessor;
            _hubContext = hubContext;
        }

        [BindProperty]
        public NewsArticle Article { get; set; } = default!;

        [BindProperty]
        public int[] SelectedTags { get; set; } = Array.Empty<int>();

        public IList<Category> Categories { get; set; } = default!;
        public IList<Tag> Tags { get; set; } = default!;

        public bool IsCreate => string.IsNullOrEmpty(Article?.NewsArticleId);
        public bool IsStaff => _httpContextAccessor.HttpContext?.Session.GetInt32("Role") == 1;
        public int CurrentUserId => _httpContextAccessor.HttpContext?.Session.GetInt32("AccountId") ?? 0;

        public IActionResult OnGet(string? id)
        {
            LoadLookupData();

            if (!string.IsNullOrEmpty(id))
            {
                Article = _newsArticleService.Get(id);
                if (Article == null)
                    return NotFound();

                if (!IsStaff && Article.CreatedById != CurrentUserId)
                    return Unauthorized();

                SelectedTags = Article.Tags.Select(t => t.TagId).ToArray();
            }
            else
            {
                Article = new NewsArticle
                {
                    CreatedById = (short)CurrentUserId,
                    NewsStatus = false 
                };
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                LoadLookupData();
                return Page();
            }

            bool isNew = string.IsNullOrEmpty(Article.NewsArticleId);

            if (isNew)
            {
                Article.NewsArticleId = Guid.NewGuid().ToString();
                Article.CreatedById = (short)CurrentUserId;
                Article.CreatedDate = DateTime.Now;
                Article.ModifiedDate = DateTime.Now;

                if (!IsStaff)
                    Article.NewsStatus = false;

                _newsArticleService.Add(Article, SelectedTags ?? Array.Empty<int>());

                if (Article.NewsStatus == true)
                {
                    await _hubContext.Clients.All.SendAsync("ReceiveNewArticleNotification",
                        $"New article published: {Article.Headline}");
                }
            }
            else
            {
                var existing = _newsArticleService.Get(Article.NewsArticleId);
                if (existing == null)
                    return NotFound();

                if (!IsStaff && existing.CreatedById != CurrentUserId)
                    return Unauthorized();

                // Cập nhật thông tin
                existing.NewsTitle = Article.NewsTitle;
                existing.Headline = Article.Headline;
                existing.NewsContent = Article.NewsContent;
                existing.CategoryId = Article.CategoryId;
                existing.ModifiedDate = DateTime.Now;
                existing.UpdatedById = (short)CurrentUserId;

                if (IsStaff)
                    existing.NewsStatus = Article.NewsStatus;

                _newsArticleService.Update(existing, SelectedTags ?? Array.Empty<int>());

                if (Article.NewsStatus == true && existing.NewsStatus == false)
                {
                    await _hubContext.Clients.All.SendAsync("ReceiveNewArticleNotification",
                        $"Article published: {Article.Headline}");
                }
            }

            return new JsonResult(new { success = true });
        }

        private void LoadLookupData()
        {
            Categories = _categoryService.GetAll().ToList();
            Tags = _tagService.GetAll().ToList();
        }
    }
}
