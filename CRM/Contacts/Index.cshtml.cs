
using Cloud9_2.Models;
using Cloud9_2.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Cloud9_2.Data;

namespace Cloud9_2.Pages.CRM.Contacts
{
    public class IndexModel : PageModel
    {
        private readonly ContactService _service;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<IndexModel> _logger;
        private readonly ApplicationDbContext _context;

        public IndexModel(
            ContactService service,
            UserManager<ApplicationUser> userManager,
            ILogger<IndexModel> logger,
            ApplicationDbContext context)
        {
            _service = service;
            _userManager = userManager;
            _logger = logger;
            _context = context;
        }

        public List<ContactDto> Contacts { get; set; } = new();
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; }
        public int PageSize { get; set; } = 10;
        public int TotalRecords { get; set; }
        public string SearchTerm { get; set; } = string.Empty;

        public SelectList Statuses { get; set; } = new SelectList(new List<Status>());
        public SelectList Partners { get; set; } = new SelectList(new List<Partner>());

        public async Task OnGetAsync(int pageNumber = 1, int pageSize = 10, string searchTerm = "", string filter = "")
        {
            CurrentPage = pageNumber < 1 ? 1 : pageNumber;
            PageSize = pageSize < 1 ? 10 : pageSize;
            SearchTerm = searchTerm;

            try
            {
                var (pagedContacts, totalCount) = await _service.GetPagedAsync(CurrentPage, PageSize, SearchTerm, filter);
                Contacts = pagedContacts;
                TotalRecords = totalCount;
                TotalPages = (int)Math.Ceiling((double)totalCount / PageSize);

                var statuses = await _context.Statuses.ToListAsync();
                Statuses = new SelectList(statuses, "Id", "Name");

                var partners = await _context.Partners.ToListAsync();
                Partners = new SelectList(partners, "PartnerId", "Name");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading contacts page");
                TempData["ErrorMessage"] = "Hiba történt az adatok betöltése közben.";
            }
        }

        public async Task<IActionResult> OnPostCreateContactAsync([Bind] CreateContactDto dto)
        {
            try
            {
                await _service.CreateAsync(dto);
                TempData["SuccessMessage"] = "Kontakt létrehozva sikeresen!";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating contact");
                TempData["ErrorMessage"] = "Kontakt létrehozása sikertelen. Próbálja újra.";
            }
            return RedirectToPage("./Index", new { pageNumber = 1, searchTerm = SearchTerm, pageSize = PageSize });
        }

        public async Task<IActionResult> OnPostUpdateContactAsync(int contactId, [Bind] UpdateContactDto dto)
        {
            try
            {
                var updated = await _service.UpdateAsync(contactId, dto);
                if (updated == null)
                {
                    TempData["ErrorMessage"] = "Kontakt nem található!";
                    return RedirectToPage("./Index", new { pageNumber = CurrentPage, searchTerm = SearchTerm, pageSize = PageSize });
                }
                TempData["SuccessMessage"] = "Kontakt frissítve sikeresen!";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating contact {ContactId}", contactId);
                TempData["ErrorMessage"] = "Kontakt frissítése sikertelen. Próbálja újra.";
            }
            return RedirectToPage("./Index", new { pageNumber = CurrentPage, searchTerm = SearchTerm, pageSize = PageSize });
        }

        public async Task<IActionResult> OnPostDeleteContactAsync(int contactId)
        {
            try
            {
                var hasRelated = await _context.CustomerCommunications.AnyAsync(cc => cc.ContactId == contactId);
                if (hasRelated)
                {
                    TempData["ErrorMessage"] = "Kontakt nem törölhető, mert van hozzákapcsolt adat!";
                    return RedirectToPage("./Index", new { pageNumber = CurrentPage, searchTerm = SearchTerm, pageSize = PageSize });
                }

                var success = await _service.DeleteAsync(contactId);
                if (!success)
                {
                    TempData["ErrorMessage"] = "Kontakt nem található!";
                    return RedirectToPage("./Index", new { pageNumber = CurrentPage, searchTerm = SearchTerm, pageSize = PageSize });
                }
                TempData["SuccessMessage"] = "Kontakt törölve sikeresen!";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting contact {ContactId}", contactId);
                TempData["ErrorMessage"] = "Kontakt törlése sikertelen. Próbálja újra.";
            }
            return RedirectToPage("./Index", new { pageNumber = CurrentPage, searchTerm = SearchTerm, pageSize = PageSize });
        }
    }
}