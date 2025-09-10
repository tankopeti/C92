using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Cloud9_2.Models;
using Cloud9_2.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;

namespace Cloud9_2.Services
{
    public interface IDocumentService
    {
        Task<DocumentDto> GetDocumentAsync(int documentId);
        Task<List<DocumentDto>> GetDocumentsAsync(string searchTerm, int? documentTypeId, int? partnerId, int? siteId, DocumentStatusEnum? status, string sortBy, int skip, int take);
        Task<int> GetDocumentsCountAsync(string searchTerm, int? documentTypeId, int? partnerId, int? siteId);
        Task<DocumentDto> CreateDocumentAsync(CreateDocumentDto documentDto); // Updated to use CreateDocumentDto
        Task<DocumentDto> UpdateDocumentAsync(int documentId, DocumentDto documentUpdate);
        Task<bool> DeleteDocumentAsync(int documentId);
        Task<string> GetNextDocumentNumberAsync();
        Task<Document> GetDocumentByIdAsync(int documentId);
    }

    public class DocumentService : IDocumentService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<DocumentService> _logger;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly UserManager<ApplicationUser> _userManager;

        public DocumentService(
            ApplicationDbContext context,
            ILogger<DocumentService> logger,
            IHttpContextAccessor httpContextAccessor,
            UserManager<ApplicationUser> userManager)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
        }

        private string GetCurrentUser() =>
            _httpContextAccessor.HttpContext?.User?.Identity?.Name ?? "System";

        private async Task LogStatusChangeAsync(int documentId, DocumentStatusEnum oldStatus, DocumentStatusEnum newStatus)
        {
            if (oldStatus == newStatus)
                return;

            var history = new DocumentStatusHistory
            {
                DocumentId = documentId,
                OldStatus = oldStatus,
                NewStatus = newStatus,
                ChangeDate = DateTime.UtcNow,
                ChangedBy = GetCurrentUser()
            };

            _context.DocumentStatusHistory.Add(history);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Logged status change for document {DocumentId} from {OldStatus} to {NewStatus}", documentId, oldStatus, newStatus);
        }

        public async Task<DocumentDto> GetDocumentAsync(int documentId)
        {
            try
            {
                var user = await _userManager.GetUserAsync(_httpContextAccessor.HttpContext.User);
                var isAdmin = user != null && (await _userManager.IsInRoleAsync(user, "Admin") || await _userManager.IsInRoleAsync(user, "SuperAdmin"));

                var query = _context.Documents
                    .AsNoTracking()
                    .Include(d => d.DocumentType)
                    .Include(d => d.DocumentLinks)
                    .Include(d => d.StatusHistory)
                    .GroupJoin(_context.Partners,
                        d => d.PartnerId,
                        p => p.PartnerId,
                        (d, p) => new { Document = d, Partner = p })
                    .SelectMany(
                        dp => dp.Partner.DefaultIfEmpty(),
                        (d, p) => new DocumentDto
                        {
                            DocumentId = d.Document.DocumentId,
                            FileName = d.Document.FileName,
                            FilePath = d.Document.FilePath,
                            DocumentTypeId = d.Document.DocumentTypeId,
                            DocumentTypeName = d.Document.DocumentType != null ? d.Document.DocumentType.Name : null,
                            UploadDate = d.Document.UploadDate,
                            UploadedBy = d.Document.UploadedBy,
                            SiteId = d.Document.SiteId,
                            PartnerId = d.Document.PartnerId,
                            PartnerName = d.Document.PartnerId.HasValue ? p.Name ?? "Unknown" : "N/A",
                            Status = d.Document.Status,
                            DocumentLinks = d.Document.DocumentLinks.Select(l => new DocumentLinkDto
                            {
                                Id = l.ID,
                                DocumentId = l.DocumentId,
                                ModuleId = l.ModuleID,
                                RecordId = l.RecordID
                            }).ToList(),
                            StatusHistory = d.Document.StatusHistory.Select(h => new DocumentStatusHistoryDto
                            {
                                Id = h.Id,
                                DocumentId = h.DocumentId,
                                OldStatus = h.OldStatus,
                                NewStatus = h.NewStatus,
                                ChangeDate = h.ChangeDate,
                                ChangedBy = h.ChangedBy
                            }).ToList()
                        });

                if (!isAdmin)
                {
                    query = query.Where(d => d.UploadedBy == GetCurrentUser());
                }

                var doc = await query.FirstOrDefaultAsync(d => d.DocumentId == documentId);
                if (doc == null)
                {
                    _logger.LogWarning("Document {DocumentId} not found or user lacks permission", documentId);
                    return null;
                }

                return doc;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching document {DocumentId}: {Message}, StackTrace: {StackTrace}", documentId, ex.Message, ex.StackTrace);
                throw;
            }
        }

        public static DocumentDto MapToDto(Document document, ApplicationDbContext context)
        {
            return new DocumentDto
            {
                DocumentId = document.DocumentId,
                FileName = document.FileName,
                FilePath = document.FilePath,
                DocumentTypeId = document.DocumentTypeId,
                DocumentTypeName = document.DocumentType?.Name,
                UploadDate = document.UploadDate,
                UploadedBy = document.UploadedBy,
                SiteId = document.SiteId,
                PartnerId = document.PartnerId,
                PartnerName = document.PartnerId.HasValue ? context.Partners
                    .Where(p => p.PartnerId == document.PartnerId)
                    .Select(p => p.Name)
                    .FirstOrDefault() ?? "Unknown" : "N/A",
                Status = document.Status,
                DocumentLinks = document.DocumentLinks?.Select(l => new DocumentLinkDto
                {
                    Id = l.ID,
                    DocumentId = l.DocumentId,
                    ModuleId = l.ModuleID,
                    RecordId = l.RecordID
                }).ToList() ?? new List<DocumentLinkDto>(),
                StatusHistory = document.StatusHistory?.Select(h => new DocumentStatusHistoryDto
                {
                    Id = h.Id,
                    DocumentId = h.DocumentId,
                    OldStatus = h.OldStatus,
                    NewStatus = h.NewStatus,
                    ChangeDate = h.ChangeDate,
                    ChangedBy = h.ChangedBy
                }).ToList() ?? new List<DocumentStatusHistoryDto>()
            };
        }

        public async Task<Document> GetDocumentByIdAsync(int documentId)
        {
            var user = await _userManager.GetUserAsync(_httpContextAccessor.HttpContext.User);
            var isAdmin = user != null && (await _userManager.IsInRoleAsync(user, "Admin") || await _userManager.IsInRoleAsync(user, "SuperAdmin"));

            var query = _context.Documents
                .Include(d => d.DocumentType)
                .AsQueryable();

            if (!isAdmin)
            {
                query = query.Where(d => d.UploadedBy == GetCurrentUser());
            }

            var document = await query.FirstOrDefaultAsync(d => d.DocumentId == documentId);
            if (document == null)
            {
                _logger.LogWarning("Document not found or user lacks permission for DocumentId: {DocumentId}", documentId);
                throw new ArgumentException($"Érvénytelen DocumentId: {documentId}");
            }
            return document;
        }

        public async Task<List<DocumentDto>> GetDocumentsAsync(string searchTerm, int? documentTypeId, int? partnerId, int? siteId, DocumentStatusEnum? status, string sortBy, int skip, int take)
        {
            try
            {
                var user = await _userManager.GetUserAsync(_httpContextAccessor.HttpContext.User);
                var isAdmin = user != null && (await _userManager.IsInRoleAsync(user, "Admin") || await _userManager.IsInRoleAsync(user, "SuperAdmin"));

                var query = _context.Documents
                    .AsNoTracking()
                    .Include(d => d.DocumentType)
                    .Include(d => d.DocumentLinks)
                    .Include(d => d.StatusHistory)
                    .GroupJoin(_context.Partners,
                        d => d.PartnerId,
                        p => p.PartnerId,
                        (d, p) => new { Document = d, Partner = p })
                    .SelectMany(
                        dp => dp.Partner.DefaultIfEmpty(),
                        (d, p) => new DocumentDto
                        {
                            DocumentId = d.Document.DocumentId,
                            FileName = d.Document.FileName,
                            FilePath = d.Document.FilePath,
                            DocumentTypeId = d.Document.DocumentTypeId,
                            DocumentTypeName = d.Document.DocumentType != null ? d.Document.DocumentType.Name : null,
                            UploadDate = d.Document.UploadDate,
                            UploadedBy = d.Document.UploadedBy,
                            SiteId = d.Document.SiteId,
                            PartnerId = d.Document.PartnerId,
                            PartnerName = d.Document.PartnerId.HasValue ? p.Name ?? "Unknown" : "N/A",
                            Status = d.Document.Status,
                            DocumentLinks = d.Document.DocumentLinks.Select(l => new DocumentLinkDto
                            {
                                Id = l.ID,
                                DocumentId = l.DocumentId,
                                ModuleId = l.ModuleID,
                                RecordId = l.RecordID
                            }).ToList(),
                            StatusHistory = d.Document.StatusHistory.Select(h => new DocumentStatusHistoryDto
                            {
                                Id = h.Id,
                                DocumentId = h.DocumentId,
                                OldStatus = h.OldStatus,
                                NewStatus = h.NewStatus,
                                ChangeDate = h.ChangeDate,
                                ChangedBy = h.ChangedBy
                            }).ToList()
                        });

                if (!isAdmin)
                {
                    query = query.Where(d => d.UploadedBy == GetCurrentUser());
                }

                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    query = query.Where(d =>
                        d.FileName.Contains(searchTerm) ||
                        d.UploadedBy.Contains(searchTerm) ||
                        d.DocumentLinks.Any(l => l.ModuleId.Contains(searchTerm)));
                }

                if (documentTypeId.HasValue)
                    query = query.Where(d => d.DocumentTypeId == documentTypeId);

                if (partnerId.HasValue)
                    query = query.Where(d => d.PartnerId == partnerId);

                if (siteId.HasValue)
                    query = query.Where(d => d.SiteId == siteId);

                if (status.HasValue)
                    query = query.Where(d => d.Status == status.Value);

                sortBy = sortBy?.ToLower() ?? "uploaddate";
                query = sortBy switch
                {
                    "filename" => query.OrderBy(d => d.FileName),
                    "documentid" => query.OrderByDescending(d => d.DocumentId),
                    "status" => query.OrderBy(d => d.Status),
                    _ => query.OrderByDescending(d => d.UploadDate)
                };

                var docs = await query
                    .Skip(skip)
                    .Take(take)
                    .ToListAsync();

                _logger.LogInformation("Fetched {DocumentCount} documents", docs.Count);
                return docs;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching documents: {Message}, StackTrace: {StackTrace}", ex.Message, ex.StackTrace);
                throw;
            }
        }

        public async Task<int> GetDocumentsCountAsync(string searchTerm, int? documentTypeId, int? partnerId, int? siteId)
        {
            try
            {
                var user = await _userManager.GetUserAsync(_httpContextAccessor.HttpContext.User);
                var isAdmin = user != null && (await _userManager.IsInRoleAsync(user, "Admin") || await _userManager.IsInRoleAsync(user, "SuperAdmin"));

                var query = _context.Documents.AsQueryable();

                if (!isAdmin)
                {
                    query = query.Where(d => d.UploadedBy == GetCurrentUser());
                }

                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    query = query.Where(d =>
                        d.FileName.Contains(searchTerm) ||
                        d.UploadedBy.Contains(searchTerm));
                }

                if (documentTypeId.HasValue)
                    query = query.Where(d => d.DocumentTypeId == documentTypeId);

                if (partnerId.HasValue)
                    query = query.Where(d => d.PartnerId == partnerId);

                if (siteId.HasValue)
                    query = query.Where(d => d.SiteId == siteId);

                var count = await query.CountAsync();
                _logger.LogInformation("Counted {Count} documents", count);
                return count;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error counting documents: {Message}, StackTrace: {StackTrace}", ex.Message, ex.StackTrace);
                throw;
            }
        }

        public async Task<DocumentDto> CreateDocumentAsync(CreateDocumentDto documentDto)
        {
            if (documentDto == null) throw new ArgumentNullException(nameof(documentDto));

            var user = await _userManager.GetUserAsync(_httpContextAccessor.HttpContext.User);
            var isAdmin = user != null && (await _userManager.IsInRoleAsync(user, "Admin") || await _userManager.IsInRoleAsync(user, "SuperAdmin"));
            var isEditor = user != null && await _userManager.IsInRoleAsync(user, "Editor");

            if (!isAdmin && !isEditor)
            {
                _logger.LogWarning("User {User} lacks permission to create documents", GetCurrentUser());
                throw new UnauthorizedAccessException("User lacks permission to create documents.");
            }

            var doc = new Document
            {
                FileName = documentDto.FileName,
                FilePath = documentDto.FilePath,
                DocumentTypeId = documentDto.DocumentTypeId,
                UploadDate = DateTime.UtcNow,
                UploadedBy = GetCurrentUser(),
                PartnerId = documentDto.PartnerId,
                SiteId = documentDto.SiteId,
                Status = documentDto.Status
            };

            try
            {
                _context.Documents.Add(doc);
                await _context.SaveChangesAsync();

                // Log initial status
                await LogStatusChangeAsync(doc.DocumentId, doc.Status, doc.Status);

                // Fetch the document with related data to create DocumentDto
                var result = await _context.Documents
                    .AsNoTracking()
                    .Include(d => d.DocumentType)
                    .Include(d => d.DocumentLinks)
                    .Include(d => d.StatusHistory) // Added to ensure StatusHistory is included
                    .GroupJoin(_context.Partners,
                        d => d.PartnerId,
                        p => p.PartnerId,
                        (d, p) => new { Document = d, Partner = p })
                    .SelectMany(
                        dp => dp.Partner.DefaultIfEmpty(),
                        (d, p) => new DocumentDto
                        {
                            DocumentId = d.Document.DocumentId,
                            FileName = d.Document.FileName,
                            FilePath = d.Document.FilePath,
                            DocumentTypeId = d.Document.DocumentTypeId,
                            DocumentTypeName = d.Document.DocumentType != null ? d.Document.DocumentType.Name : null,
                            UploadDate = d.Document.UploadDate,
                            UploadedBy = d.Document.UploadedBy,
                            SiteId = d.Document.SiteId,
                            PartnerId = d.Document.PartnerId,
                            PartnerName = d.Document.PartnerId.HasValue ? p.Name ?? "Unknown" : "N/A",
                            Status = d.Document.Status,
                            DocumentLinks = d.Document.DocumentLinks.Select(l => new DocumentLinkDto
                            {
                                Id = l.ID,
                                DocumentId = l.DocumentId,
                                ModuleId = l.ModuleID,
                                RecordId = l.RecordID
                            }).ToList(),
                            StatusHistory = d.Document.StatusHistory.Select(h => new DocumentStatusHistoryDto
                            {
                                Id = h.Id,
                                DocumentId = h.DocumentId,
                                OldStatus = h.OldStatus,
                                NewStatus = h.NewStatus,
                                ChangeDate = h.ChangeDate,
                                ChangedBy = h.ChangedBy
                            }).ToList()
                        })
                    .FirstOrDefaultAsync(d => d.DocumentId == doc.DocumentId);

                return result ?? throw new Exception("Failed to retrieve created document");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating document {FileName}: {Message}, StackTrace: {StackTrace}", documentDto.FileName, ex.Message, ex.StackTrace);
                throw;
            }
        }

        public async Task<DocumentDto> UpdateDocumentAsync(int documentId, DocumentDto documentUpdate)
        {
            if (documentUpdate == null) throw new ArgumentNullException(nameof(documentUpdate));

            var user = await _userManager.GetUserAsync(_httpContextAccessor.HttpContext.User);
            var isAdmin = user != null && (await _userManager.IsInRoleAsync(user, "Admin") || await _userManager.IsInRoleAsync(user, "SuperAdmin"));
            var isEditor = user != null && await _userManager.IsInRoleAsync(user, "Editor");

            var doc = await _context.Documents
                .Include(d => d.DocumentType)
                .FirstOrDefaultAsync(d => d.DocumentId == documentId);
            if (doc == null)
            {
                _logger.LogWarning("Document {DocumentId} not found", documentId);
                return null;
            }

            if (!isAdmin && !isEditor)
            {
                _logger.LogWarning("User {User} lacks permission to update document {DocumentId}", GetCurrentUser(), documentId);
                throw new UnauthorizedAccessException("User lacks permission to update documents.");
            }

            if (isEditor && doc.UploadedBy != GetCurrentUser())
            {
                _logger.LogWarning("Editor {User} lacks permission to update document {DocumentId}", GetCurrentUser(), documentId);
                throw new UnauthorizedAccessException("Editor lacks permission to update this document.");
            }

            try
            {
                if (doc.Status != documentUpdate.Status)
                {
                    await LogStatusChangeAsync(documentId, doc.Status, documentUpdate.Status);
                }

                doc.FileName = documentUpdate.FileName ?? doc.FileName;
                doc.FilePath = documentUpdate.FilePath ?? doc.FilePath;
                doc.DocumentTypeId = documentUpdate.DocumentTypeId ?? doc.DocumentTypeId;
                doc.PartnerId = documentUpdate.PartnerId ?? doc.PartnerId;
                doc.SiteId = documentUpdate.SiteId ?? doc.SiteId;
                doc.Status = documentUpdate.Status;

                await _context.SaveChangesAsync();

                var result = await _context.Documents
                    .AsNoTracking()
                    .Include(d => d.DocumentType)
                    .Include(d => d.DocumentLinks)
                    .Include(d => d.StatusHistory) // Added to ensure StatusHistory is included
                    .GroupJoin(_context.Partners,
                        d => d.PartnerId,
                        p => p.PartnerId,
                        (d, p) => new { Document = d, Partner = p })
                    .SelectMany(
                        dp => dp.Partner.DefaultIfEmpty(),
                        (d, p) => new DocumentDto
                        {
                            DocumentId = d.Document.DocumentId,
                            FileName = d.Document.FileName,
                            FilePath = d.Document.FilePath,
                            DocumentTypeId = d.Document.DocumentTypeId,
                            DocumentTypeName = d.Document.DocumentType != null ? d.Document.DocumentType.Name : null,
                            UploadDate = d.Document.UploadDate,
                            UploadedBy = d.Document.UploadedBy,
                            SiteId = d.Document.SiteId,
                            PartnerId = d.Document.PartnerId,
                            PartnerName = d.Document.PartnerId.HasValue ? p.Name ?? "Unknown" : "N/A",
                            Status = d.Document.Status,
                            DocumentLinks = d.Document.DocumentLinks.Select(l => new DocumentLinkDto
                            {
                                Id = l.ID,
                                DocumentId = l.DocumentId,
                                ModuleId = l.ModuleID,
                                RecordId = l.RecordID
                            }).ToList(),
                            StatusHistory = d.Document.StatusHistory.Select(h => new DocumentStatusHistoryDto
                            {
                                Id = h.Id,
                                DocumentId = h.DocumentId,
                                OldStatus = h.OldStatus,
                                NewStatus = h.NewStatus,
                                ChangeDate = h.ChangeDate,
                                ChangedBy = h.ChangedBy
                            }).ToList()
                        })
                    .FirstOrDefaultAsync(d => d.DocumentId == documentId);

                return result ?? throw new Exception("Failed to retrieve updated document");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating document {DocumentId}: {Message}, StackTrace: {StackTrace}", documentId, ex.Message, ex.StackTrace);
                throw;
            }
        }

        public async Task<bool> DeleteDocumentAsync(int documentId)
        {
            var user = await _userManager.GetUserAsync(_httpContextAccessor.HttpContext.User);
            var isAdmin = user != null && (await _userManager.IsInRoleAsync(user, "Admin") || await _userManager.IsInRoleAsync(user, "SuperAdmin"));

            var doc = await _context.Documents
                .Include(d => d.DocumentMetadata)
                .Include(d => d.DocumentLinks)
                .Include(d => d.StatusHistory)
                .FirstOrDefaultAsync(d => d.DocumentId == documentId);

            if (doc == null)
            {
                _logger.LogWarning("Document {DocumentId} not found", documentId);
                return false;
            }

            if (!isAdmin)
            {
                _logger.LogWarning("User {User} lacks permission to delete document {DocumentId}", GetCurrentUser(), documentId);
                return false;
            }

            try
            {
                if (doc.DocumentMetadata?.Any() == true)
                    _context.DocumentMetadata.RemoveRange(doc.DocumentMetadata);
                if (doc.DocumentLinks?.Any() == true)
                    _context.DocumentLinks.RemoveRange(doc.DocumentLinks);
                if (doc.StatusHistory?.Any() == true)
                    _context.DocumentStatusHistory.RemoveRange(doc.StatusHistory);

                _context.Documents.Remove(doc);
                var rowsAffected = await _context.SaveChangesAsync();
                _logger.LogInformation("Deleted document {DocumentId}, rows affected: {RowsAffected}", documentId, rowsAffected);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting document {DocumentId}: {Message}, InnerException: {InnerException}", documentId, ex.Message, ex.InnerException?.Message);
                throw;
            }
        }

        public async Task<string> GetNextDocumentNumberAsync()
        {
            var yearDay = DateTime.UtcNow.DayOfYear;
            var randomNum = new Random().Next(100, 1000);
            var count = await _context.Documents.CountAsync();
            return $"TestDocument-{yearDay}-{count + 1:D4}-{randomNum}";
        }
    }
}