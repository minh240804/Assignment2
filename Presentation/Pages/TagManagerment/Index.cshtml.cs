using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Assignment2.BusinessLogic;
using Assignment2.DataAccess.Models;
using System.Collections.Generic;
using System.Linq;
using System;


namespace Presentation.Pages.TagManagement 
{
    public class IndexModel : PageModel
    {
        private readonly ITagService _tagService;

        public IndexModel(ITagService tagService)
        {
            _tagService = tagService;
        }

        public IEnumerable<Tag> Tags { get; set; } = new List<Tag>();


        private bool IsAuthorized()
        {

            var role = HttpContext.Session.GetInt32("Role");
            Console.WriteLine($"User role from session: {role}");
            return true; 
        }

        public IActionResult OnGet()
        {
            //if (!IsAuthorized()) return Forbid(); 
            Tags = _tagService.GetAll();
            return Page();
        }


        public IActionResult OnGetCreatePopup()
        {
            if (!IsAuthorized()) return Forbid();
            return Partial("_TagForm", new Tag());
        }

        public IActionResult OnGetEditPopup(int id)
        {
            if (!IsAuthorized()) return Forbid();
            var tag = _tagService.Get(id);
            if (tag == null) return NotFound();
            return Partial("_TagForm", tag);
        }


        [HttpPost]
        public async Task<IActionResult> OnPostCreateAsync(Tag tag)
        {
            if (!IsAuthorized()) return Forbid();
            if (!ModelState.IsValid)
            {
                return Partial("_TagForm", tag);
            }
            try
            {
                 _tagService.Add(tag);
                return new JsonResult(new { success = true });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", ex.Message); 
                return Partial("_TagForm", tag);
            }
        }

        [HttpPost]
        public async Task<IActionResult> OnPostEditAsync(Tag tag)
        {
            if (!IsAuthorized()) return Forbid();
            if (!ModelState.IsValid)
            {
                return Partial("_TagForm", tag);
            }
            try
            {
                var result =  _tagService.Update(tag);
                if (result.Success)
                {
                    return new JsonResult(new { success = true });
                }
                else
                {
                    ModelState.AddModelError("", result.Message); 
                    return Partial("_TagForm", tag);
                }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Error updating tag: {ex.Message}");
                return Partial("_TagForm", tag);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken] 
        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            if (!IsAuthorized()) return Forbid();
            try
            {
                var tag = _tagService.Get(id);
                _tagService.Delete(id);

                TempData["ToastMessage"] = $"Tag '{tag.TagName}' was deleted by Admin.";
            }
            catch (InvalidOperationException ex) 
            {
                TempData["Error"] = ex.Message; 
            }
            catch (Exception) 
            {
                TempData["Error"] = "An error occurred while deleting the tag.";
            }

            return RedirectToPage();
        }
    }
}