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
            public string PartnerName { get; set; }
            public int PartnerId { get; set; } // Remains int
        }

        public async Task OnGetAsync()
        {
            CurrentPage = PageNumber;

            var query = _context.Contacts
                .Include(c => c.Partner)
                .AsQueryable();

            if (!string.IsNullOrEmpty(SearchTerm))
            {
                query = query.Where(c =>
                    c.FirstName.Contains(SearchTerm) ||
                    c.LastName.Contains(SearchTerm) ||
                    c.Email.Contains(SearchTerm) ||
                    c.PhoneNumber.Contains(SearchTerm) ||
                    (c.Partner != null && c.Partner.Name.Contains(SearchTerm)));
            }

            TotalRecords = await query.CountAsync();
            TotalPages = (int)Math.Ceiling(TotalRecords / (double)PageSize);

            Contacts = await query
                .Select(c => new ContactViewModel
                {
                    ContactId = c.ContactId,
                    FirstName = c.FirstName,
                    LastName = c.LastName,
                    Email = c.Email,
                    PhoneNumber = c.PhoneNumber,
                    PartnerName = c.Partner != null ? c.Partner.Name : null,
                    PartnerId = c.PartnerId // No change needed
                })
                .OrderBy(c => c.LastName)
                .ThenBy(c => c.FirstName)
                .Skip((PageNumber - 1) * PageSize)
                .Take(PageSize)
                .ToListAsync();

            PartnerList = await _context.Partners
                .OrderBy(p => p.Name)
                .Take(10)
                .ToListAsync();
        }

        public async Task<IActionResult> OnPostCreateContactAsync(string firstName, string lastName, string email, string phoneNumber, int? partnerId)
        {
            var contact = new Contact
            {
                FirstName = firstName,
                LastName = lastName,
                Email = email,
                PhoneNumber = phoneNumber,
                PartnerId = partnerId ?? 0 // Default to 0 if no partner selected
            };

            try
            {
                _context.Contacts.Add(contact);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Contact created successfully";
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Error creating contact");
                TempData["ErrorMessage"] = "Error creating contact";
                return Page();
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostEditContactAsync(int contactId, string firstName, string lastName, string email, string phoneNumber)
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

            try
            {
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Contact updated successfully";
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Error updating contact");
                TempData["ErrorMessage"] = "Error updating contact";
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
                TempData["SuccessMessage"] = "Contact deleted successfully";
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Error deleting contact");
                TempData["ErrorMessage"] = "Error deleting contact";
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
                .Select(p => new { id = p.PartnerId, text = p.Name })
                .ToListAsync();

            var total = await query.CountAsync();
            _logger.LogInformation("Found {Count} partners, returning {PageSize} for page {Page}", total, partners.Count, page);

            return new JsonResult(new
            {
                results = partners,
                pagination = new { more = (page * pageSize) < total }
            });
        }
    }
}