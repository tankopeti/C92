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
        public IDictionary<string, string> StatusDisplayNames { get; set; } = new Dictionary<string, string>
        {
            { DocumentStatusEnum.Beérkezett.ToString(), "Beérkezett" },
            { DocumentStatusEnum.Függőben.ToString(), "Függőben" },
            { DocumentStatusEnum.Elfogadott.ToString(), "Elfogadott" },
            { DocumentStatusEnum.Lezárt.ToString(), "Lezárt" },
            { DocumentStatusEnum.Jóváhagyandó.ToString(), "Jóváhagyandó" }
        };

        public async Task OnGetAsync(int? pageNumber, string searchTerm, int? pageSize, string statusFilter, string sortBy, int? documentId)
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

                DocumentStatusEnum? status = null;
                if (!string.IsNullOrEmpty(statusFilter) && statusFilter != "all" && Enum.TryParse<DocumentStatusEnum>(statusFilter, true, out var parsedStatus))
                {
                    status = parsedStatus;
                }

                // Use DocumentService to fetch documents
                Documents = await _documentService.GetDocumentsAsync(
                    searchTerm,
                    documentTypeId: null,
                    partnerId: null,
                    siteId: null,
                    status,
                    sortBy,
                    skip: (CurrentPage - 1) * PageSize,
                    take: PageSize);

                                    // Populate StatusDisplayNames in each DocumentDto for modal
                foreach (var doc in Documents)
                {
                    doc.StatusDisplayNames = StatusDisplayNames;
                }

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

                if (documentId.HasValue)
                {
                    SelectedDocument = await _context.Documents
                        .Include(d => d.DocumentType)
                        .Include(d => d.DocumentLinks)
                        .AsQueryable()
                        .Where(d => isAdmin || d.UploadedBy == User.Identity.Name)
                        .FirstOrDefaultAsync(d => d.DocumentId == documentId);

                    if (SelectedDocument != null)
                    {
                        _logger.LogInformation("Retrieved Document {DocumentId} with {LinkCount} links", documentId, SelectedDocument.DocumentLinks?.Count ?? 0);
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

                public async Task<IActionResult> OnPostUpdateStatusAsync(int documentId, string status, string returnUrl)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                var isAdmin = user != null && (await _userManager.IsInRoleAsync(user, "Admin") || await _userManager.IsInRoleAsync(user, "SuperAdmin"));

                var document = await _context.Documents
                    .FirstOrDefaultAsync(d => d.DocumentId == documentId && (isAdmin || d.UploadedBy == User.Identity.Name));

                if (document == null)
                {
                    return NotFound();
                }

                if (Enum.TryParse<DocumentStatusEnum>(status, true, out var newStatus))
                {
                    document.Status = newStatus;
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Updated status for Document {DocumentId} to {Status}", documentId, newStatus);
                }

                return Redirect(returnUrl);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating status for Document {DocumentId}: {Message}", documentId, ex.Message);
                return Redirect(returnUrl);
            }
        }

        public async Task<IActionResult> OnPostDeleteAsync(int documentId, string returnUrl)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                var isAdmin = user != null && (await _userManager.IsInRoleAsync(user, "Admin") || await _userManager.IsInRoleAsync(user, "SuperAdmin"));

                var document = await _context.Documents
                    .FirstOrDefaultAsync(d => d.DocumentId == documentId && (isAdmin || d.UploadedBy == User.Identity.Name));

                if (document == null)
                {
                    return NotFound();
                }

                _context.Documents.Remove(document);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Deleted Document {DocumentId}", documentId);

                return Redirect(returnUrl);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting Document {DocumentId}: {Message}", documentId, ex.Message);
                return Redirect(returnUrl);
            }
        }

    }
}