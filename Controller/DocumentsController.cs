using Cloud9_2.Data;
using Cloud9_2.Models;
using Cloud9_2.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace Cloud9_2.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class DocumentsController : ControllerBase
    {
        private readonly IDocumentService _documentService;
        private readonly ILogger<DocumentsController> _logger;
        private readonly ApplicationDbContext _context;

        public DocumentsController(
            IDocumentService documentService,
            ILogger<DocumentsController> logger,
            ApplicationDbContext context)
        {
            _documentService = documentService ?? throw new ArgumentNullException(nameof(documentService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

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
                    sortBy: "uploaddate",
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

        // GET api/documents?search=&documentTypeId=&partnerId=&siteId=&sortBy=uploaddate&page=1&pageSize=10
        [HttpGet]
        public async Task<IActionResult> GetDocuments(
            [FromQuery] string search = "",
            [FromQuery] int? documentTypeId = null,
            [FromQuery] int? partnerId = null,
            [FromQuery] int? siteId = null,
            [FromQuery] string status = null,
            [FromQuery] string sortBy = "uploaddate",
            [FromQuery] int skip = 0,
            [FromQuery] int take = 50)
        {
            try
            {
                DocumentStatusEnum? statusEnum = null;
                if (!string.IsNullOrEmpty(status) && status != "all" && Enum.TryParse<DocumentStatusEnum>(status, true, out var parsedStatus))
                {
                    statusEnum = parsedStatus;
                }

                var docs = await _documentService.GetDocumentsAsync(search, documentTypeId, partnerId, siteId, statusEnum, sortBy, skip, take);
                return Ok(docs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching documents");
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

        // POST api/documents
        [HttpPost]
        public async Task<IActionResult> CreateDocument([FromBody] CreateDocumentDto documentDto)
        {
            if (documentDto == null)
            {
                _logger.LogError("Received null documentDto in CreateDocument");
                return BadRequest(new { message = "Document data is null" });
            }

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                _logger.LogWarning("Invalid model state for documentDto: {Errors}", string.Join(", ", errors));
                return BadRequest(new { message = "Invalid input data", errors });
            }

            try
            {
                var createdDoc = await _documentService.CreateDocumentAsync(documentDto);
                return CreatedAtAction(nameof(GetDocument), new { id = createdDoc.DocumentId }, createdDoc);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning("Unauthorized attempt to create document: {Message}", ex.Message);
                return StatusCode(403, new { message = "Unauthorized to create document" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating document: {Message}", ex.Message);
                return StatusCode(500, new { message = "An unexpected error occurred", detail = ex.Message });
            }
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
                    _logger.LogWarning("Document {DocumentId} not found", id);
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
    }
}