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
using Cloud9_2.Services;

namespace Cloud9_2.Pages.CRM.Partners
{
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<IndexModel> _logger;
        private readonly IPartnerService _partnerService;

        public IndexModel(ApplicationDbContext context, UserManager<ApplicationUser> userManager, ILogger<IndexModel> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public IList<Partner> Partners { get; set; } = new List<Partner>();
        public IList<Status> AvailableStatuses { get; set; } = new List<Status>(); // Add this property
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

            // Define mapping of English to Hungarian column names
            var columnNameMapping = new Dictionary<string, string>
                {
                    { "Name", "Név" },
                    { "Email", "Email" },
                    { "PhoneNumber", "Telefonszám" },
                    { "TaxId", "Adószám" },
                    { "AddressLine1", "Címsor 1" },
                    { "AddressLine2", "Címsor 2" },
                    { "City", "Város" },
                    { "State", "Megye" },
                    { "PostalCode", "Irányítószám" },
                    { "Status", "Státusz" },
                    { "PreferredCurrency", "Pénznem" },
                    { "AssignedTo", "Felelős" }
                };

            if (User.IsInRole("SuperAdmin"))
            {
                VisibleColumns = new HashSet<string>
        {
            "Név", "Email", "Telefonszám", "Adószám", "Címsor 1", "Címsor 2",
            "Város", "Megye", "Irányítószám", "Státusz", "Pénznem", "Felelős"
        };
            }
            else
            {
                VisibleColumns = permissions
                    .Where(p => p.ColumnName != null && p.CanViewColumn)
                    .Select(p => columnNameMapping.ContainsKey(p.ColumnName!) ? columnNameMapping[p.ColumnName!] : p.ColumnName!)
                    .ToHashSet();
            }

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
                .Include(p => p.Sites)
                .Include(p => p.Contacts)
                .Include(p => p.Documents)
                .Include(p => p.Status)
                .OrderByDescending(p => p.PartnerId)
                .Skip((CurrentPage - 1) * PageSize)
                .Take(PageSize)
                .ToListAsync();

            AvailableStatuses = await GetStatusesAsync();
            return Page();
        }

        public async Task<List<Status>> GetStatusesAsync()
        {
            return await _context.PartnerStatuses
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<IActionResult> OnPostChangeStatusAsync(int partnerId, int newStatusId, string searchTerm, int pageNumber, int pageSize)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var roles = await _userManager.GetRolesAsync(user);
            bool canEdit = roles.Contains("SuperAdmin") || await _context.AccessPermissions
                .Join(_context.Roles, p => p.RoleId, r => r.Id, (p, r) => new { Permission = p, RoleName = r.Name })
                .AnyAsync(x => roles.Contains(x.RoleName) && x.Permission.PagePath == "/CRM/PartnersCanEdit");

            if (!canEdit) return Forbid();

            var partner = await _context.Partners.FindAsync(partnerId);
            if (partner == null)
            {
                _logger.LogWarning("Partner with ID {PartnerId} not found.", partnerId);
                return NotFound();
            }

            var status = await _context.PartnerStatuses.FindAsync(newStatusId);
            if (status == null)
            {
                _logger.LogWarning("Status with ID {StatusId} not found.", newStatusId);
                return BadRequest("Invalid status.");
            }

            partner.StatusId = newStatusId;
            _context.Partners.Update(partner);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Status for Partner ID {PartnerId} updated to {StatusName}.", partnerId, status.Name);

            return RedirectToPage("./Index", new { searchTerm, pageNumber, pageSize });
        }



        public async Task<IActionResult> OnPostCreatePartnerAsync()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(NewPartner.Name))
                {
                    TempData["ErrorMessage"] = "Partner name is required";
                    return RedirectToPage();
                }

                NewPartner.CreatedDate = DateTime.Now;
                NewPartner.CreatedBy = User.Identity?.Name ?? "System";
                NewPartner.UpdatedDate = DateTime.Now;
                NewPartner.UpdatedBy = User.Identity?.Name ?? "System";

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

        [BindProperty]
        public PartnerDto EditPartnerDto { get; set; } = new PartnerDto();

        public async Task<IActionResult> OnPostEditPartnerAsync(int partnerId)
        {
            if (partnerId != EditPartnerDto.PartnerId)
            {
                _logger.LogWarning("ID mismatch: URL ID={UrlId}, DTO ID={DtoId}", partnerId, EditPartnerDto.PartnerId);
                TempData["ErrorMessage"] = "ID mismatch.";
                return RedirectToPage("./Index", new { pageNumber = CurrentPage, pageSize = PageSize, searchTerm = SearchTerm });
            }

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Invalid model state for PartnerId: {PartnerId}, Errors: {@Errors}", partnerId, ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                TempData["ErrorMessage"] = "Invalid partner data.";
                await OnGetAsync(CurrentPage, PageSize, SearchTerm);
                return Page();
            }

            try
            {
                var updatedPartner = await _partnerService.UpdatePartnerAsync(partnerId, EditPartnerDto);
                if (updatedPartner == null)
                {
                    _logger.LogWarning("Partner {PartnerId} not found", partnerId);
                    TempData["ErrorMessage"] = $"Partner {partnerId} not found.";
                    return NotFound();
                }

                _logger.LogInformation("Partner {PartnerId} updated successfully", partnerId);
                TempData["SuccessMessage"] = "Partner updated successfully.";
                return RedirectToPage("./Index", new { pageNumber = CurrentPage, pageSize = PageSize, searchTerm = SearchTerm });
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Validation error updating partner {PartnerId}: {Message}", partnerId, ex.Message);
                TempData["ErrorMessage"] = ex.Message;
                await OnGetAsync(CurrentPage, PageSize, SearchTerm);
                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating partner {PartnerId}", partnerId);
                TempData["ErrorMessage"] = "Failed to update partner.";
                await OnGetAsync(CurrentPage, PageSize, SearchTerm);
                return Page();
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
                if (string.IsNullOrWhiteSpace(NewContact.FirstName) ||
                    string.IsNullOrWhiteSpace(NewContact.LastName))
                {
                    TempData["ContactError"] = "First and Last name are required";
                    return RedirectToPage();
                }

                var partner = await _context.Partners
                    .Include(p => p.Contacts)
                    .FirstOrDefaultAsync(p => p.PartnerId == partnerId);

                if (partner == null)
                {
                    TempData["ContactError"] = "Partner not found";
                    return RedirectToPage();
                }

                if (NewContact.IsPrimary)
                {
                    var currentPrimary = partner.Contacts.FirstOrDefault(c => c.IsPrimary);
                    if (currentPrimary != null)
                    {
                        currentPrimary.IsPrimary = false;
                        _context.Update(currentPrimary);
                    }
                }

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

                return RedirectToPage(new
                {
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
                _logger.LogInformation($"Uploading document for partner {partnerId}, type {documentTypeId}, site {siteId}");

                if (File == null || File.Length == 0)
                {
                    _logger.LogWarning("No file was uploaded");
                    TempData["ErrorMessage"] = "Please select a file";
                    return RedirectToPage();
                }

                var uploadsDir = Path.Combine("wwwroot", "Uploads", "documents");
                Directory.CreateDirectory(uploadsDir);

                var uniqueFileName = $"{Guid.NewGuid()}{Path.GetExtension(File.FileName)}";
                var filePath = Path.Combine(uploadsDir, uniqueFileName);

                _logger.LogInformation($"Saving file to {filePath}");
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await File.CopyToAsync(stream);
                }

                var document = new Document
                {
                    FileName = File.FileName,
                    FilePath = $"/Uploads/documents/{uniqueFileName}",
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

        public async Task<IActionResult> OnPostDeletePartnerAsync(int partnerId, int pageNumber, int pageSize, string searchTerm)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                    return Request.Headers["X-Requested-With"] == "XMLHttpRequest"
                        ? new JsonResult(new { success = false, message = "User not authenticated" }) { StatusCode = 401 }
                        : Challenge();

                var roles = await _userManager.GetRolesAsync(user);
                bool canDelete = roles.Contains("SuperAdmin") || await _context.AccessPermissions
                    .Join(_context.Roles, p => p.RoleId, r => r.Id, (p, r) => new { Permission = p, RoleName = r.Name })
                    .AnyAsync(x => roles.Contains(x.RoleName) && x.Permission.PagePath == "/CRM/PartnersCanEdit");

                if (!canDelete)
                {
                    _logger.LogWarning("User {UserId} does not have permission to delete partner {PartnerId}", user.Id, partnerId);
                    return Request.Headers["X-Requested-With"] == "XMLHttpRequest"
                        ? new JsonResult(new { success = false, message = "You do not have permission to delete partners." }) { StatusCode = 403 }
                        : Forbid();
                }

                var result = await _partnerService.DeletePartnerAsync(partnerId);
                if (!result)
                {
                    _logger.LogWarning("Partner not found for deletion, PartnerId: {PartnerId}", partnerId);
                    return Request.Headers["X-Requested-With"] == "XMLHttpRequest"
                        ? new JsonResult(new { success = false, message = $"Partner {partnerId} not found." }) { StatusCode = 404 }
                        : NotFound();
                }

                _logger.LogInformation("Deleted partner with PartnerId: {PartnerId} by User: {UserId}", partnerId, user.Id);
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return new JsonResult(new { success = true, message = "Partner deleted successfully." });
                }

                TempData["SuccessMessage"] = "Partner deleted successfully.";
                return RedirectToPage("./Index", new { pageNumber, pageSize, searchTerm });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Cannot delete PartnerId: {PartnerId} due to related records", partnerId);
                return Request.Headers["X-Requested-With"] == "XMLHttpRequest"
                    ? new JsonResult(new { success = false, message = ex.Message }) { StatusCode = 400 }
                    : BadRequest(TempData["ErrorMessage"] = ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting PartnerId: {PartnerId}", partnerId);
                return Request.Headers["X-Requested-With"] == "XMLHttpRequest"
                    ? new JsonResult(new { success = false, message = "An error occurred while deleting the partner." }) { StatusCode = 500 }
                    : StatusCode(500, TempData["ErrorMessage"] = "An error occurred while deleting the partner.");
            }
        }

        
    }
}