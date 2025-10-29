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

            IsCreate = !id.HasValue;

            if (IsCreate)
            {
                Category = new Category { IsActive = true };
            }
            else
            {
                var existingCategory = _cats.Get(id.Value);
                if (existingCategory == null) return NotFound();
                Category = new Category
                {
                    CategoryId = existingCategory.CategoryId,
                    CategoryName = existingCategory.CategoryName,
                    ParentCategoryId = existingCategory.ParentCategoryId,
                    IsActive = existingCategory.IsActive
                };
            }

            return Page();
        }


        public async Task<IActionResult> OnPostAsync()
        {
            if (!IsStaff) return Unauthorized();

            IsCreate = Category.CategoryId == 0;

            if (!IsCreate)
            {
                var existingCategory = _cats.Get(Category.CategoryId);
                if (existingCategory == null) return NotFound();
                Category.CategoryId = existingCategory.CategoryId;
            }

            Validate(Category);
            if (!ModelState.IsValid)
            {
                return Page();
            }

            try
            {
                if (IsCreate)
                {
                    _cats.Add(Category);
                    SuccessMessage = "Category created successfully.";
                }
                else
                {
                    var existing = _cats.Get(Category.CategoryId);
                    if (existing == null) return NotFound();
                    Category.CategoryId = existing.CategoryId;
                    _cats.Update(Category);
                    SuccessMessage = "Category updated successfully.";
                }
                if (IsModal)
                {
                    return new JsonResult(new { success = true });
                }

                return RedirectToPage("Index");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, $"An error occurred: {ex.Message}");
                return Page();
            }
        }
    }
}
