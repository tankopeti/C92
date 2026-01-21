using Cloud9_2.Data;
using Cloud9_2.Models;
using Cloud9_2.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Cloud9_2.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class DocumentsController : ControllerBase
    {
        private readonly DocumentService _documentService;
        private readonly ILogger<DocumentsController> _logger;
        private readonly ApplicationDbContext _context;

        public DocumentsController(
            DocumentService documentService,
            ILogger<DocumentsController> logger,
            ApplicationDbContext context)
        {
            _documentService = documentService ?? throw new ArgumentNullException(nameof(documentService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        // GET api/documents/select?search=abc
// GET api/documents/select?search=abc
[HttpGet("select")]
[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
public async Task<IActionResult> GetDocumentsForSelect([FromQuery] string search = "")
{
    try
    {
        var docs = await _documentService.GetDocumentsAsync(
            searchTerm: search,
            documentTypeId: null,
            partnerId: null,
            siteId: null,
            status: null,
            dateFrom: null,
            dateTo: null,
            sortBy: "uploaddate",
            sortDir: "desc",
            skip: 0,
            take: 50);

        var result = docs.Select(d => new
        {
            id = d.DocumentId,
            text = d.FileName + (d.DocumentTypeName != null ? $" ({d.DocumentTypeName})" : "")
        }).ToList();

        _logger.LogInformation("Fetched {DocumentCount} documents for select", result.Count);
        return Ok(result);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error fetching documents for select");
        return StatusCode(500, new { error = "Failed to retrieve documents for select" });
    }
}

// GET api/documents?search=&documentTypeId=&partnerId=&siteId=&status=&dateFrom=&dateTo=&sortBy=&sortDir=&skip=&take=
// GET api/documents?search=&documentTypeId=&partnerId=&siteId=&status=&dateFrom=&dateTo=&sortBy=&sortDir=&skip=&take=
[HttpGet]
public async Task<IActionResult> GetDocuments(
    [FromQuery] string search = "",
    [FromQuery] int? documentTypeId = null,
    [FromQuery] int? partnerId = null,
    [FromQuery] int? siteId = null,
    [FromQuery] string? status = null,
    [FromQuery] DateTime? dateFrom = null,
    [FromQuery] DateTime? dateTo = null,
    [FromQuery] string sortBy = "uploaddate",
    [FromQuery] string sortDir = "desc",
    [FromQuery] int skip = 0,
    [FromQuery] int take = 50)
{
    try
    {
        take = Math.Clamp(take, 1, 200);
        if (skip < 0) skip = 0;

        DocumentStatusEnum? statusEnum = null;
        if (!string.IsNullOrWhiteSpace(status) &&
            status != "all" &&
            Enum.TryParse(status, true, out DocumentStatusEnum parsed))
        {
            statusEnum = parsed;
        }

        sortDir = string.Equals(sortDir, "asc", StringComparison.OrdinalIgnoreCase) ? "asc" : "desc";

        var docs = await _documentService.GetDocumentsAsync(
            searchTerm: search,
            documentTypeId: documentTypeId,
            partnerId: partnerId,
            siteId: siteId,
            status: statusEnum,
            dateFrom: dateFrom,
            dateTo: dateTo,
            sortBy: sortBy,
            sortDir: sortDir,
            skip: skip,
            take: take);

        return Ok(docs);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error fetching documents");
        return StatusCode(500, new { error = "Failed to retrieve documents" });
    }
}

// GET api/documents/list?...
[HttpGet("list")]
public async Task<IActionResult> GetDocumentsList(
    [FromQuery] string? search = "",
    [FromQuery] int? documentTypeId = null,
    [FromQuery] int? partnerId = null,
    [FromQuery] int? siteId = null,
    [FromQuery] string? status = null,
    [FromQuery] DateTime? dateFrom = null,
    [FromQuery] DateTime? dateTo = null,
    [FromQuery] bool includeInactive = false,
    [FromQuery] string sortBy = "uploaddate",
    [FromQuery] string sortDir = "desc",
    [FromQuery] int skip = 0,
    [FromQuery] int take = 50)
{
    try
    {
        DocumentStatusEnum? statusEnum = null;
        if (!string.IsNullOrWhiteSpace(status) &&
            status != "all" &&
            Enum.TryParse<DocumentStatusEnum>(status, true, out var parsed))
        {
            statusEnum = parsed;
        }

        var items = await _documentService.GetDocumentsListAsync(
            search, documentTypeId, partnerId, siteId,
            statusEnum, dateFrom, dateTo,
            includeInactive,
            sortBy, sortDir, skip, take);

        return Ok(items);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error fetching document list");
        return StatusCode(500, new { error = "Failed to retrieve documents" });
    }
}


        // GET api/documents/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<DocumentDto>> GetDocument(int id)
        {
            try
            {
                var doc = await _documentService.GetDocumentAsync(id);
                if (doc == null)
                {
                    _logger.LogWarning("Document {DocumentId} not found", id);
                    return NotFound(new { error = $"Document {id} not found" });
                }

                return Ok(doc);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching document {DocumentId}", id);
                return StatusCode(500, new { error = "Failed to retrieve document" });
            }
        }

// POST api/documents  (multipart/form-data)
[HttpPost]
[RequestSizeLimit(200_000_000)] // opcionális: 200MB
public async Task<IActionResult> CreateDocument([FromForm] IFormFile file, [FromForm] string payloadJson)
{
    _logger.LogWarning(">>> CreateDocument HIT <<<");

    var opId = Guid.NewGuid().ToString("N");

    _logger.LogInformation("[{OpId}] CreateDocument START", opId);

    // --- Request basics ---
    try
    {
        _logger.LogInformation(
            "[{OpId}] Request: Method={Method}, Path={Path}, ContentType={ContentType}, ContentLength={ContentLength}",
            opId,
            Request?.Method,
            Request?.Path.Value,
            Request?.ContentType,
            Request?.ContentLength
        );
    }
    catch { /* ignore request logging failure */ }

    // --- File basics ---
    _logger.LogInformation(
        "[{OpId}] File: isNull={IsNull}, FileName={FileName}, Length={Length}, ContentType={FileContentType}",
        opId,
        file == null,
        file?.FileName,
        file?.Length,
        file?.ContentType
    );

    // --- PayloadJson basics ---
    _logger.LogInformation(
        "[{OpId}] payloadJson: isNullOrWhiteSpace={IsEmpty}, length={Len}",
        opId,
        string.IsNullOrWhiteSpace(payloadJson),
        payloadJson?.Length ?? 0
    );

    // (Választható) teljes payloadJson log — ha túl nagy, csonkoljuk
    _logger.LogInformation("[{OpId}] payloadJson (truncated): {PayloadJson}",
        opId,
        Trunc(payloadJson, 8000)
    );

    if (file == null || file.Length == 0)
    {
        _logger.LogWarning("[{OpId}] BadRequest: File is required (null or empty).", opId);
        return BadRequest(new { message = "File is required" });
    }

    if (string.IsNullOrWhiteSpace(payloadJson))
    {
        _logger.LogWarning("[{OpId}] BadRequest: payloadJson is required (null/empty).", opId);
        return BadRequest(new { message = "payloadJson is required" });
    }

    CreateDocumentPayload payload;

    try
    {
        payload = System.Text.Json.JsonSerializer.Deserialize<CreateDocumentPayload>(
            payloadJson,
            new System.Text.Json.JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }
        );

        _logger.LogInformation("[{OpId}] Deserialize OK. payload is null? {IsNull}", opId, payload == null);

        if (payload == null)
        {
            _logger.LogWarning("[{OpId}] BadRequest: Invalid payloadJson (deserialized null).", opId);
            return BadRequest(new { message = "Invalid payloadJson" });
        }
    }
    catch (Exception ex)
    {
        _logger.LogWarning(ex, "[{OpId}] BadRequest: Invalid payloadJson (deserialize exception).", opId);
        return BadRequest(new { message = "Invalid payloadJson", detail = ex.Message });
    }

    // --- Payload dump ---
    _logger.LogInformation(
        "[{OpId}] Payload fields: FileName='{FileName}', Status='{Status}', DocumentTypeId={DocumentTypeId}, PartnerId={PartnerId}, SiteId={SiteId}, CustomMetadataCount={MetaCount}",
        opId,
        payload.FileName,
        payload.Status,
        payload.DocumentTypeId,
        payload.PartnerId,
        payload.SiteId,
        payload.CustomMetadata?.Count ?? 0
    );

    if (payload.CustomMetadata != null && payload.CustomMetadata.Any())
    {
        for (int i = 0; i < payload.CustomMetadata.Count; i++)
        {
            var m = payload.CustomMetadata[i];
            _logger.LogInformation(
                "[{OpId}] Payload.CustomMetadata[{Index}] Key='{Key}', Value='{Value}'",
                opId,
                i,
                (m?.Key ?? "").Trim(),
                Trunc((m?.Value ?? "").Trim(), 500)
            );
        }
    }
    else
    {
        _logger.LogInformation("[{OpId}] Payload.CustomMetadata is NULL or EMPTY.", opId);
    }

    // basic validation
    if (string.IsNullOrWhiteSpace(payload.FileName))
    {
        _logger.LogWarning("[{OpId}] BadRequest: FileName is required (payload.FileName empty).", opId);
        return BadRequest(new { message = "FileName is required" });
    }

    if (payload.Status == null)
    {
        _logger.LogWarning("[{OpId}] BadRequest: Status is required (payload.Status null).", opId);
        return BadRequest(new { message = "Status is required" });
    }

    // Status enum parse
    if (!Enum.TryParse<DocumentStatusEnum>(payload.Status, true, out var statusEnum))
    {
        _logger.LogWarning("[{OpId}] BadRequest: Invalid Status enum value. payload.Status='{Status}'", opId, payload.Status);
        return BadRequest(new { message = "Invalid Status enum value", status = payload.Status });
    }

    _logger.LogInformation("[{OpId}] Status parsed OK: {StatusEnum}", opId, statusEnum);

    try
    {
        // 1) file mentés
        var uploadsRoot = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "documents");
        _logger.LogInformation("[{OpId}] uploadsRoot={UploadsRoot}", opId, uploadsRoot);

        Directory.CreateDirectory(uploadsRoot);

        var safeFileName = Path.GetFileName(file.FileName);
        var storedFileName = $"{Guid.NewGuid():N}_{safeFileName}";
        var fullPath = Path.Combine(uploadsRoot, storedFileName);

        _logger.LogInformation(
            "[{OpId}] Saving file: safeFileName={SafeFileName}, storedFileName={StoredFileName}, fullPath={FullPath}",
            opId,
            safeFileName,
            storedFileName,
            fullPath
        );

        await using (var stream = System.IO.File.Create(fullPath))
        {
            await file.CopyToAsync(stream);
        }

        // file verify
        var fi = new FileInfo(fullPath);
        _logger.LogInformation(
            "[{OpId}] File saved OK? exists={Exists}, size={Size}",
            opId,
            fi.Exists,
            fi.Exists ? fi.Length : -1
        );

        var relativePath = $"/uploads/documents/{storedFileName}";
        _logger.LogInformation("[{OpId}] relativePath={RelativePath}", opId, relativePath);

        // 2) DTO a service felé
        var dto = new CreateDocumentDto
        {
            FileName = payload.FileName.Trim(),
            FilePath = relativePath,
            DocumentTypeId = payload.DocumentTypeId,
            PartnerId = payload.PartnerId,
            SiteId = payload.SiteId,
            Status = statusEnum,
            CustomMetadata = payload.CustomMetadata ?? new List<MetadataEntry>()
        };

        _logger.LogInformation(
            "[{OpId}] CreateDocumentDto built: FileName='{FileName}', FilePath='{FilePath}', DocumentTypeId={DocumentTypeId}, PartnerId={PartnerId}, SiteId={SiteId}, Status={Status}, CustomMetadataCount={MetaCount}",
            opId,
            dto.FileName,
            dto.FilePath,
            dto.DocumentTypeId,
            dto.PartnerId,
            dto.SiteId,
            dto.Status,
            dto.CustomMetadata?.Count ?? 0
        );

        if (dto.CustomMetadata != null && dto.CustomMetadata.Any())
        {
            for (int i = 0; i < dto.CustomMetadata.Count; i++)
            {
                var m = dto.CustomMetadata[i];
                _logger.LogInformation(
                    "[{OpId}] DTO.CustomMetadata[{Index}] Key='{Key}', Value='{Value}'",
                    opId,
                    i,
                    (m?.Key ?? "").Trim(),
                    Trunc((m?.Value ?? "").Trim(), 500)
                );
            }
        }

        _logger.LogInformation("[{OpId}] Calling _documentService.CreateDocumentAsync...", opId);

        var createdDoc = await _documentService.CreateDocumentAsync(dto);

        _logger.LogInformation(
            "[{OpId}] Service returned: DocumentId={DocumentId}, FileName='{FileName}', PartnerId={PartnerId}, SiteId={SiteId}, Status={Status}",
            opId,
            createdDoc?.DocumentId,
            createdDoc?.FileName,
            createdDoc?.PartnerId,
            createdDoc?.SiteId,
            createdDoc?.Status
        );

        // Ha a DocumentDto-ban már benne van:
        _logger.LogInformation(
            "[{OpId}] Service returned CustomMetadataCount={Count}",
            opId,
            createdDoc?.CustomMetadata?.Count ?? -1
        );

        if (createdDoc?.CustomMetadata != null && createdDoc.CustomMetadata.Any())
        {
            for (int i = 0; i < createdDoc.CustomMetadata.Count; i++)
            {
                var m = createdDoc.CustomMetadata[i];
                _logger.LogInformation(
                    "[{OpId}] Returned.CustomMetadata[{Index}] Key='{Key}', Value='{Value}'",
                    opId,
                    i,
                    (m?.Key ?? "").Trim(),
                    Trunc((m?.Value ?? "").Trim(), 500)
                );
            }
        }

        _logger.LogInformation("[{OpId}] CreateDocument END OK. Returning 201.", opId);
        return CreatedAtAction(nameof(GetDocument), new { id = createdDoc.DocumentId }, createdDoc);
    }
    catch (UnauthorizedAccessException ex)
    {
        _logger.LogWarning(ex, "[{OpId}] Unauthorized attempt to create document", opId);
        return StatusCode(403, new { message = "Unauthorized to create document" });
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "[{OpId}] Error creating document", opId);
        return StatusCode(500, new { message = "An unexpected error occurred", detail = ex.Message });
    }

    static string Trunc(string? s, int max)
        => string.IsNullOrEmpty(s) ? "" : (s.Length <= max ? s : s.Substring(0, max) + "…");
}

// Payload class (teheted a Models alá is)
public class CreateDocumentPayload
{
    public string FileName { get; set; }
    public int? DocumentTypeId { get; set; }
    public int? PartnerId { get; set; }
    public int? SiteId { get; set; }
    public string Status { get; set; }
    public List<MetadataEntry>? CustomMetadata { get; set; }
}


        // PUT api/documents/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateDocument(int id, [FromBody] DocumentDto documentDto)
        {
            if (documentDto == null || id != documentDto.DocumentId)
            {
                return BadRequest(new { error = "ID mismatch or invalid document" });
            }

            try
            {
                var updatedDoc = await _documentService.UpdateDocumentAsync(id, documentDto);
                if (updatedDoc == null)
                {
                    _logger.LogWarning("Document {DocumentId} not found", id);
                    return NotFound(new { error = $"Document {id} not found" });
                }

                return Ok(updatedDoc);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning("Unauthorized attempt to update document {DocumentId}: {Message}", id, ex.Message);
                return StatusCode(403, new { message = "Unauthorized to update document" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating document {DocumentId}: {Message}", id, ex.Message);
                return StatusCode(500, new { error = "Failed to update document" });
            }
        }

        // DELETE api/documents/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteDocument(int id)
        {
            try
            {
                var deleted = await _documentService.DeleteDocumentAsync(id);

                if (!deleted)
                {
                    _logger.LogWarning("Document {DocumentId} not found (or user not allowed)", id);
                    return NotFound(new { error = $"Document {id} not found" });
                }

                return NoContent();
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning("Unauthorized attempt to delete document {DocumentId}: {Message}", id, ex.Message);
                return StatusCode(403, new { message = "Unauthorized to delete document" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting document {DocumentId}: {Message}", id, ex.Message);
                return StatusCode(500, new { error = "Failed to delete document" });
            }
        }

        // GET api/documents/search?term=invoice&take=20
        [HttpGet("search")]
        public async Task<IActionResult> Search([FromQuery] string term, [FromQuery] int take = 20)
        {
            try
            {
                take = Math.Clamp(take, 1, 50);
                var results = await _documentService.SearchDocumentsAsync(term, take);
                return Ok(results);
            }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                error = "Search failed",
                exception = ex.GetType().FullName,
                message = ex.Message,
                inner = ex.InnerException?.Message
            });
        }

        }

    }

        [ApiController]
    [Route("api/documenttypes")]
    [Authorize]
    public class DocumentTypesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<DocumentTypesController> _logger;

        public DocumentTypesController(
            ApplicationDbContext context,
            ILogger<DocumentTypesController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET /api/documenttypes/select?search=abc
        [HttpGet("select")]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public async Task<IActionResult> Select([FromQuery] string search = "")
        {
            try
            {
                search ??= "";

                var query = _context.DocumentTypes
                    .AsNoTracking();

                if (!string.IsNullOrWhiteSpace(search))
                {
                    query = query.Where(dt => dt.Name.Contains(search));
                }

                var result = await query
                    .OrderBy(dt => dt.Name)
                    .Take(200)
                    .Select(dt => new
                    {
                        id = dt.DocumentTypeId,
                        text = dt.Name
                    })
                    .ToListAsync();

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching document types for select");
                return StatusCode(500, new { error = "Failed to retrieve document types" });
            }
        }
    }

}
