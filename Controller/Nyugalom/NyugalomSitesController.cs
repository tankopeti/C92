using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Cloud9_2.Data;
using Cloud9_2.Models;

namespace Cloud9_2.Controllers.Api
{
    [Route("api/documents")]
    [ApiController]
    [Authorize]
    public class DocumentsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<DocumentsController> _logger;

        public DocumentsController(ApplicationDbContext context, ILogger<DocumentsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet("all/select")]
        public async Task<IActionResult> GetAllDocumentsForSelect(
            [FromQuery] string search = "",
            [FromQuery] int take = 300)
        {
            try
            {
                var query = _context.Documents
                    .AsNoTracking()
                    .Include(d => d.DocumentType)
                    .AsQueryable();

                if (!string.IsNullOrWhiteSpace(search))
                {
                    var term = search.Trim().ToLower();

                    query = query.Where(d =>
                        EF.Functions.Like(d.FileName.ToLower(), $"%{term}%") ||
                        (d.DocumentType != null && EF.Functions.Like(d.DocumentType.Name.ToLower(), $"%{term}%")) ||
                        EF.Functions.Like(d.UploadedBy.ToLower(), $"%{term}%")
                    );
                }

                var documents = await query
                    .OrderByDescending(d => d.UploadDate)
                    .ThenBy(d => d.FileName)
                    .Take(Math.Max(1, Math.Min(take, 500)))
                    .Select(d => new
                    {
                        id = d.DocumentId,
                        // EF Core-barát: ternary operator null ellenőrzéssel
                        text = d.FileName + " (" + (d.DocumentType != null ? d.DocumentType.Name : "Ismeretlen típus") + ")",

                        fileName = d.FileName,
                        filePath = d.FilePath,
                        // Ugyanaz a minta mindenhol
                        documentType = d.DocumentType != null ? d.DocumentType.Name : "Nincs típus",
                        uploadDate = d.UploadDate.HasValue 
                            ? d.UploadDate.Value.ToString("yyyy. MM. dd. HH:mm") 
                            : "Ismeretlen",
                        uploadedBy = d.UploadedBy ?? "Ismeretlen",
                        status = d.Status.ToString(),
                        partnerId = d.PartnerId
                    })
                    .ToListAsync();

                // Ha szeretnéd, itt client-oldalon is biztonságosan használhatsz ?., mert már nem EF query
                var result = documents.Select(d => new
                {
                    id = d.id,
                    text = d.text,
                    fileName = d.fileName,
                    filePath = d.filePath,
                    documentType = d.documentType,
                    uploadDate = d.uploadDate,
                    uploadedBy = d.uploadedBy,
                    status = d.status,
                    partnerId = d.partnerId
                });

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Hiba a dokumentumok betöltésekor (autocomplete API)");
                return StatusCode(500, new { message = "Szerveroldali hiba történt a dokumentumok betöltése közben." });
            }
        }
    }
}