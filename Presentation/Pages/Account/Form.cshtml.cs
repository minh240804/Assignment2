using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Assignment2.BusinessLogic;
using Assignment2.DataAccess.Models;
using System.Linq;
using Microsoft.AspNetCore.SignalR;
using Presentation.Hubs;

namespace Presentation.Pages.Account
{
    public class FormModel : PageModel
    {
        private readonly IAccountService _acc;
        private readonly IHubContext<NotificationHub> _hubContext;

        public FormModel(IAccountService acc, IHubContext<NotificationHub> hubContext)
        {
            _acc = acc;
            _hubContext = hubContext;
        }

        [BindProperty]
        public SystemAccount Account { get; set; } = default!;

        [BindProperty]
        public string Password { get; set; } = string.Empty;

        public bool IsCreate { get; set; }

        [BindProperty(SupportsGet = true)]
        public bool IsModal { get; set; }

        private bool IsAdmin() => HttpContext.Session.GetInt32("Role") is int r && r == 0;

        public IActionResult OnGet(short? id)
        {
            if (!IsAdmin()) return Unauthorized();

            IsCreate = !id.HasValue;
            if (IsCreate)
            {
                Account = new SystemAccount();
            }
            else
            {
                var existingAccount = _acc.Get(id.Value);
                if (existingAccount == null) return NotFound();
                Account = new SystemAccount
                {
                    AccountId = existingAccount.AccountId,
                    AccountName = existingAccount.AccountName,
                    AccountEmail = existingAccount.AccountEmail,
                    AccountRole = existingAccount.AccountRole,
                    AccountPassword = existingAccount.AccountPassword
                };
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!IsAdmin()) return Unauthorized();

            IsCreate = Account.AccountId == 0;

            if (!IsCreate)
            {
                var existing = _acc.Get(Account.AccountId);
                if (existing == null) return NotFound();
                Account.AccountEmail = existing.AccountEmail; // Ensure email doesn't change
            }

            Validate(IsCreate);

            if (!ModelState.IsValid)
            {
                return Page();
            }

            try
            {
                if (IsCreate)
                {
                    _acc.Add(Account, Password);
                    await _hubContext.Clients.All.SendAsync("ReceiveNewAccountNotification", 
                        $"Admin has added a new account: {Account.AccountName}");
                    TempData["SuccessMessage"] = "Account created successfully.";
                }
                else
                {
                    var existing = _acc.Get(Account.AccountId);
                    if (existing == null) return NotFound();

                    // Update only allowed fields
                    existing.AccountName = Account.AccountName;
                    existing.AccountRole = Account.AccountRole;
                    
                    // If account is being deactivated
                    if (!Account.AccountStatus && existing.AccountStatus)
                    {
                        await _hubContext.Clients.All.SendAsync("AccountDeactivated", Account.AccountId.ToString());
                    }
                    
                    _acc.Update(existing);
                    TempData["SuccessMessage"] = "Account updated successfully.";
                }

                if (IsModal)
                {
                    return new JsonResult(new { success = true });
                }

                return RedirectToPage("Index");
            }
            catch (System.Exception ex)
            {
                ModelState.AddModelError("", "An error occurred: " + ex.Message);
                return IsModal ? Page() : Page();
            }
        }

        private void Validate(bool isCreate)
        {
            if (string.IsNullOrWhiteSpace(Account.AccountName))
                ModelState.AddModelError(nameof(Account.AccountName), "Name is required");

            if (!new[] { 1, 2 }.Contains(Account.AccountRole ?? 0))
                ModelState.AddModelError(nameof(Account.AccountRole), "Role must be 1 (Staff) or 2 (Lecturer)");

            if (isCreate)
            {
                if (string.IsNullOrWhiteSpace(Password))
                    ModelState.AddModelError(nameof(Password), "Password is required");
                else if (Password.Length < 6)
                    ModelState.AddModelError(nameof(Password), "Password must be at least 6 characters");

                if (string.IsNullOrWhiteSpace(Account.AccountEmail))
                    ModelState.AddModelError(nameof(Account.AccountEmail), "Email is required");
                else if (_acc.ExistsEmail(Account.AccountEmail))
                    ModelState.AddModelError(nameof(Account.AccountEmail), "Email already exists");
            }
        }
    }
}