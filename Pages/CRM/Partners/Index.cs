using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Cloud9_2.Models;
using Cloud9_2.Data;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.AspNetCore.Identity;
using System;

namespace Cloud9_2.Pages.CRM.Partners
{
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public IndexModel(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public IList<Partner> Partners { get; set; } = new List<Partner>();
        [BindProperty(SupportsGet = true)]
        public int CurrentPage { get; set; } = 1;
        [BindProperty(SupportsGet = true)]
        public int PageSize { get; set; } = 10;
        public int TotalRecords { get; set; }
        public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalRecords / PageSize) : 0;
        [BindProperty(SupportsGet = true)]
        public string SearchTerm { get; set; }
        public HashSet<string> VisibleColumns { get; set; } = new HashSet<string>();

        [BindProperty]
        public Partner NewPartner { get; set; }

        public async Task<IActionResult> OnGetAsync(int? pageNumber, int? pageSize, string searchTerm)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var roles = await _userManager.GetRolesAsync(user);
            var permissions = await _context.AccessPermissions
                .Join(_context.Roles, p => p.RoleId, r => r.Id, (p, r) => new { Permission = p, RoleName = r.Name })
                .Where(x => roles.Contains(x.RoleName) && x.Permission.PagePath == "/CRM/Partners")
                .Select(x => x.Permission)
                .ToListAsync();

            bool canViewPage = permissions.Any(p => p.CanViewPage) || User.IsInRole("SuperAdmin");
            if (!canViewPage) return Forbid();

            VisibleColumns = permissions
                .Where(p => p.ColumnName != null && p.CanViewColumn)
                .Select(p => p.ColumnName!)
                .ToHashSet();
            if (User.IsInRole("SuperAdmin")) VisibleColumns = new HashSet<string> { "Name", "Email" };

            CurrentPage = pageNumber ?? CurrentPage;
            PageSize = pageSize ?? PageSize;
            SearchTerm = searchTerm ?? SearchTerm;
            if (PageSize <= 0) PageSize = 10;
            if (CurrentPage <= 0) CurrentPage = 1;

            IQueryable<Partner> partnersQuery = _context.Partners.AsNoTracking();
            if (!string.IsNullOrWhiteSpace(SearchTerm))
            {
                string lowerSearchTerm = SearchTerm.ToLower();
                partnersQuery = partnersQuery.Where(p =>
                    (p.Name != null && p.Name.ToLower().Contains(lowerSearchTerm)) ||
                    (p.Email != null && p.Email.ToLower().Contains(lowerSearchTerm))
                );
            }
            TotalRecords = await partnersQuery.CountAsync();
            CurrentPage = Math.Max(1, Math.Min(CurrentPage, TotalPages > 0 ? TotalPages : 1));
            Partners = await partnersQuery
                .OrderBy(p => p.PartnerId)
                .Skip((CurrentPage - 1) * PageSize)
                .Take(PageSize)
                .ToListAsync();

            return Page();
        }

        public async Task<IActionResult> OnPostCreatePartnerAsync()
{
    ModelState.Remove(nameof(SearchTerm));
    ModelState.Remove(nameof(CurrentPage));
    ModelState.Remove(nameof(PageSize));
    // Remove navigation properties (not in form)
    ModelState.Remove("NewPartner.Sites");
    ModelState.Remove("NewPartner.Contacts");
    ModelState.Remove("NewPartner.Documents");
    ModelState.Remove("NewPartner.PartnerTypes");
    ModelState.Remove("NewPartner.LeadSources");

    if (!ModelState.IsValid)
    {
        var errorMessages = new List<string> { "Validation failed:" };
        foreach (var state in ModelState)
        {
            var errors = state.Value.Errors;
            if (errors.Any())
            {
                errorMessages.Add($"{state.Key}: {errors.First().ErrorMessage}");
            }
        }
        TempData["ValidationErrors"] = string.Join("\n", errorMessages);
        TempData["ShowModal"] = true;
        await PrepareAndReturnPage();
        return Page();
    }

    NewPartner.CreatedDate = DateTime.Now;
    NewPartner.CreatedBy = User.Identity?.Name ?? "Unknown";
    NewPartner.PartnerId = 0;

    _context.Partners.Add(NewPartner);
    await _context.SaveChangesAsync();

    TempData["SuccessMessage"] = $"Partner '{NewPartner.Name}' created successfully.";
    return RedirectToPage("./Index", new { pageNumber = CurrentPage, pageSize = PageSize, searchTerm = SearchTerm });
}

        private async Task PrepareAndReturnPage()
        {
            await OnGetAsync(CurrentPage, PageSize, SearchTerm);
        }
    }
}