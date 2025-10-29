using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Assignment2.BusinessLogic;
using Assignment2.DataAccess.Models;
using System.Linq;
using Microsoft.AspNetCore.SignalR;
using Presentation.Hubs;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace Presentation.Pages.AccountManagement
{
    [IgnoreAntiforgeryToken]
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

            // Chuẩn hoá input
            Account.AccountName = (Account.AccountName ?? string.Empty).Trim();
            Account.AccountEmail = (Account.AccountEmail ?? string.Empty).Trim().ToLowerInvariant();
            Password = (Password ?? string.Empty).Trim();

            // Không cho đổi email khi update
            if (!IsCreate)
            {
                var existing = _acc.Get(Account.AccountId);
                if (existing == null) return NotFound();
                Account.AccountEmail = existing.AccountEmail; // giữ nguyên email cũ
            }

            ValidateAccount(IsCreate);

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

                    existing.AccountName = Account.AccountName;
                    existing.AccountRole = Account.AccountRole;

                    if (!Account.AccountStatus && existing.AccountStatus)
                    {
                        await _hubContext.Clients.All.SendAsync("AccountDeactivated", Account.AccountId.ToString());
                    }

                    existing.AccountStatus = Account.AccountStatus;
                    _acc.Update(existing);
                    TempData["SuccessMessage"] = "Account updated successfully.";
                }

                if (IsModal) return new JsonResult(new { success = true });
                return RedirectToPage("Index");
            }
            catch (System.Exception ex)
            {
                ModelState.AddModelError("", "An error occurred: " + ex.Message);
                return IsModal ? Page() : Page();
            }
        }


        private void ValidateAccount(bool isCreate)
        {
            if (_acc.ExistsEmail(Account.AccountEmail))
            {
                ModelState.AddModelError(nameof(Account.AccountEmail), "Email đã tồn tại.");
            }

            // Name: required, max 100, regex ký tự cho phép
            if (string.IsNullOrWhiteSpace(Account.AccountName))
                ModelState.AddModelError(nameof(Account.AccountName), "Name là bắt buộc.");
            else
            {
                if (Account.AccountName.Length > 100)
                    ModelState.AddModelError(nameof(Account.AccountName), "Name tối đa 100 ký tự.");

                // Cho phép chữ có dấu, số và 1 số ký tự thông dụng
                var nameRegex = new Regex(@"^[\p{L}\p{M}0-9 .,'\-_/()]+$");
                if (!nameRegex.IsMatch(Account.AccountName))
                    ModelState.AddModelError(nameof(Account.AccountName), "Name chỉ chứa chữ, số và một số ký tự thông dụng.");
            }

            if (string.IsNullOrWhiteSpace(Account.AccountEmail))
                ModelState.AddModelError(nameof(Account.AccountEmail), "Email là bắt buộc.");
            else
            {
                var emailAttr = new EmailAddressAttribute();
                if (!emailAttr.IsValid(Account.AccountEmail))
                    ModelState.AddModelError(nameof(Account.AccountEmail), "Email không hợp lệ.");
                if (Account.AccountEmail.Length > 150)
                    ModelState.AddModelError(nameof(Account.AccountEmail), "Email tối đa 150 ký tự.");
            }

            // Role: required (1 hoặc 2)
            if (Account.AccountRole != 1 && Account.AccountRole != 2)
                ModelState.AddModelError(nameof(Account.AccountRole), "Role không hợp lệ. Hãy chọn Staff hoặc Lecturer.");

            // Password: bắt buộc khi tạo mới; tối thiểu 6 ký tự
            if (isCreate)
            {
                if (string.IsNullOrWhiteSpace(Password))
                    ModelState.AddModelError(nameof(Password), "Password là bắt buộc khi tạo mới.");
                else
                {
                    if (Password.Length < 6)
                        ModelState.AddModelError(nameof(Password), "Password cần tối thiểu 6 ký tự.");

                    // (Tuỳ chọn) Bật rule mạnh hơn: ít nhất 1 chữ hoa, 1 chữ thường, 1 số
                    var strong = new Regex(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).{6,100}$");
                    if (!strong.IsMatch(Password))
                        ModelState.AddModelError(nameof(Password), "Password nên có chữ hoa, chữ thường và số để đảm bảo an toàn.");
                }
            }
        }
        
    }
}