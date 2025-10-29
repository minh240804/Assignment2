using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Assignment2.BusinessLogic;
using Assignment2.DataAccess.Models;
using System.Collections.Generic;
using System.Linq;
using System;

namespace Presentation.Pages.Account
{
    public class IndexModel : PageModel
    {
        private readonly IAccountService _acc;

        public IndexModel(IAccountService acc)
        {
            _acc = acc;
        }

        public List<SystemAccount> Accounts { get; set; }
        
        [BindProperty(SupportsGet = true)]
        public string Search { get; set; }
        
        [BindProperty(SupportsGet = true)]
        public int? RoleFilter { get; set; }
        
        [BindProperty(SupportsGet = true)]
        public int CurrentPage { get; set; } = 1;
        
        public int TotalPages { get; set; }
        
        private const int PageSize = 5;

        private bool IsAdmin() => HttpContext.Session.GetInt32("Role") is int r && r != 1 && r != 2;

        public IActionResult OnGet()
        {
            if (!IsAdmin()) return Unauthorized();

            var query = _acc.GetAll();

            if (!string.IsNullOrWhiteSpace(Search))
            {
                query = query.Where(x =>
                    x.AccountName.Contains(Search, StringComparison.OrdinalIgnoreCase) ||
                    x.AccountEmail.Contains(Search, StringComparison.OrdinalIgnoreCase));
            }

            if (RoleFilter.HasValue && RoleFilter > 0)
            {
                query = query.Where(x => x.AccountRole == RoleFilter.Value);
            }

            var total = query.Count();
            TotalPages = (int)Math.Ceiling((double)total / PageSize);

            Accounts = query
                .Skip((CurrentPage - 1) * PageSize)
                .Take(PageSize)
                .ToList();

            return Page();
        }

        public IActionResult OnPostDelete(short id)
        {
            if (!IsAdmin()) return Unauthorized();

            var result = _acc.Delete(id);
            
            if (result.Success)
            {
                TempData["SuccessMessage"] = result.Message;
            }
            else
            {
                TempData["ErrorMessage"] = result.Message;
            }

            return RedirectToPage();
        }
    }
}