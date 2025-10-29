using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Assignment2.BusinessLogic;
using Assignment2.DataAccess.Models;

namespace Presentation.Pages.AccountManagement
{
    public class ProfileModel : PageModel
    {
        private readonly IAccountService _acc;

        public ProfileModel(IAccountService acc)
        {
            _acc = acc;
        }

        [BindProperty]
        public SystemAccount Account { get; set; }

        [BindProperty]
        public string NewPassword { get; set; }

        public string PasswordError { get; set; }

        public IActionResult OnGet()
        {
            var id = HttpContext.Session.GetInt32("AccountId");
            if (id == null) return RedirectToPage("Login");

            Account = _acc.Get((short)id);
            if (Account == null) return NotFound();

            return Page();
        }

        public IActionResult OnPost()
        {
            var id = HttpContext.Session.GetInt32("AccountId");
            if (id == null || Account.AccountId != id) return Unauthorized();

            var existing = _acc.Get(Account.AccountId);
            if (existing == null) return NotFound();

            if (string.IsNullOrWhiteSpace(Account.AccountName))
                ModelState.AddModelError(nameof(Account.AccountName), "Name is required");

            if (!string.IsNullOrWhiteSpace(NewPassword) && NewPassword.Length < 6)
            {
                PasswordError = "Password must be at least 6 characters";
                return Page();
            }

            if (!ModelState.IsValid) return Page();

            existing.AccountName = Account.AccountName;
            _acc.Update(existing, NewPassword);

            TempData["Success"] = "Profile updated.";
            return RedirectToPage();
        }
    }
}