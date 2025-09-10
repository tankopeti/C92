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

        public IndexModel(
            ApplicationDbContext context,
            ILogger<IndexModel> logger,
            IDocumentService documentService,
            UserManager<ApplicationUser> userManager)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _documentService = documentService ?? throw new ArgumentNullException(nameof(documentService));
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
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
            // Validate form data and file
            if (!ModelState.IsValid || (file == null && string.IsNullOrEmpty(createDocument.FilePath)))
            {
                ViewData["ErrorMessage"] = file == null && string.IsNullOrEmpty(createDocument.FilePath) ? "A file must be uploaded." : "Invalid input data.";
                return await LoadPageDataAsync(createDocument);
            }
            
            if (createDocument.PartnerId.HasValue && !createDocument.SiteId.HasValue)
            {
                ModelState.AddModelError("SiteId", "Site is required when Partner is selected.");
                return await LoadPageDataAsync(createDocument);
            }

            if (file != null)
            {
                var allowedExtensions = new[] { ".pdf", ".doc", ".docx" };
                var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
                if (!allowedExtensions.Contains(extension))
                {
                    ViewData["ErrorMessage"] = "Only PDF, DOC, and DOCX files are allowed.";
                    return await LoadPageDataAsync(createDocument);
                }

                if (file.Length > 5 * 1024 * 1024)
                {
                    ViewData["ErrorMessage"] = "File size must not exceed 5MB.";
                    return await LoadPageDataAsync(createDocument);
                }

                var uploadsDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads");
                if (!Directory.Exists(uploadsDir))
                {
                    Directory.CreateDirectory(uploadsDir);
                }
                var fileName = Guid.NewGuid().ToString() + extension;
                var filePath = Path.Combine(uploadsDir, fileName);
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }
                createDocument.FilePath = $"/uploads/{fileName}";
                createDocument.FileName = createDocument.FileName ?? file.FileName;
            }

            try
            {
                var user = await _userManager.GetUserAsync(User);
                var isAdmin = user != null && (await _userManager.IsInRoleAsync(user, "Admin") || await _userManager.IsInRoleAsync(user, "SuperAdmin"));
                var isEditor = user != null && await _userManager.IsInRoleAsync(user, "Editor");

                if (!isAdmin && !isEditor)
                {
                    _logger.LogWarning("User {User} lacks permission to create documents", User.Identity?.Name ?? "Unknown");
                    ViewData["ErrorMessage"] = "You do not have permission to create documents.";
                    return await LoadPageDataAsync(createDocument);
                }

                var createdDoc = await _documentService.CreateDocumentAsync(createDocument);
                TempData["SuccessMessage"] = $"Document {createdDoc.DocumentId} created successfully.";
                _logger.LogInformation("Created document {DocumentId}", createdDoc.DocumentId);
                return Redirect(returnUrl ?? "/DocManagement/Doc/Index");
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning("Unauthorized attempt to create document: {Message}", ex.Message);
                ViewData["ErrorMessage"] = "You do not have permission to create documents.";
                return await LoadPageDataAsync(createDocument);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating document: {Message}", ex.Message);
                ViewData["ErrorMessage"] = "An error occurred while creating the document.";
                return await LoadPageDataAsync(createDocument);
            }
        }

        private async Task<IActionResult> LoadPageDataAsync(CreateDocumentDto createDocument)
        {
            Documents = await _documentService.GetDocumentsAsync(
                SearchTerm ?? string.Empty,
                documentTypeId: null,
                partnerId: null,
                siteId: null,
                status: null,
                SortBy ?? "uploaddate",
                skip: (CurrentPage - 1) * PageSize,
                take: PageSize);
            TotalRecords = await _documentService.GetDocumentsCountAsync(
                SearchTerm ?? string.Empty,
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
                Sites = new List<SelectListItem>(), // Sites are loaded dynamically via AJAX
                NextDocumentNumber = NextDocumentNumber
            };

            return Page();
        }

        public async Task<IActionResult> OnPostUpdateStatusAsync(int documentId, string? status, string? returnUrl)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                var isAdmin = user != null && (await _userManager.IsInRoleAsync(user, "Admin") || await _userManager.IsInRoleAsync(user, "SuperAdmin"));
                var isEditor = user != null && await _userManager.IsInRoleAsync(user, "Editor");
                var currentUserName = User.Identity?.Name;

                if (!isAdmin && !isEditor)
                {
                    _logger.LogWarning("User {User} lacks permission to update document status for DocumentId: {DocumentId}", currentUserName ?? "Unknown", documentId);
                    TempData["ErrorMessage"] = "You do not have permission to update document status.";
                    return Redirect(returnUrl ?? "/DocManagement/Doc/Index");
                }

                if (!Enum.TryParse<DocumentStatusEnum>(status, true, out var newStatus) || !Enum.IsDefined(typeof(DocumentStatusEnum), newStatus))
                {
                    _logger.LogWarning("Invalid status value: {Status} for DocumentId: {DocumentId}", status, documentId);
                    TempData["ErrorMessage"] = "Invalid status value.";
                    return Redirect(returnUrl ?? "/DocManagement/Doc/Index");
                }

                var document = await _documentService.GetDocumentAsync(documentId);
                if (document == null)
                {
                    _logger.LogWarning("Document {DocumentId} not found or user lacks permission", documentId);
                    TempData["ErrorMessage"] = $"Document {documentId} not found.";
                    return Redirect(returnUrl ?? "/DocManagement/Doc/Index");
                }

                if (isEditor && document.UploadedBy != currentUserName)
                {
                    _logger.LogWarning("Editor {User} lacks permission to update document {DocumentId}", currentUserName ?? "Unknown", documentId);
                    TempData["ErrorMessage"] = "You do not have permission to update this document.";
                    return Redirect(returnUrl ?? "/DocManagement/Doc/Index");
                }

                var documentDto = new DocumentDto
                {
                    DocumentId = document.DocumentId,
                    FileName = document.FileName,
                    FilePath = document.FilePath,
                    DocumentTypeId = document.DocumentTypeId,
                    DocumentTypeName = document.DocumentTypeName,
                    UploadDate = document.UploadDate,
                    UploadedBy = document.UploadedBy,
                    SiteId = document.SiteId,
                    PartnerId = document.PartnerId,
                    PartnerName = document.PartnerName,
                    Status = newStatus,
                    DocumentLinks = document.DocumentLinks,
                    StatusHistory = document.StatusHistory
                };

                var updatedDoc = await _documentService.UpdateDocumentAsync(documentId, documentDto);
                if (updatedDoc == null)
                {
                    _logger.LogWarning("Failed to update Document {DocumentId}", documentId);
                    TempData["ErrorMessage"] = "Failed to update document status.";
                    return Redirect(returnUrl ?? "/DocManagement/Doc/Index");
                }

                TempData["SuccessMessage"] = $"Document status updated to {DocumentDto.StatusDisplayNames[newStatus.ToString()]}.";
                _logger.LogInformation("Updated status for Document {DocumentId} to {Status}", documentId, newStatus);
                return Redirect(returnUrl ?? "/DocManagement/Doc/Index");
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning("Unauthorized attempt to update document status for DocumentId: {DocumentId}: {Message}", documentId, ex.Message);
                TempData["ErrorMessage"] = "You do not have permission to update document status.";
                return Redirect(returnUrl ?? "/DocManagement/Doc/Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating status for Document {DocumentId}: {Message}", documentId, ex.Message);
                TempData["ErrorMessage"] = "An error occurred while updating the document status.";
                return Redirect(returnUrl ?? "/DocManagement/Doc/Index");
            }
        }

        public async Task<IActionResult> OnPostDeleteAsync(int documentId, string? returnUrl)
        {
            try
            {
                var deleted = await _documentService.DeleteDocumentAsync(documentId);
                if (!deleted)
                {
                    _logger.LogWarning("Document {DocumentId} not found or user lacks permission", documentId);
                    TempData["ErrorMessage"] = $"Document {documentId} not found or you lack permission.";
                    return Redirect(returnUrl ?? "/DocManagement/Doc/Index");
                }

                TempData["SuccessMessage"] = "Document deleted successfully.";
                _logger.LogInformation("Deleted Document {DocumentId}", documentId);
                return Redirect(returnUrl ?? "/DocManagement/Doc/Index");
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning("Unauthorized attempt to delete document {DocumentId}: {Message}", documentId, ex.Message);
                TempData["ErrorMessage"] = "You do not have permission to delete this document.";
                return Redirect(returnUrl ?? "/DocManagement/Doc/Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting Document {DocumentId}: {Message}, InnerException: {InnerException}", documentId, ex.Message, ex.InnerException?.Message);
                TempData["ErrorMessage"] = "Failed to delete the document. Please try again.";
                return Redirect(returnUrl ?? "/DocManagement/Doc/Index");
            }
        }
    }
}