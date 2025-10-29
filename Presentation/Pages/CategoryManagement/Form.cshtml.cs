using Assignment2.BusinessLogic;
using Assignment2.DataAccess.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Presentation.Pages.CategoryManagement
{
    public class FormModel : PageModel
    {
        private readonly ICategoryService _cats;

        public FormModel(ICategoryService cats)
        {
            _cats = cats;
        }
        [BindProperty]
        public Category Category { get; set; }

        public List<SelectListItem> ParentCategories { get; set; } = new List<SelectListItem>();

        public bool IsCreate { get; set; }

        [TempData]
        public string SuccessMessage { get; set; }

        [BindProperty(SupportsGet = true)]
        public bool IsModal { get; set; }

        private int? Role => HttpContext.Session.GetInt32("Role");
        private bool IsStaff => Role == 1;

        private void Validate(Category c)
        {
            if (string.IsNullOrWhiteSpace(c.CategoryName))
                ModelState.AddModelError(nameof(c.CategoryName), "Name is required");
            if (c.ParentCategoryId == c.CategoryId)
                ModelState.AddModelError(nameof(c.ParentCategoryId), "Parent cannot be itself");
        }

        public IActionResult OnGet(short? id)
        {
            if (!IsStaff) return Unauthorized();

            var allCat = _cats.GetAll(true).ToList();

            if (id.HasValue)
            {
                var existingCategory = _cats.Get(id.Value);
                if (existingCategory == null) return NotFound();
                Category = new Category
                {
                    CategoryId = existingCategory.CategoryId,
                    CategoryName = existingCategory.CategoryName,
                    CategoryDesciption = existingCategory.CategoryDesciption,
                    ParentCategoryId = existingCategory.ParentCategoryId,
                    IsActive = existingCategory.IsActive
                };
            }

            ParentCategories = allCat
                .Where(c => !id.HasValue || c.CategoryId != id.Value)
                .Select(c => new SelectListItem
                {
                    Value = c.CategoryId.ToString(),
                    Text = c.CategoryName
                })
                .ToList();

            return Page();
        }

        private void LoadLookups(short? parentId = null) =>
            ViewData["ParentList"] = new SelectList(_cats.GetAll(true), "CategoryId", "CategoryName", parentId);

        public IActionResult OnPost()
        {
            Console.WriteLine("Post modal");
            if (!IsStaff) return Unauthorized();

            Validate(Category);

            if (!ModelState.IsValid)
            {
                LoadLookups(Category.ParentCategoryId);

                return Page();
            }

            if (Category.CategoryId == 0)
            {
                _cats.Add(Category);
            }
            else
            {
                _cats.Update(Category);
                SuccessMessage = "Category updated successfully.";
                var result = _cats.Update(Category);
                if (!result.Success)
                {
                    ModelState.AddModelError(string.Empty, result.Message);
                    LoadLookups(Category.ParentCategoryId);
                    return Page();
                }
            }

            return RedirectToPage("Index"); ;
        }
    }
}
