using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Cloud9_2.Models;
using Cloud9_2.Data;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Cloud9_2.Pages.HR.WorkGroup
{
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public List<Site> Sites { get; set; }
        
        [BindProperty(SupportsGet = true)]
        public string SearchTerm { get; set; }
        
        [BindProperty(SupportsGet = true)]
        public int PageSize { get; set; } = 10;
        
        [BindProperty(SupportsGet = true)]
        public int CurrentPage { get; set; } = 1;
        
        public int TotalRecords { get; set; }
        public int TotalPages { get; set; }

        public async Task OnGetAsync()
        {
            var query = _context.Sites
                .Include(s => s.Partner)
                .AsQueryable();

            if (!string.IsNullOrEmpty(SearchTerm))
            {
                query = query.Where(s => 
                    s.SiteName.Contains(SearchTerm) || 
                    (s.Partner != null && s.Partner.Name.Contains(SearchTerm)) ||
                    s.AddressLine1.Contains(SearchTerm) ||
                    s.City.Contains(SearchTerm));
            }

            TotalRecords = await query.CountAsync();
            TotalPages = (int)Math.Ceiling(TotalRecords / (double)PageSize);

            Sites = await query
                .OrderBy(s => s.SiteName)
                .Skip((CurrentPage - 1) * PageSize)
                .Take(PageSize)
                .ToListAsync();
        }

        public async Task<IActionResult> OnPostEditSiteAsync(int siteId, string siteName, 
            string addressLine1, string addressLine2, string city, 
            string postalCode, string country, bool isPrimary)
        {
            var site = await _context.Sites.FindAsync(siteId);
            if (site == null)
            {
                return NotFound();
            }

            site.SiteName = siteName;
            site.AddressLine1 = addressLine1;
            site.AddressLine2 = addressLine2;
            site.City = city;
            site.PostalCode = postalCode;
            site.Country = country;
            site.IsPrimary = isPrimary;

            try
            {
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Site updated successfully";
            }
            catch (DbUpdateException)
            {
                TempData["ErrorMessage"] = "Error updating site";
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostDeleteSiteAsync(int siteId)
        {
            var site = await _context.Sites.FindAsync(siteId);
            if (site == null)
            {
                return NotFound();
            }

            try
            {
                _context.Sites.Remove(site);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Site deleted successfully";
            }
            catch (DbUpdateException)
            {
                TempData["ErrorMessage"] = "Error deleting site";
            }

            return RedirectToPage();
        }
    }
}