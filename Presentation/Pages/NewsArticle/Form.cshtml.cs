//using Microsoft.AspNetCore.Mvc;
//using Microsoft.AspNetCore.Mvc.RazorPages;
//using Assignment2.BusinessLogic;
//using Assignment2.DataAccess.Models;
//using Microsoft.AspNetCore.SignalR;
//using Presentation.Hubs;

//namespace Presentation.Pages.NewsArticle
//{
//    public class FormModel : PageModel
//    {
//        private readonly INewsArticleService _newsArticleService;
//        private readonly ICategoryService _categoryService;
//        private readonly ITagService _tagService;
//        private readonly IHttpContextAccessor _httpContextAccessor;
//        private readonly IHubContext<NotificationHub> _hubContext;

//        public FormModel(
//            INewsArticleService newsArticleService,
//            ICategoryService categoryService,
//            ITagService tagService,
//            IHttpContextAccessor httpContextAccessor,
//            IHubContext<NotificationHub> hubContext)
//        {
//            _newsArticleService = newsArticleService;
//            _categoryService = categoryService;
//            _tagService = tagService;
//            _httpContextAccessor = httpContextAccessor;
//            _hubContext = hubContext;
//        }

//        [BindProperty]
//        public Assignment2.DataAccess.Models.NewsArticle Article { get; set; } = default!;

//        [BindProperty]
//        public int[] SelectedTags { get; set; } = Array.Empty<int>();

//        public IList<Category> Categories { get; set; } = default!;
//        public IList<Tag> Tags { get; set; } = default!;

//        public bool IsCreate => Article?.NewsArticleId == 0;
//        public bool IsAdmin => _httpContextAccessor.HttpContext?.Session.GetInt32("Role") == 0;
//        public int CurrentUserId => _httpContextAccessor.HttpContext?.Session.GetInt32("AccountId") ?? 0;

//        public IActionResult OnGet(int? id)
//        {
//            LoadLookupData();

//            if (id.HasValue)
//            {
//                Article = _newsArticleService.Get(id.Value);
//                if (Article == null)
//                {
//                    return NotFound();
//                }

//                // Only allow editing by admin or the author
//                if (!IsAdmin && Article.AuthorId != CurrentUserId)
//                {
//                    return Unauthorized();
//                }

//                SelectedTags = Article.Tags.Select(t => t.TagId).ToArray();
//            }
//            else
//            {
//                Article = new Assignment2.DataAccess.Models.NewsArticle
//                {
//                    AuthorId = CurrentUserId,
//                    ArticleStatus = false // Draft by default
//                };
//            }

//            return Page();
//        }

//        public async Task<IActionResult> OnPostAsync()
//        {
//            if (!ModelState.IsValid)
//            {
//                LoadLookupData();
//                return Page();
//            }

//            bool isNewArticle = Article.ArticleId == 0;

//            if (isNewArticle)
//            {
//                Article.AuthorId = CurrentUserId;
//                if (!IsAdmin)
//                {
//                    Article.ArticleStatus = false; // Non-admin can only save as draft
//                }

//                _newsArticleService.Add(Article);

//                // Notify about new article if it's published
//                if (Article.ArticleStatus)
//                {
//                    await _hubContext.Clients.All.SendAsync("ReceiveNewArticleNotification", 
//                        $"New article published: {Article.ArticleTitle}");
//                }
//            }
//            else
//            {
//                var existing = _newsArticleService.Get(Article.ArticleId);
//                if (existing == null)
//                {
//                    return NotFound();
//                }

//                // Only allow editing by admin or the author
//                if (!IsAdmin && existing.AuthorId != CurrentUserId)
//                {
//                    return Unauthorized();
//                }

//                // Update fields
//                existing.ArticleTitle = Article.ArticleTitle;
//                existing.ArticleContent = Article.ArticleContent;
//                existing.CategoryId = Article.CategoryId;
                
//                // Only admin can change publish status
//                if (IsAdmin)
//                {
//                    existing.ArticleStatus = Article.ArticleStatus;
//                }

//                _newsArticleService.Update(existing);

//                // Notify if article is being published
//                if (Article.ArticleStatus && !existing.ArticleStatus)
//                {
//                    await _hubContext.Clients.All.SendAsync("ReceiveNewArticleNotification", 
//                        $"Article published: {Article.ArticleTitle}");
//                }
//            }

//            // Update tags
//            _newsArticleService.UpdateArticleTags(Article.ArticleId, SelectedTags);

//            return new JsonResult(new { success = true });
//        }

//        private void LoadLookupData()
//        {
//            Categories = _categoryService.GetAll().ToList();
//            Tags = _tagService.GetAll().ToList();
//        }

//        private void Validate()
//        {
//            if (string.IsNullOrWhiteSpace(Article.ArticleTitle))
//            {
//                ModelState.AddModelError("Article.ArticleTitle", "Title is required");
//            }

//            if (string.IsNullOrWhiteSpace(Article.ArticleContent))
//            {
//                ModelState.AddModelError("Article.ArticleContent", "Content is required");
//            }

//            if (Article.CategoryId <= 0)
//            {
//                ModelState.AddModelError("Article.CategoryId", "Category is required");
//            }
//        }
//    }
//}