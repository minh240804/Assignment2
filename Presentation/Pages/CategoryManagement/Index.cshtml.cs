using Assignment2.BusinessLogic;
using Assignment2.DataAccess.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;


namespace Presentation.Pages.CategoryManagement
{
    public class IndexModel : PageModel
    {
        private readonly ICategoryService _cats;

        public IndexModel(ICategoryService cats)
        {
            _cats = cats;
        }

        public IEnumerable<Category> Categories { get; set; }
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public string Search { get; set; }
        public bool? Status { get; set; }

        // [BindProperty] 
        [BindProperty]
        public Category Category { get; set; }

        // [TempData] để hiển thị thông báo
        [TempData]
        public string SuccessMessage { get; set; }
        [TempData]
        public string ErrorMessage { get; set; }

        // Helpers
        private int? Role => HttpContext.Session.GetInt32("Role");
        private bool IsStaff => Role == 1;

        // Dùng ViewData (tương tự ViewBag) để truyền SelectList
        // cho các partial view (form popup)
        private void LoadLookups(short? parentId = null) =>
            ViewData["ParentList"] = new SelectList(_cats.GetAll(true), "CategoryId", "CategoryName", parentId);

        private void Validate(Category c)
        {
            if (string.IsNullOrWhiteSpace(c.CategoryName))
                ModelState.AddModelError(nameof(c.CategoryName), "Name is required");
            if (c.ParentCategoryId == c.CategoryId)
                ModelState.AddModelError(nameof(c.ParentCategoryId), "Parent cannot be itself");
        }

        // Handler cho HTTP GET (thay thế action Index)
        public IActionResult OnGet(string? search, bool? active, int page = 1, int size = 5)
        {
            if (!IsStaff) return Unauthorized();

            var q = _cats.GetAll(active);
            if (!string.IsNullOrWhiteSpace(search))
                q = q.Where(x => x.CategoryName.Contains(search, StringComparison.OrdinalIgnoreCase));

            // Gán giá trị cho properties (thay vì ViewBag)
            CurrentPage = page;
            TotalPages = (int)Math.Ceiling(q.Count() / (double)size);
            Search = search;
            Status = active;

            Categories = q.Skip((page - 1) * size).Take(size);

            // Hiển thị thông báo nếu có
            if (!string.IsNullOrEmpty(SuccessMessage))
                ViewData["Success"] = SuccessMessage;
            if (!string.IsNullOrEmpty(ErrorMessage))
                ViewData["Error"] = ErrorMessage;

            return Page();
        }

        // Handler cho GET popup (thay thế action CreatePopup)
        public IActionResult OnGetCreatePopup()
        {
            Console.WriteLine("create");
            if (!IsStaff) return Unauthorized();
            LoadLookups();
            // Trả về PartialView, giả sử file partial tên là _Form.cshtml
            return Partial("Form", new Category { IsActive = true });
        }

        // Handler cho GET popup (thay thế action EditPopup)
        public IActionResult OnGetEditPopup(short id)
        {

            Console.WriteLine("create");
            if (!IsStaff) return Unauthorized();
            var c = _cats.Get(id);
            if (c == null) return NotFound();
            LoadLookups(c.ParentCategoryId);
            return Partial("Form", c);
        }

        public IActionResult OnPostCreate()
        {
            if (!IsStaff) return Unauthorized();
            Validate(Category);

            if (!ModelState.IsValid)
            {
                LoadLookups(Category.ParentCategoryId);
                return Partial("_Form", Category);
            }

            _cats.Add(Category);
            SuccessMessage = "Category created"; // Dùng TempData
            return new JsonResult(new { success = true });
        }

        public IActionResult OnPostEdit()
        {
            if (!IsStaff) return Unauthorized();
            Validate(Category);

            if (!ModelState.IsValid)
            {
                LoadLookups(Category.ParentCategoryId);
                return Partial("_Form", Category);
            }

            var result = _cats.Update(Category);

            if (!result.Success)
            {
                ModelState.AddModelError(string.Empty, result.Message);
                LoadLookups(Category.ParentCategoryId);
                return Partial("_Form", Category);
            }

            SuccessMessage = result.Message; // Dùng TempData
            return new JsonResult(new { success = true });
        }

        public IActionResult OnPostDelete(short id)
        {
            if (!IsStaff) return Unauthorized();

            try
            {
                var isSuccess = _cats.Delete(id);
                if (isSuccess)
                {
                    SuccessMessage = "Category deleted successfully.";
                }
                else
                {
                    ErrorMessage = "Cannot delete this category because it is in use.";
                }
            }
            catch (Exception)
            {
                ErrorMessage = "An error occurred while deleting the category.";
            }

            // Redirect về OnGet của trang Index hiện tại
            return RedirectToPage();
        }
    }
}