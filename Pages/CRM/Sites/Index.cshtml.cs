using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Cloud9_2.Models;
using Cloud9_2.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Identity;

namespace Cloud9_2.Pages.CRM.Sites
{
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<IndexModel> _logger;
        private readonly UserManager<ApplicationUser> _userManager;

        public IndexModel(ApplicationDbContext context, ILogger<IndexModel> logger, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _logger = logger;
            _userManager = userManager;
        }

        public List<Site> Sites { get; set; } = new List<Site>();
        public List<Partner> Partners { get; set; } = new List<Partner>();
        public List<Status> Statuses { get; set; } = new List<Status>();

        [BindProperty(SupportsGet = true)]
        public string SearchTerm { get; set; }

        [BindProperty(SupportsGet = true)]
        public int PageSize { get; set; } = 10;

        [BindProperty(SupportsGet = true)]
        public int CurrentPage { get; set; } = 1;

        public int TotalRecords { get; set; }
        public int TotalPages { get; set; }

        [BindProperty]
        public bool IsPrimary { get; set; }

        public async Task OnGetAsync()
        {
            Partners = await _context.Partners.OrderBy(p => p.Name).ToListAsync();
            Statuses = await _context.PartnerStatuses.OrderBy(s => s.Name).ToListAsync();
            _logger.LogInformation("Statuses loaded: {Count}", Statuses.Count);

            var query = _context.Sites
                .Include(s => s.Partner)
                .Include(s => s.Status)
                .AsQueryable();

            if (!string.IsNullOrEmpty(SearchTerm))
            {
                query = query.Where(s =>
                    (s.SiteName != null && s.SiteName.Contains(SearchTerm)) ||
                    (s.Partner != null && s.Partner.Name != null && s.Partner.Name.Contains(SearchTerm)) ||
                    (s.AddressLine1 != null && s.AddressLine1.Contains(SearchTerm)) ||
                    (s.City != null && s.City.Contains(SearchTerm)));
            }

            TotalRecords = await query.CountAsync();
            TotalPages = (int)Math.Ceiling(TotalRecords / (double)PageSize);

            Sites = await query
                .OrderByDescending(s => s.SiteId)
                .Skip((CurrentPage - 1) * PageSize)
                .Take(PageSize)
                .ToListAsync();

            foreach (var site in Sites)
            {
                _logger.LogInformation($"SiteId: {site.SiteId}, SiteName: '{(site.SiteName ?? "null")}', PartnerId: {site.PartnerId}, Partner: {(site.Partner != null ? (site.Partner.Name ?? "null") : "null")}, Status: {(site.Status != null ? (site.Status.Name ?? "null") : "null")}");
            }
        }

        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostCreateSiteAsync(string siteName, int partnerId, string addressLine1, string addressLine2, string city, string state, string postalCode, string country, bool isPrimary, string contactPerson1, string contactPerson2, string contactPerson3, string comment1, string comment2, int statusId = 1)
        {
            _logger.LogInformation("OnPostCreateSiteAsync called with siteName={SiteName}, partnerId={PartnerId}, statusId={StatusId}, contactPerson1={ContactPerson1}, contactPerson2={ContactPerson2}, contactPerson3={ContactPerson3}, comment1={Comment1}, comment2={Comment2}", siteName, partnerId, statusId, contactPerson1, contactPerson2, contactPerson3, comment1, comment2);

            if (string.IsNullOrEmpty(siteName))
            {
                _logger.LogWarning("A telephely neve szükséges");
                TempData["ErrorMessage"] = "A telephely neve szükséges.";
                return RedirectToPage();
            }

            var partner = await _context.Partners.FindAsync(partnerId);
            if (partnerId == 0 || partner == null)
            {
                _logger.LogWarning("Létező partner megadása kötelező partnerId={PartnerId}", partnerId);
                TempData["ErrorMessage"] = "Létező partner megadása kötelező.";
                return RedirectToPage();
            }

            if (await _context.PartnerStatuses.FindAsync(statusId) == null)
            {
                _logger.LogWarning("Létező státusz megadása kötelező statusId={StatusId}", statusId);
                TempData["ErrorMessage"] = "Létező státusz megadása kötelező.";
                return RedirectToPage();
            }

            var userId = _userManager.GetUserId(User) ?? "System";
            _logger.LogInformation("UserId: {UserId}, Partner: {PartnerName}", userId, partner.Name);

            var site = new Site
            {
                SiteName = siteName,
                PartnerId = partnerId,
                AddressLine1 = addressLine1,
                AddressLine2 = addressLine2,
                City = city,
                State = state,
                PostalCode = postalCode,
                Country = country,
                IsPrimary = isPrimary,
                StatusId = statusId,
                ContactPerson1 = contactPerson1,
                ContactPerson2 = contactPerson2,
                ContactPerson3 = contactPerson3,
                Comment1 = comment1,
                Comment2 = comment2,
                CreatedDate = DateTime.UtcNow,
                CreatedById = userId,
                LastModifiedDate = DateTime.UtcNow,
                LastModifiedById = userId
            };

            try
            {
                _context.Sites.Add(site);
                await _context.SaveChangesAsync();
                _logger.LogInformation("A telephely sikeresen létrehozva: SiteId={SiteId}, PartnerId={PartnerId}, PartnerName={PartnerName}", site.SiteId, site.PartnerId, partner.Name);
                TempData["SuccessMessage"] = "A telephely sikeresen létrehozva.";
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Hiba a létrehozás során: {Details}", ex.InnerException?.Message ?? ex.Message);
                TempData["ErrorMessage"] = $"Error creating site: {ex.InnerException?.Message ?? "Ismeretlen hiba"}";
            }

            return RedirectToPage();
        }

        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostEditSiteAsync(int siteId, string siteName, int partnerId, string addressLine1, string addressLine2, string city, string state, string postalCode, string country, bool isPrimary, string contactPerson1, string contactPerson2, string contactPerson3, string comment1, string comment2, int statusId = 1)
        {
            var site = await _context.Sites
                .Include(s => s.Partner)
                .FirstOrDefaultAsync(s => s.SiteId == siteId);
            if (site == null)
            {
                _logger.LogWarning("Site not found: SiteId={SiteId}", siteId);
                return NotFound();
            }

            if (string.IsNullOrEmpty(siteName))
            {
                _logger.LogWarning("Validation failed: siteName is empty for SiteId={SiteId}", siteId);
                TempData["ErrorMessage"] = "A telephely neve kötelező.";
                return RedirectToPage();
            }

            var partner = await _context.Partners.FindAsync(partnerId);
            if (partnerId == 0 || partner == null)
            {
                _logger.LogWarning("Létező partner szükséges partnerId={PartnerId} for SiteId={SiteId}", partnerId, siteId);
                TempData["ErrorMessage"] = "Létező partner szükséges.";
                return RedirectToPage();
            }

            if (await _context.PartnerStatuses.FindAsync(statusId) == null)
            {
                _logger.LogWarning("Létező státusz szükséges statusId={StatusId} for SiteId={SiteId}", statusId, siteId);
                TempData["ErrorMessage"] = "Létező státusz szükséges.";
                return RedirectToPage();
            }

            _logger.LogInformation("Updating site: SiteId={SiteId}, Old PartnerId={OldPartnerId}, New PartnerId={NewPartnerId}, New PartnerName={NewPartnerName}", siteId, site.PartnerId, partnerId, partner.Name);

            site.SiteName = siteName;
            site.PartnerId = partnerId;
            site.AddressLine1 = addressLine1;
            site.AddressLine2 = addressLine2;
            site.City = city;
            site.State = state;
            site.PostalCode = postalCode;
            site.Country = country;
            site.IsPrimary = isPrimary;
            site.StatusId = statusId;
            site.ContactPerson1 = contactPerson1;
            site.ContactPerson2 = contactPerson2;
            site.ContactPerson3 = contactPerson3;
            site.Comment1 = comment1;
            site.Comment2 = comment2;
            site.LastModifiedDate = DateTime.UtcNow;
            site.LastModifiedById = _userManager.GetUserId(User) ?? "System";

            try
            {
                await _context.SaveChangesAsync();
                _logger.LogInformation("A telephely sikeresen módosítva: SiteId={SiteId}, PartnerId={PartnerId}, PartnerName={PartnerName}", siteId, site.PartnerId, partner.Name);
                TempData["SuccessMessage"] = "A telephely sikeresen módosítva.";
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Hiba a telephely frissítésekor: {Details}", ex.InnerException?.Message ?? ex.Message);
                TempData["ErrorMessage"] = $"Hiba a telephely frissítésekor: {ex.InnerException?.Message ?? "Ismeretlen hiba"}";
            }

            return RedirectToPage();
        }

        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostDeleteSiteAsync(int siteId)
        {
            var site = await _context.Sites.FindAsync(siteId);
            if (site == null)
            {
                _logger.LogWarning("Site not found: SiteId={SiteId}", siteId);
                return NotFound();
            }

            try
            {
                _context.Sites.Remove(site);
                await _context.SaveChangesAsync();
                _logger.LogInformation("A telephely sikeresen törölve: SiteId={SiteId}", siteId);
                TempData["SuccessMessage"] = "A telephely sikeresen törölve.";
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Hiba a telephely törlésekor: {Details}", ex.InnerException?.Message ?? ex.Message);
                TempData["ErrorMessage"] = $"Hiba a telephely törlésekor: {ex.InnerException?.Message ?? "Ismeretlen hiba"}";
            }

            return RedirectToPage();
        }
    }
}