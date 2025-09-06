using Cloud9_2.Data;
using Cloud9_2.Models;
using Cloud9_2.Services;
using Cloud9_2.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Cloud9_2.Pages.DocManagement.Doc
{
    [Authorize]
    public class ViewModel : Microsoft.AspNetCore.Mvc.RazorPages.PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<ViewModel> _logger;

        public ViewModel(ApplicationDbContext context, UserManager<ApplicationUser> userManager, ILogger<ViewModel> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        public DocumentDto Document { get; set; }
        public Dictionary<string, string> StatusDisplayNames => new Dictionary<string, string>
        {
            { DocumentStatusEnum.Beérkezett.ToString(), "Beérkezett" },
            { DocumentStatusEnum.Függőben.ToString(), "Függőben" },
            { DocumentStatusEnum.Elfogadott.ToString(), "Elfogadott" },
            { DocumentStatusEnum.Lezárt.ToString(), "Lezárt" },
            { DocumentStatusEnum.Jóváhagyandó.ToString(), "Jóváhagyandó" }
        };

        public async Task<IActionResult> OnGetAsync(int id)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                var isAdmin = user != null && (await _userManager.IsInRoleAsync(user, "Admin") || await _userManager.IsInRoleAsync(user, "SuperAdmin"));

                Document = await _context.Documents
                    .AsNoTracking()
                    .Include(d => d.DocumentType)
                    .Include(d => d.DocumentLinks)
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
                            }).ToList()
                        })
                    .Where(d => d.DocumentId == id && (isAdmin || d.UploadedBy == User.Identity.Name))
                    .FirstOrDefaultAsync();

                if (Document == null)
                {
                    _logger.LogWarning("Document {DocumentId} not found or access denied", id);
                    return NotFound();
                }

                _logger.LogInformation("Retrieved Document {DocumentId}", id);
                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving Document {DocumentId}: {Message}", id, ex.Message);
                return NotFound();
            }
        }
    }
}