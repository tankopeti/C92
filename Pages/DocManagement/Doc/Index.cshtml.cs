using Cloud9_2.Data;
using Cloud9_2.Models;
using Cloud9_2.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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

        public IList<DocumentDto> Documents { get; set; }
        public Document SelectedDocument { get; set; }
        public string SearchTerm { get; set; }
        public string StatusFilter { get; set; }
        public string SortBy { get; set; }
        public int CurrentPage { get; set; }
        public int PageSize { get; set; }
        public int TotalRecords { get; set; }
        public int TotalPages { get; set; }
        public int DistinctDocumentIdCount { get; set; }
        public string NextDocumentNumber { get; set; }
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

        public async Task OnGetAsync(int? pageNumber, string? searchTerm, int? pageSize, string? statusFilter, string? sortBy, int? documentId)
        {
            try
            {
                SearchTerm = searchTerm;
                StatusFilter = statusFilter;
                SortBy = sortBy;
                CurrentPage = pageNumber ?? 1;
                PageSize = pageSize ?? 10;

                var user = await _userManager.GetUserAsync(User);
                var isAdmin = user != null && (await _userManager.IsInRoleAsync(user, "Admin") || await _userManager.IsInRoleAsync(user, "SuperAdmin"));
                var currentUserName = User.Identity?.Name; // Add null check

                DocumentStatusEnum? status = null;
                if (!string.IsNullOrEmpty(statusFilter) && statusFilter != "all" && Enum.TryParse<DocumentStatusEnum>(statusFilter, true, out var parsedStatus))
                {
                    status = parsedStatus;
                }

                Documents = await _documentService.GetDocumentsAsync(
                    searchTerm,
                    documentTypeId: null,
                    partnerId: null,
                    siteId: null,
                    status,
                    sortBy,
                    skip: (CurrentPage - 1) * PageSize,
                    take: PageSize);

                TotalRecords = await _documentService.GetDocumentsCountAsync(
                    searchTerm,
                    documentTypeId: null,
                    partnerId: null,
                    siteId: null);

                TotalPages = (int)Math.Ceiling((double)TotalRecords / PageSize);
                DistinctDocumentIdCount = await _context.Documents
                    .AsNoTracking()
                    .Select(d => d.DocumentId)
                    .Distinct()
                    .CountAsync();

                if (documentId.HasValue && currentUserName != null) // Add null check
                {
                    SelectedDocument = await _context.Documents
                        .Include(d => d.DocumentType)
                        .Include(d => d.DocumentLinks)
                        .Include(d => d.StatusHistory)
                        .AsQueryable()
                        .Where(d => isAdmin || d.UploadedBy == currentUserName)
                        .FirstOrDefaultAsync(d => d.DocumentId == documentId);

                    if (SelectedDocument != null)
                    {
                        _logger.LogInformation("Retrieved Document {DocumentId} with {LinkCount} links and {HistoryCount} history records", documentId, SelectedDocument.DocumentLinks?.Count ?? 0, SelectedDocument.StatusHistory?.Count ?? 0);
                    }
                }

                NextDocumentNumber = await _documentService.GetNextDocumentNumberAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in OnGetAsync: {Message}", ex.Message);
                throw;
            }
        }

        public async Task<IActionResult> OnPostUpdateStatusAsync(int documentId, string? status, string? returnUrl)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                var isAdmin = user != null && (await _userManager.IsInRoleAsync(user, "Admin") || await _userManager.IsInRoleAsync(user, "SuperAdmin"));
                var isEditor = user != null && await _userManager.IsInRoleAsync(user, "Editor");
                var currentUserName = User.Identity?.Name; // Add null check

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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating status for Document {DocumentId}: {Message}", documentId, ex.Message);
                TempData["ErrorMessage"] = "An error occurred while updating the document status.";
                return Redirect(returnUrl ?? "/DocManagement/Doc/Index");
            }
        }

        public async Task<IActionResult> OnPostDeleteAsync(int documentId, string returnUrl)
        {
            try
            {
                var deleted = await _documentService.DeleteDocumentAsync(documentId);
                if (!deleted)
                {
                    _logger.LogWarning("Document {DocumentId} not found or user lacks permission", documentId);
                    TempData["ErrorMessage"] = $"Document {documentId} not found or you lack permission.";
                    return Redirect(returnUrl);
                }

                TempData["SuccessMessage"] = "Document deleted successfully.";
                _logger.LogInformation("Deleted Document {DocumentId}", documentId);
                return Redirect(returnUrl);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting Document {DocumentId}: {Message}, InnerException: {InnerException}", documentId, ex.Message, ex.InnerException?.Message);
                TempData["ErrorMessage"] = "Failed to delete the document. Please try again.";
                return Redirect(returnUrl);
            }
        }
    }
}