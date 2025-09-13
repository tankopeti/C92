using Cloud9_2.Data;
using Cloud9_2.Models;
using Cloud9_2.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Cloud9_2.Pages.DocManagement.Doc
{
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<IndexModel> _logger;
        private readonly IDocumentService _documentService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IWebHostEnvironment _environment; // Added

        public IndexModel(
            ApplicationDbContext context,
            ILogger<IndexModel> logger,
            IDocumentService documentService,
            UserManager<ApplicationUser> userManager,
            IWebHostEnvironment environment) // Added
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _documentService = documentService ?? throw new ArgumentNullException(nameof(documentService));
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
            _environment = environment ?? throw new ArgumentNullException(nameof(environment)); // Added
        }

        public IList<DocumentDto>? Documents { get; set; }
        public string? SearchTerm { get; set; }
        public string? StatusFilter { get; set; }
        public string? SortBy { get; set; }
        public int CurrentPage { get; set; }
        public int PageSize { get; set; }
        public int TotalRecords { get; set; }
        public int TotalPages { get; set; }
        public int DistinctDocumentIdCount { get; set; }
        public string? NextDocumentNumber { get; set; }
        public IDictionary<string, string> StatusDisplayNames { get; set; } = GetStatusDisplayNames();

        private static IDictionary<string, string> GetStatusDisplayNames()
        {
            return Enum.GetValues(typeof(DocumentStatusEnum))
                .Cast<DocumentStatusEnum>()
                .ToDictionary(
                    e => e.ToString(),
                    e => e switch
                    {
                        DocumentStatusEnum.Beérkezett => "Beérkezett",
                        DocumentStatusEnum.Függőben => "Függőben",
                        DocumentStatusEnum.Elfogadott => "Elfogadott",
                        DocumentStatusEnum.Lezárt => "Lezárt",
                        DocumentStatusEnum.Jóváhagyandó => "Jóváhagyandó",
                        _ => e.ToString()
                    });
        }

        public async Task OnGetAsync(int? pageNumber, string? searchTerm, int? pageSize, string? statusFilter, string? sortBy)
        {
            // Unchanged OnGetAsync implementation
            try
            {
                SearchTerm = searchTerm ?? string.Empty;
                StatusFilter = statusFilter ?? string.Empty;
                SortBy = sortBy ?? "uploaddate";
                CurrentPage = pageNumber ?? 1;
                PageSize = pageSize ?? 10;

                var user = await _userManager.GetUserAsync(User);
                var isAdmin = user != null && (await _userManager.IsInRoleAsync(user, "Admin") || await _userManager.IsInRoleAsync(user, "SuperAdmin"));

                DocumentStatusEnum? status = null;
                if (!string.IsNullOrEmpty(statusFilter) && statusFilter != "all" && Enum.TryParse<DocumentStatusEnum>(statusFilter, true, out var parsedStatus))
                {
                    status = parsedStatus;
                }

                Documents = await _documentService.GetDocumentsAsync(
                    SearchTerm,
                    documentTypeId: null,
                    partnerId: null,
                    siteId: null,
                    status,
                    SortBy,
                    skip: (CurrentPage - 1) * PageSize,
                    take: PageSize);

                TotalRecords = await _documentService.GetDocumentsCountAsync(
                    SearchTerm,
                    documentTypeId: null,
                    partnerId: null,
                    siteId: null);

                TotalPages = (int)Math.Ceiling((double)TotalRecords / PageSize);
                DistinctDocumentIdCount = await _documentService.GetDocumentsCountAsync(
                    searchTerm: string.Empty,
                    documentTypeId: null,
                    partnerId: null,
                    siteId: null);

                NextDocumentNumber = await _documentService.GetNextDocumentNumberAsync();
                ViewData["ModalViewModel"] = new DocumentModalViewModel
                {
                    CreateDocument = new CreateDocumentDto(),
                    DocumentTypes = await _context.DocumentTypes
                        .AsNoTracking()
                        .Select(dt => new SelectListItem { Value = dt.DocumentTypeId.ToString(), Text = dt.Name })
                        .ToListAsync(),
                    Partners = await _context.Partners
                        .AsNoTracking()
                        .Select(p => new SelectListItem { Value = p.PartnerId.ToString(), Text = p.Name })
                        .ToListAsync(),
                    Sites = new List<SelectListItem>(),
                    NextDocumentNumber = NextDocumentNumber
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in OnGetAsync: {Message}", ex.Message);
                TempData["ErrorMessage"] = "An error occurred while loading the page.";
                Documents = new List<DocumentDto>();
                throw;
            }
        }

        public async Task<IActionResult> OnPostCreateAsync([FromForm] CreateDocumentDto createDocument, [FromForm] IFormFile? file, string? returnUrl)
        {
            _logger.LogInformation("OnPostCreateAsync called. File: {FileName}", file?.FileName);

            if (file == null || file.Length == 0)
            {
                _logger.LogWarning("No file uploaded");
                ViewData["ErrorMessage"] = "Please select a file.";
                return Page();
            }

            var allowedExtensions = new[] { ".pdf", ".doc", ".docx" };
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(extension))
            {
                _logger.LogWarning("Invalid file extension: {Extension}", extension);
                ViewData["ErrorMessage"] = "Only PDF, DOC, and DOCX files are allowed.";
                return Page();
            }

            if (file.Length > 5 * 1024 * 1024)
            {
                _logger.LogWarning("File size too large: {Size}", file.Length);
                ViewData["ErrorMessage"] = "File size must not exceed 5MB.";
                return Page();
            }

            var uploadsDir = Path.Combine(_environment.WebRootPath, "Uploads");
            _logger.LogInformation("Uploads directory: {UploadsDir}", uploadsDir);
            try
            {
                if (!Directory.Exists(uploadsDir))
                {
                    Directory.CreateDirectory(uploadsDir);
                    _logger.LogInformation("Created uploads directory");
                }
                var fileName = Guid.NewGuid().ToString() + extension;
                var filePath = Path.Combine(uploadsDir, fileName);
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                    _logger.LogInformation("File saved to {FilePath}", filePath);
                }

                var currentUserName = _userManager.GetUserName(User) ?? "System";
                var document = new Document
                {
                    FileName = file.FileName,
                    DocumentTypeId = createDocument.DocumentTypeId,
                    FilePath = $"/Uploads/{fileName}",
                    UploadDate = DateTime.UtcNow,
                    PartnerId = createDocument.PartnerId,
                    SiteId = createDocument.SiteId,
                    Status = createDocument.Status,
                    UploadedBy = _userManager.GetUserName(User) ?? "System",
                    StatusHistory = new List<DocumentStatusHistory>
                    {
                        new DocumentStatusHistory
                        {
                        NewStatus = createDocument.Status,
                        ChangeDate = DateTime.UtcNow,
                        ChangedBy = currentUserName
                        }
                    }
                    
                };
                _context.Documents.Add(document);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Saved document {DocumentId}", document.DocumentId);

                TempData["SuccessMessage"] = $"Document {document.DocumentId} uploaded successfully.";
                return Redirect(returnUrl ?? "/DocManagement/Doc/Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving file or document: {Message}", ex.Message);
                ViewData["ErrorMessage"] = "Error uploading the file.";
                return Page();
            }
        }

        // Other methods (OnPostUpdateStatusAsync, OnPostDeleteAsync, LoadPageDataAsync) remain unchanged
        // For brevity, they are not included here but should be kept as in your original code
    }
}