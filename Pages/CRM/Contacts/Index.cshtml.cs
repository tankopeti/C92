using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Cloud9_2.Models;
using Cloud9_2.Data;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Cloud9_2.Pages.CRM.Contacts
{
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<IndexModel> _logger;

        [BindProperty(SupportsGet = true)]
        public int PageNumber { get; set; } = 1;

        public int PageSize { get; set; } = 10;
        public int CurrentPage { get; set; } = 1;
        public int TotalRecords { get; set; }
        public int TotalPages { get; set; }
        [BindProperty(SupportsGet = true)]
        public string SearchTerm { get; set; }
        public List<Partner> PartnerList { get; set; }
        public List<Status> StatusList { get; set; }

        public IndexModel(ApplicationDbContext context, ILogger<IndexModel> logger)
        {
            _context = context;
            _logger = logger;
        }

        public List<ContactViewModel> Contacts { get; set; }

        public class ContactViewModel
        {
            public int ContactId { get; set; }
            public string FirstName { get; set; }
            public string LastName { get; set; }
            public string Email { get; set; }
            public string PhoneNumber { get; set; }
            public string PhoneNumber2 { get; set; }
            public string Comment { get; set; }
            public string Comment2 { get; set; }
            public string PartnerName { get; set; }
            public int PartnerId { get; set; }
            public int? StatusId { get; set; }
            public string StatusName { get; set; }
            public string StatusColor { get; set; }
        }

        public async Task OnGetAsync()
        {
            CurrentPage = PageNumber;

            var query = _context.Contacts
                .Include(c => c.Partner)
                .Include(c => c.Status)
                .AsQueryable();

            if (!string.IsNullOrEmpty(SearchTerm))
            {
                query = query.Where(c =>
                    c.FirstName.Contains(SearchTerm) ||
                    c.LastName.Contains(SearchTerm) ||
                    c.Email.Contains(SearchTerm) ||
                    c.PhoneNumber.Contains(SearchTerm) ||
                    c.PhoneNumber2.Contains(SearchTerm) ||
                    c.Comment.Contains(SearchTerm) ||
                    c.Comment2.Contains(SearchTerm) ||
                    (c.Partner != null && c.Partner.Name.Contains(SearchTerm)) ||
                    (c.Status != null && c.Status.Name.Contains(SearchTerm)));
            }

            TotalRecords = await query.CountAsync();
            TotalPages = (int)Math.Ceiling(TotalRecords / (double)PageSize);

            var rawContacts = await query
                .Select(c => new
                {
                    c.ContactId,
                    c.FirstName,
                    c.LastName,
                    c.Email,
                    c.PhoneNumber,
                    c.PhoneNumber2,
                    c.Comment,
                    c.Comment2,
                    c.PartnerId,
                    PartnerName = c.Partner != null ? c.Partner.Name : null,
                    c.StatusId,
                    StatusName = c.Status != null ? c.Status.Name : null,
                    StatusColor = c.Status != null ? c.Status.Color : null
                })
                .OrderByDescending(c => c.ContactId)
                .ThenBy(c => c.FirstName)
                .Skip((PageNumber - 1) * PageSize)
                .Take(PageSize)
                .ToListAsync();

            Contacts = rawContacts.Select(c => new ContactViewModel
            {
                ContactId = c.ContactId,
                FirstName = c.FirstName,
                LastName = c.LastName,
                Email = c.Email,
                PhoneNumber = c.PhoneNumber,
                PhoneNumber2 = c.PhoneNumber2,
                Comment = c.Comment,
                Comment2 = c.Comment2,
                PartnerName = string.IsNullOrEmpty(c.PartnerName) ? "No Partner" : c.PartnerName,
                PartnerId = c.PartnerId,
                StatusId = c.StatusId,
                StatusName = string.IsNullOrEmpty(c.StatusName) ? "No Status" : c.StatusName,
                StatusColor = c.StatusColor ?? "#000000" // Default to black if no color
            }).ToList();

            // Debug logging
            foreach (var contact in rawContacts)
            {
                _logger.LogInformation("ContactId: {ContactId}, PartnerId: {PartnerId}, RawPartnerName: {PartnerName}, StatusId: {StatusId}, StatusName: {StatusName}, StatusColor: {StatusColor}",
                    contact.ContactId, contact.PartnerId, contact.PartnerName ?? "null", contact.StatusId, contact.StatusName ?? "null", contact.StatusColor ?? "null");
                if (contact.PartnerId > 0 && contact.PartnerName == null)
                {
                    _logger.LogWarning("ContactId {ContactId} has PartnerId {PartnerId} but PartnerName is null",
                        contact.ContactId, contact.PartnerId);
                }
                if (contact.StatusId.HasValue && contact.StatusName == null)
                {
                    _logger.LogWarning("ContactId {ContactId} has StatusId {StatusId} but StatusName is null",
                        contact.ContactId, contact.StatusId);
                }
            }

            PartnerList = await _context.Partners
                .OrderBy(p => p.Name)
                .Take(10)
                .ToListAsync();

            StatusList = await _context.PartnerStatuses
                .OrderBy(s => s.Name)
                .Take(10)
                .ToListAsync();

            _logger.LogInformation("Loaded {Count} partners: {Partners}",
                PartnerList.Count,
                string.Join(", ", PartnerList.Select(p => $"PartnerId: {p.PartnerId}, Name: {p.Name ?? "null"}")));
            _logger.LogInformation("Loaded {Count} statuses: {Statuses}",
                StatusList.Count,
                string.Join(", ", StatusList.Select(s => $"StatusId: {s.Id}, Name: {s.Name ?? "null"}, Color: {s.Color ?? "null"}")));
        }

        public async Task<IActionResult> OnPostCreateContactAsync(string firstName, string lastName, string email, string phoneNumber, string phoneNumber2, string comment, string comment2, int? partnerId, int? statusId)
        {
            var contact = new Contact
            {
                FirstName = firstName,
                LastName = lastName,
                Email = email,
                PhoneNumber = phoneNumber,
                PhoneNumber2 = phoneNumber2,
                Comment = comment,
                Comment2 = comment2,
                PartnerId = partnerId ?? 0,
                StatusId = statusId
            };

            try
            {
                _context.Contacts.Add(contact);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Kontakt sikeresen létrehozva";
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Hiba történt a kontakt létrehozása során");
                TempData["ErrorMessage"] = "Hiba történt a kontakt létrehozása során";
                return Page();
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostEditContactAsync(int contactId, string firstName, string lastName, string email, string phoneNumber, string phoneNumber2, string comment, string comment2, int? partnerId, int? statusId)
        {
            var contact = await _context.Contacts.FindAsync(contactId);
            if (contact == null)
            {
                return NotFound();
            }

            contact.FirstName = firstName;
            contact.LastName = lastName;
            contact.Email = email;
            contact.PhoneNumber = phoneNumber;
            contact.PhoneNumber2 = phoneNumber2;
            contact.Comment = comment;
            contact.Comment2 = comment2;
            contact.PartnerId = partnerId ?? 0;
            contact.StatusId = statusId;

            try
            {
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Kontakt sikeresen módosítva";
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Hiba történt a kontakt módosítása során");
                TempData["ErrorMessage"] = "Hiba történt a kontakt módosítása során";
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostDeleteContactAsync(int contactId)
        {
            var contact = await _context.Contacts.FindAsync(contactId);
            if (contact == null)
            {
                return NotFound();
            }

            try
            {
                _context.Contacts.Remove(contact);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Kontakt sikeresen törölve";
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Hiba történt a kontakt törlése során");
                TempData["ErrorMessage"] = "Hiba történt a kontakt törlése során";
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnGetPartners(string term, int page = 1)
        {
            _logger.LogInformation("OnGetPartners called with term: {Term}, page: {Page}", term, page);
            const int pageSize = 10;
            var query = _context.Partners.AsQueryable();

            if (!string.IsNullOrEmpty(term))
            {
                query = query.Where(p => p.Name.Contains(term, StringComparison.OrdinalIgnoreCase));
            }

            var partners = await query
                .OrderBy(p => p.Name)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(p => new { id = p.PartnerId, text = p.Name ?? "Unnamed Partner" })
                .ToListAsync();

            var total = await query.CountAsync();
            _logger.LogInformation("Found {Count} partners, returning {PageSize} for page {Page}: {Partners}",
                total, partners.Count, page, string.Join(", ", partners.Select(p => $"Id: {p.id}, Name: {p.text}")));

            return new JsonResult(new
            {
                results = partners,
                pagination = new { more = (page * pageSize) < total }
            });
        }

        public async Task<IActionResult> OnGetStatuses(string term, int page = 1)
        {
            _logger.LogInformation("OnGetStatuses called with term: {Term}, page: {Page}", term, page);
            const int pageSize = 10;
            var query = _context.PartnerStatuses.AsQueryable();

            if (!string.IsNullOrEmpty(term))
            {
                query = query.Where(s => s.Name.Contains(term, StringComparison.OrdinalIgnoreCase));
            }

            var statuses = await query
                .OrderBy(s => s.Name)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(s => new { id = s.Id, text = s.Name ?? "Unnamed Status", color = s.Color })
                .ToListAsync();

            var total = await query.CountAsync();
            _logger.LogInformation("Found {Count} statuses, returning {PageSize} for page {Page}: {Statuses}",
                total, statuses.Count, page, string.Join(", ", statuses.Select(s => $"Id: {s.id}, Name: {s.text}, Color: {s.color ?? "null"}")));

            return new JsonResult(new
            {
                results = statuses,
                pagination = new { more = (page * pageSize) < total }
            });
        }
    }
}