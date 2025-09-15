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
using Microsoft.Extensions.Logging;
using System.Text.Json;


namespace Cloud9_2.Pages.CRM.Partners
{
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<IndexModel> _logger;

        public IndexModel(ApplicationDbContext context, UserManager<ApplicationUser> userManager, ILogger<IndexModel> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context)); // Added null checks - good practice
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger)); // <-- Assignment must happen AND logger parameter itself shouldn't be null
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
        public IFormFile File { get; set; }
        public List<DocumentType> DocumentTypes { get; set; } = new List<DocumentType>();

        [BindProperty]
        public Partner NewPartner { get; set; }

        [BindProperty]
        public Contact NewContact { get; set; } = new Contact();

        public async Task<IActionResult> OnGetAsync(int? pageNumber, int? pageSize, string searchTerm)
        {

            // DocumentTypes = await _context.DocumentTypes
            //         .OrderBy(dt => dt.Name)
            //         .ToListAsync();

            var query = _context.Partners
                    .Include(p => p.Sites)
                    .Include(p => p.Contacts)
                    .Include(p => p.Documents)
                    // .ThenInclude(d => d.DocumentType)
                    .AsQueryable();

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
                .Include(p => p.Sites)      // Include Sites
                .Include(p => p.Contacts)   // Include Contacts
                .Include(p => p.Documents)  // Include Documents
                .OrderBy(p => p.PartnerId)
                .Skip((CurrentPage - 1) * PageSize)
                .Take(PageSize)
                .ToListAsync();

            return Page();
        }

        public async Task<IActionResult> OnPostCreatePartnerAsync()
{
    try
    {
        // Manual validation for required fields
        if (string.IsNullOrWhiteSpace(NewPartner.Name))
        {
            TempData["ErrorMessage"] = "Partner name is required";
            return RedirectToPage();
        }

        // Set audit fields
        NewPartner.CreatedDate = DateTime.Now;
        NewPartner.CreatedBy = User.Identity?.Name ?? "System";
        NewPartner.UpdatedDate = DateTime.Now;
        NewPartner.UpdatedBy = User.Identity?.Name ?? "System";

        // Clear navigation properties to avoid circular references
        NewPartner.Contacts = null;
        NewPartner.Documents = null;
        NewPartner.Sites = null;

        _context.Partners.Add(NewPartner);
        var changes = await _context.SaveChangesAsync();

        if (changes > 0)
        {
            TempData["SuccessMessage"] = $"Partner '{NewPartner.Name}' created successfully!";
            return RedirectToPage();
        }
        else
        {
            TempData["ErrorMessage"] = "No changes were saved to the database";
            return RedirectToPage();
        }
    }
    catch (DbUpdateException ex)
    {
        _logger.LogError(ex, "Database error creating partner");
        TempData["ErrorMessage"] = $"Database error: {ex.InnerException?.Message ?? ex.Message}";
        return RedirectToPage();
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error creating partner");
        TempData["ErrorMessage"] = $"Error creating partner: {ex.Message}";
        return RedirectToPage();
    }
}


        [HttpPost]
        public async Task<IActionResult> OnPostDeletePartner(int partnerId)
        {
            var partner = await _context.Partners
                .Include(p => p.Sites)
                .Include(p => p.Contacts)
                .Include(p => p.Documents)
                .FirstOrDefaultAsync(p => p.PartnerId == partnerId);

            if (partner == null)
            {
                return NotFound();
            }

            // Check for related records
            if (partner.Sites.Any() || partner.Contacts.Any() || partner.Documents.Any())
            {
                TempData["ErrorMessage"] = "Cannot delete partner with related records.";
                return RedirectToPage("./Index");
            }

            _context.Partners.Remove(partner);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Partner deleted successfully.";
            return RedirectToPage("./Index");
        }

        [HttpPost]
        public async Task<IActionResult> OnPostEditPartner(int partnerId, string Name, string Email, string PhoneNumber, string AlternatePhone, string Website, string CompanyName, string TaxId, string IntTaxId, string Industry, string AddressLine1, string AddressLine2, string City, string State, string PostalCode, string Country, string Status, DateTime? LastContacted, string Notes, string AssignedTo, string BillingContactName, string BillingEmail, string PaymentTerms, int? CreditLimit, string PreferredCurrency, bool IsTaxExempt, DateTime? CreatedDate, string CreatedBy, DateTime? UpdatedDate, string UpdatedBy)
        {
            try
            {
                _logger.LogInformation($"Attempting to edit Partner ID: {partnerId}, Name: {Name}, Email: {Email}");
                var partner = await _context.Partners.FindAsync(partnerId);
                if (partner == null)
                {
                    _logger.LogWarning($"Partner {partnerId} not found");
                    return NotFound();
                }

                partner.Name = Name;
                partner.Email = Email;
                partner.PhoneNumber = PhoneNumber;
                partner.AlternatePhone = AlternatePhone;
                partner.Website = Website;
                partner.CompanyName = CompanyName;
                partner.TaxId = TaxId;
                partner.IntTaxId = IntTaxId;
                partner.Industry = Industry;
                partner.AddressLine1 = AddressLine1;
                partner.AddressLine2 = AddressLine2;
                partner.City = City;
                partner.State = State;
                partner.PostalCode = PostalCode;
                partner.Country = Country;
                partner.Status = Status;
                partner.LastContacted = LastContacted;
                partner.Notes = Notes;
                partner.AssignedTo = AssignedTo;
                partner.BillingContactName = BillingContactName;
                partner.BillingEmail = BillingEmail;
                partner.PaymentTerms = PaymentTerms;
                partner.CreditLimit = CreditLimit;
                partner.PreferredCurrency = PreferredCurrency;
                partner.IsTaxExempt = IsTaxExempt;
                partner.CreatedDate = CreatedDate ?? partner.CreatedDate;
                partner.CreatedBy = CreatedBy ?? partner.CreatedBy;
                partner.UpdatedDate = DateTime.Now;
                partner.UpdatedBy = User.Identity?.Name ?? "Unknown";

                _context.Partners.Update(partner);
                await _context.SaveChangesAsync();
                _logger.LogInformation($"Partner {partnerId} updated successfully");
                return RedirectToPage("./Index", new { pageNumber = CurrentPage, pageSize = PageSize, searchTerm = SearchTerm });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error editing partner {partnerId}");
                return StatusCode(500, "Failed to edit partner");
            }
        }

        [HttpGet("/CRM/Partners/CheckRelatedRecords")]
        public async Task<IActionResult> OnGetCheckRelatedRecords(int id)
        {
            Console.WriteLine($"Checking ID: {id}");
            var partner = await _context.Partners
                .Include(p => p.Sites)
                .Include(p => p.Contacts)
                .Include(p => p.Documents)
                .FirstOrDefaultAsync(p => p.PartnerId == id);
            var hasRelatedRecords = partner != null && (partner.Sites.Any() || partner.Contacts.Any() || partner.Documents.Any());
            return new JsonResult(new { hasRelatedRecords });
        }

        [HttpGet("/CRM/Partners/GetPartner")]
        public async Task<IActionResult> OnGetGetPartnerAsync(int id)
        {
            _logger.LogInformation("AJAX Handler OnGetGetPartnerAsync called for ID: {PartnerId}", id);
            try
            {
                var partner = await _context.Partners
                    .AsNoTracking()
                    .Where(p => p.PartnerId == id)
                    .Select(p => new
                    {
                        partnerId = p.PartnerId,
                        name = p.Name,
                        email = p.Email,
                        phoneNumber = p.PhoneNumber,
                        alternatePhone = p.AlternatePhone,
                        website = p.Website,
                        companyName = p.CompanyName,
                        taxId = p.TaxId,
                        intTaxId = p.IntTaxId,
                        industry = p.Industry,
                        addressLine1 = p.AddressLine1,
                        addressLine2 = p.AddressLine2,
                        city = p.City,
                        state = p.State,
                        postalCode = p.PostalCode,
                        country = p.Country,
                        status = p.Status,
                        lastContacted = p.LastContacted.HasValue ? p.LastContacted.Value.ToString("yyyy-MM-dd") : null,
                        notes = p.Notes,
                        assignedTo = p.AssignedTo,
                        billingContactName = p.BillingContactName,
                        billingEmail = p.BillingEmail,
                        paymentTerms = p.PaymentTerms,
                        creditLimit = p.CreditLimit,
                        preferredCurrency = p.PreferredCurrency,
                        isTaxExempt = p.IsTaxExempt,
                        createdDate = p.CreatedDate.HasValue ? p.CreatedDate.Value.ToString("yyyy-MM-ddTHH:mm") : null,
                        createdBy = p.CreatedBy,
                        updatedDate = p.UpdatedDate.HasValue ? p.UpdatedDate.Value.ToString("yyyy-MM-ddTHH:mm") : null,
                        updatedBy = p.UpdatedBy
                    })
                    .FirstOrDefaultAsync();

                if (partner == null)
                {
                    _logger.LogWarning("Partner not found for ID {PartnerId} in OnGetGetPartnerAsync", id);
                    return new JsonResult(new { error = "Partner not found" }) { StatusCode = 404 };
                }

                _logger.LogInformation("Partner data found for ID {PartnerId}. Returning JSON.", id);
                return new JsonResult(partner);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching partner details for ID {PartnerId} in OnGetGetPartnerAsync", id);
                return new JsonResult(new { error = "An internal server error occurred while fetching partner details." }) { StatusCode = 500 };
            }
        }

public async Task<IActionResult> OnPostAddContactAsync(int partnerId)
{
    try
    {
        // Basic validation (since we're not using DataAnnotations)
        if (string.IsNullOrWhiteSpace(NewContact.FirstName) || 
            string.IsNullOrWhiteSpace(NewContact.LastName))
        {
            TempData["ContactError"] = "First and Last name are required";
            return RedirectToPage();
        }

        // Verify partner exists
        var partner = await _context.Partners
            .Include(p => p.Contacts)
            .FirstOrDefaultAsync(p => p.PartnerId == partnerId);

        if (partner == null)
        {
            TempData["ContactError"] = "Partner not found";
            return RedirectToPage();
        }

        // Handle primary contact logic
        if (NewContact.IsPrimary)
        {
            var currentPrimary = partner.Contacts.FirstOrDefault(c => c.IsPrimary);
            if (currentPrimary != null)
            {
                currentPrimary.IsPrimary = false;
                _context.Update(currentPrimary);
            }
        }

        // Set required fields
        NewContact.PartnerId = partnerId;
        
        _context.Contacts.Add(NewContact);
        var changes = await _context.SaveChangesAsync();

        if (changes > 0)
        {
            TempData["SuccessMessage"] = $"Contact {NewContact.FirstName} {NewContact.LastName} added!";
        }
        else
        {
            TempData["ContactError"] = "Contact not saved";
        }

        return RedirectToPage(new {
            pageNumber = CurrentPage,
            pageSize = PageSize,
            searchTerm = SearchTerm
        });
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Contact creation failed");
        TempData["ContactError"] = $"Error saving contact: {ex.Message}";
        return RedirectToPage();
    }
}




public async Task<IActionResult> OnPostAddSite(int partnerId, string siteName, 
    string addressLine1, string addressLine2, string city, 
    string postalCode, string country, bool isPrimary,
    string createdById,
    DateTime createdDate)
{
    try
    {
        var site = new Site
        {
            PartnerId = partnerId,
            SiteName = siteName,
            AddressLine1 = addressLine1,
            AddressLine2 = addressLine2,
            City = city,
            PostalCode = postalCode,
            Country = country,
            IsPrimary = isPrimary,
            CreatedById = createdById,
            CreatedDate = createdDate,
            LastModifiedById = createdById,
            LastModifiedDate = createdDate
        };

        _context.Sites.Add(site);
        await _context.SaveChangesAsync();
        
        TempData["SuccessMessage"] = "Site added successfully";
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error adding site");
        TempData["ErrorMessage"] = "Error adding site";
    }

    return RedirectToPage();
}


        public async Task<IActionResult> OnPostAddDocumentAsync(int partnerId, int? documentTypeId, int? siteId)
{
    try
    {
        // Debug: Log incoming parameters
        _logger.LogInformation($"Uploading document for partner {partnerId}, type {documentTypeId}, site {siteId}");

        if (File == null || File.Length == 0)
        {
            _logger.LogWarning("No file was uploaded");
            TempData["ErrorMessage"] = "Please select a file";
            return RedirectToPage();
        }

        // 1. Save the physical file
        var uploadsDir = Path.Combine("wwwroot", "uploads", "documents");
        Directory.CreateDirectory(uploadsDir); // Ensure directory exists
        
        var uniqueFileName = $"{Guid.NewGuid()}{Path.GetExtension(File.FileName)}";
        var filePath = Path.Combine(uploadsDir, uniqueFileName);

        _logger.LogInformation($"Saving file to {filePath}");
        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await File.CopyToAsync(stream);
        }

        // 2. Create the database record
        var document = new Document
        {
            FileName = File.FileName,
            FilePath = $"/uploads/documents/{uniqueFileName}",
            DocumentTypeId = documentTypeId,
            PartnerId = partnerId,
            SiteId = siteId,
            UploadDate = DateTime.Now,
            UploadedBy = User.Identity?.Name ?? "System"
        };

        _logger.LogInformation($"Creating document record: {JsonSerializer.Serialize(document)}");
        
        _context.Documents.Add(document);
        var changes = await _context.SaveChangesAsync();
        _logger.LogInformation($"Database changes saved: {changes} records affected");

        TempData["SuccessMessage"] = $"Document '{File.FileName}' uploaded successfully!";
        return RedirectToPage();
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Document upload failed");
        TempData["ErrorMessage"] = $"Error: {ex.Message}";
        return RedirectToPage();
    }
}


        private async Task PrepareAndReturnPage()
        {
            await OnGetAsync(CurrentPage, PageSize, SearchTerm);
        }
    }
}