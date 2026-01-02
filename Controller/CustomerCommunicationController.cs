using Cloud9_2.Data;
using Cloud9_2.Models;
using Cloud9_2.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using System;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace Cloud9_2.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class CustomerCommunicationController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<CustomerCommunicationController> _logger;
        private readonly CustomerCommunicationService _communicationService;

        public CustomerCommunicationController(
            ApplicationDbContext context,
            ILogger<CustomerCommunicationController> logger,
            CustomerCommunicationService communicationService)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _communicationService = communicationService ?? throw new ArgumentNullException(nameof(communicationService));
        }

        // ============================================================
        // ✅ SITES-MINTÁS LISTA (Load more + keresés + filter + sort)
        // GET /api/CustomerCommunicationIndex?pageNumber=1&pageSize=20&search=&typeFilter=&sortBy=
        // Header: X-Total-Count
        // ============================================================
[HttpGet("/api/CustomerCommunicationIndex")]
public async Task<IActionResult> CustomerCommunicationIndex(
    [FromQuery] int pageNumber = 1,
    [FromQuery] int pageSize = 20,
    [FromQuery] string search = "",
    [FromQuery] string typeFilter = "all",
    [FromQuery] string sortBy = "CommunicationDate")
{
    try
    {
        pageNumber = pageNumber <= 0 ? 1 : pageNumber;
        pageSize = pageSize <= 0 ? 20 : pageSize;
        search = (search ?? "").Trim();
        typeFilter = string.IsNullOrWhiteSpace(typeFilter) ? "all" : typeFilter;

        // Base query (csak a "core" tábla)
        var baseQ = _context.CustomerCommunications.AsNoTracking().AsQueryable();

        // JOIN források
        var types = _context.CommunicationTypes.AsNoTracking();
        var statuses = _context.CommunicationStatuses.AsNoTracking();
        var users = _context.Users.AsNoTracking();
        var partners = _context.Partners.AsNoTracking();

        // felelősök (CommunicationResponsibles) – current responsible = legutolsó AssignedAt
        var responsibles = _context.CommunicationResponsibles.AsNoTracking();

        // PROJEKCIÓ
        var q =
            from c in baseQ
            join t in types on c.CommunicationTypeId equals t.CommunicationTypeId into tg
            from t in tg.DefaultIfEmpty()

            join st in statuses on c.StatusId equals st.StatusId into stg
            from st in stg.DefaultIfEmpty()

            join p in partners on c.PartnerId equals p.PartnerId into pg
            from p in pg.DefaultIfEmpty()

            select new
            {
                c.CustomerCommunicationId,
                c.Date,
                c.Subject,
                c.CommunicationTypeId,
                CommunicationTypeName = t != null ? t.Name : null,

                c.StatusId,
                StatusName = st != null ? st.Name : null,

                c.PartnerId,
                PartnerName = p != null ? p.Name : null,

                c.SiteId,

                // ✅ current responsible id (CommunicationResponsibles legutolsó)
                CurrentResponsibleId = responsibles
                    .Where(r => r.CustomerCommunicationId == c.CustomerCommunicationId)
                    .OrderByDescending(r => r.AssignedAt)
                    .Select(r => r.ResponsibleId)
                    .FirstOrDefault(),

                // ✅ current responsible name (Users alapján)
                CurrentResponsibleName = users
                    .Where(u2 => u2.Id == responsibles
                        .Where(r => r.CustomerCommunicationId == c.CustomerCommunicationId)
                        .OrderByDescending(r => r.AssignedAt)
                        .Select(r => r.ResponsibleId)
                        .FirstOrDefault())
                    .Select(u2 => u2.UserName)
                    .FirstOrDefault()
            };

        // TypeFilter: name alapján (ha nálad inkább id, akkor ezt majd átírjuk)
        if (typeFilter != "all")
        {
            q = q.Where(x => x.CommunicationTypeName == typeFilter);
        }

        // Search: Subject + PartnerName + ResponsibleName
        if (!string.IsNullOrWhiteSpace(search))
        {
            q = q.Where(x =>
                (x.Subject != null && x.Subject.Contains(search)) ||
                (x.PartnerName != null && x.PartnerName.Contains(search)) ||
                (x.CurrentResponsibleName != null && x.CurrentResponsibleName.Contains(search)) ||
                (x.CommunicationTypeName != null && x.CommunicationTypeName.Contains(search)) ||
                (x.StatusName != null && x.StatusName.Contains(search))
            );
        }

        var total = await q.CountAsync();
        Response.Headers["X-Total-Count"] = total.ToString();

        // Sort
        q = sortBy switch
        {
            "CommunicationId" => q.OrderByDescending(x => x.CustomerCommunicationId),
            "PartnerName" => q.OrderBy(x => x.PartnerName),
            _ => q.OrderByDescending(x => x.CustomerCommunicationId) // "legutóbbi" közelítés
        };

        var items = await q
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new
            {
                customerCommunicationId = x.CustomerCommunicationId,
                communicationTypeName = x.CommunicationTypeName ?? "—",
                partnerName = x.PartnerName ?? "—",
                responsibleName = x.CurrentResponsibleName ?? "—",
                dateText = x.Date.ToString("yyyy-MM-dd"),
                subject = x.Subject ?? "",
                statusName = x.StatusName ?? "—",
                statusDisplayName = x.StatusName ?? "—"
            })
            .ToListAsync();

        return Ok(items);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error loading customer communications index");
        return StatusCode(500, "Failed to load customer communications.");
    }
}


        // ============================================================
        // ✅ VIEW MODAL HTML
        // GET /api/CustomerCommunicationView?id=123
        // ============================================================
[HttpGet("/api/CustomerCommunicationView")]
public async Task<IActionResult> CustomerCommunicationView([FromQuery] int id)
{
    try
    {
        var c = await _context.CustomerCommunications
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.CustomerCommunicationId == id);

        if (c == null) return NotFound("Not found");

        var typeName = await _context.CommunicationTypes.AsNoTracking()
            .Where(t => t.CommunicationTypeId == c.CommunicationTypeId)
            .Select(t => t.Name)
            .FirstOrDefaultAsync();

        var statusName = await _context.CommunicationStatuses.AsNoTracking()
            .Where(s => s.StatusId == c.StatusId)
            .Select(s => s.Name)
            .FirstOrDefaultAsync();

        // ✅ legutóbbi felelős (CommunicationResponsibles alapján)
        var responsibleId = await _context.CommunicationResponsibles
            .AsNoTracking()
            .Where(r => r.CustomerCommunicationId == id)
            .OrderByDescending(r => r.AssignedAt)
            .Select(r => r.ResponsibleId)
            .FirstOrDefaultAsync();

        var responsibleName = await _context.Users.AsNoTracking()
            .Where(u => u.Id == responsibleId)
            .Select(u => u.UserName)
            .FirstOrDefaultAsync();

        // (ha ettől függetlenül akarod az Agent-et is mutatni, hagyd meg külön)
        // var agentName = await _context.Users.AsNoTracking()
        //     .Where(u => u.Id == c.AgentId)
        //     .Select(u => u.UserName)
        //     .FirstOrDefaultAsync();

                // ✅ PartnerName / SiteName
        string? partnerName = null;
        if (c.PartnerId.HasValue)
        {
            partnerName = await _context.Partners.AsNoTracking()
                .Where(p => p.PartnerId == c.PartnerId.Value)
                .Select(p => p.Name)   // ha nálad más mezőnév: pl. Name
                .FirstOrDefaultAsync();
        }

        string? siteName = null;
        if (c.SiteId.HasValue)
        {
            siteName = await _context.Sites.AsNoTracking()
                .Where(s => s.SiteId == c.SiteId.Value)
                .Select(s => s.SiteName)      // ha nálad más mezőnév: pl. Name
                .FirstOrDefaultAsync();
        }


        string enc(string? s) => WebUtility.HtmlEncode(string.IsNullOrWhiteSpace(s) ? "—" : s);

        var sb = new StringBuilder();
        sb.AppendLine("<table class='table table-sm table-borderless'><tbody>");
        sb.AppendLine($"<tr><th>ID</th><td>{c.CustomerCommunicationId}</td></tr>");
        sb.AppendLine($"<tr><th>Típus</th><td>{enc(typeName)}</td></tr>");
        sb.AppendLine($"<tr><th>Partner</th><td>{enc(partnerName)} <span class='text-muted'>(#{enc(c.PartnerId?.ToString())})</span></td></tr>");
        sb.AppendLine($"<tr><th>Telephely</th><td>{enc(siteName)} <span class='text-muted'>(#{enc(c.SiteId?.ToString())})</span></td></tr>");
        sb.AppendLine($"<tr><th>Dátum</th><td>{enc(c.Date.ToString("yyyy-MM-dd"))}</td></tr>");
        sb.AppendLine($"<tr><th>Tárgy</th><td>{enc(c.Subject)}</td></tr>");
        sb.AppendLine($"<tr><th>Tartalom</th><td>{enc(c.Note)}</td></tr>");
        sb.AppendLine($"<tr><th>Státusz</th><td>{enc(statusName)}</td></tr>");

        // ✅ itt a változás: agentName helyett responsibleName
        sb.AppendLine($"<tr><th>Felelős</th><td>{enc(responsibleName)}</td></tr>");

        sb.AppendLine($"<tr><th>Megjegyzések</th><td>{enc(c.Metadata)}</td></tr>");
        sb.AppendLine($"<tr><th>Csatolmány</th><td>{enc(c.AttachmentPath)}</td></tr>");
        sb.AppendLine("</tbody></table>");

        return Content(sb.ToString(), "text/html; charset=utf-8");
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error loading view for {Id}", id);
        return StatusCode(500, "Failed to load view.");
    }
}


        // ============================================================
        // ✅ EDIT MODAL JSON
        // GET /api/CustomerCommunicationGet?id=123
        // ============================================================
[HttpGet("/api/CustomerCommunicationGet")]
public async Task<IActionResult> CustomerCommunicationGet([FromQuery] int id)
{
    try
    {
        var c = await _context.CustomerCommunications
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.CustomerCommunicationId == id);

        if (c == null) return NotFound();

        var responsibleId = await _context.CommunicationResponsibles
            .AsNoTracking()
            .Where(r => r.CustomerCommunicationId == id)
            .OrderByDescending(r => r.AssignedAt)
            .Select(r => r.ResponsibleId)
            .FirstOrDefaultAsync();

        return Ok(new
        {
            customerCommunicationId = c.CustomerCommunicationId,
            communicationTypeId = c.CommunicationTypeId,
            partnerId = c.PartnerId,
            siteId = c.SiteId,
            responsibleContactId = responsibleId, // ✅ EZ MÁR JÓ
            dateIso = c.Date.ToString("yyyy-MM-dd"),
            statusId = c.StatusId,
            subject = c.Subject,
            note = c.Note,
            metadata = c.Metadata
        });
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error loading communication for edit {Id}", id);
        return StatusCode(500, "Failed to load communication.");
    }
}



        // ============================================================
        // ✅ UPDATE (JS POST)
        // POST /api/CustomerCommunicationUpdate
        // ============================================================
        public class UpdateCommunicationRequest
        {
            public int CustomerCommunicationId { get; set; }
            public int? CommunicationTypeId { get; set; }
            public int? PartnerId { get; set; }
            public int? SiteId { get; set; }
            public string? ResponsibleContactId { get; set; } // Identity userId
            public string? DateIso { get; set; } // yyyy-MM-dd
            public int? StatusId { get; set; }
            public string? Subject { get; set; }
            public string? Note { get; set; }
            public string? Metadata { get; set; }
        }

        [HttpPost("/api/CustomerCommunicationUpdate")]
        public async Task<IActionResult> CustomerCommunicationUpdate([FromBody] UpdateCommunicationRequest req)
        {
            try
            {
                if (req == null || req.CustomerCommunicationId <= 0)
                    return BadRequest(new { error = "Invalid request" });

                var dto = new CustomerCommunicationDto
                {
                    CustomerCommunicationId = req.CustomerCommunicationId,
                    CommunicationTypeId = req.CommunicationTypeId ?? 0,
                    PartnerId = req.PartnerId,
                    SiteId = req.SiteId,
                    StatusId = req.StatusId ?? 0,
                    Subject = req.Subject,
                    Note = req.Note,
                    Metadata = req.Metadata,
                    Date = DateTime.TryParse(req.DateIso, out var dt) ? dt : DateTime.Today,
                    CurrentResponsible = string.IsNullOrWhiteSpace(req.ResponsibleContactId)
                        ? null
                        : new CommunicationResponsibleDto { ResponsibleId = req.ResponsibleContactId }
                };

                var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";

                await _communicationService.UpdateCommunicationAsync(dto);

                if (!string.IsNullOrWhiteSpace(dto.CurrentResponsible?.ResponsibleId))
                {
                    await _communicationService.AssignResponsibleAsync(
                        dto.CustomerCommunicationId,
                        dto.CurrentResponsible.ResponsibleId!,
                        currentUserId);
                }

                return Ok(new { communicationId = req.CustomerCommunicationId });
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Validation failed for update communication");
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating communication");
                return StatusCode(500, "Failed to update communication.");
            }
        }

        // ============================================================
        // ✅ DELETE (JS DELETE)
        // DELETE /api/CustomerCommunicationDelete?id=123
        // ============================================================
        [HttpDelete("/api/CustomerCommunicationDelete")]
        public async Task<IActionResult> CustomerCommunicationDelete([FromQuery] int id)
        {
            try
            {
                await _communicationService.DeleteCommunicationAsync(id);
                return Ok();
            }
            catch (ArgumentException ex)
            {
                return NotFound(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting communication {Id}", id);
                return StatusCode(500, "Failed to delete communication.");
            }
        }

        // ============================================================
        // ✅ LOOKUPS - RESPONSIBLES (Identity users)
        // GET /api/Lookups/Responsibles
        // ============================================================
        [HttpGet("/api/Lookups/Responsibles")]
        public async Task<IActionResult> GetResponsibles([FromQuery] string search = "")
        {
            try
            {
                search = (search ?? "").Trim();

                var q = _context.Users.AsNoTracking().AsQueryable();

                if (!string.IsNullOrWhiteSpace(search))
                {
                    q = q.Where(u =>
                        (u.UserName != null && u.UserName.Contains(search)) ||
                        (u.Email != null && u.Email.Contains(search)) ||
                        (u.NormalizedUserName != null && u.NormalizedUserName.Contains(search)));
                }

                var users = await q
                    .OrderBy(u => u.UserName)
                    .Select(u => new { id = u.Id, name = u.UserName })
                    .Take(100)
                    .ToListAsync();

                return Ok(users);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading responsibles");
                return StatusCode(500, "Failed to load responsibles");
            }
        }

        // ============================================================
        // ✅ EXISTING ENDPOINTS (MEGTARTVA)
        // ============================================================

        [HttpGet("types")]
        public async Task<IActionResult> GetCommunicationTypes()
        {
            try
            {
                var types = await _context.CommunicationTypes
                    .AsNoTracking()
                    .Select(ct => new { id = ct.CommunicationTypeId, text = ct.Name })
                    .OrderBy(ct => ct.id)
                    .ToListAsync();

                _logger.LogInformation("Fetched {Count} communication types", types.Count);
                return Ok(types.Any() ? types : new List<object>());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching communication types: {Message}", ex.Message);
                return StatusCode(500, new
                {
                    title = "Internal server error",
                    errors = new { General = new[] { $"Failed to retrieve communication types: {ex.Message}" } }
                });
            }
        }

        [HttpGet("statuses")]
        public async Task<IActionResult> GetCommunicationStatuses()
        {
            try
            {
                var statuses = await _context.CommunicationStatuses
                    .AsNoTracking()
                    .Select(cs => new { id = cs.StatusId, text = cs.Name })
                    .OrderBy(cs => cs.id)
                    .ToListAsync();

                _logger.LogInformation("Fetched {Count} communication statuses", statuses.Count);
                return Ok(statuses.Any() ? statuses : new List<object>());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching communication statuses: {Message}", ex.Message);
                return StatusCode(500, new
                {
                    title = "Internal server error",
                    errors = new { General = new[] { $"Failed to retrieve communication statuses: {ex.Message}" } }
                });
            }
        }

        // POST /api/customercommunication (Create)
        [HttpPost]
        public async Task<IActionResult> CreateCommunication([FromBody] CustomerCommunicationDto dto)
        {
            _logger.LogInformation("Create: currentUserId={User}, dto.CurrentResponsible={Resp}",
                User.FindFirstValue(ClaimTypes.NameIdentifier),
                dto.CurrentResponsible?.ResponsibleId);

            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(new { title = "Validation Error", errors = ModelState });

                var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                _logger.LogInformation("Create DTO before record: CustomerCommunicationId={Id}, ResponsibleId={Resp}",
                    dto.CustomerCommunicationId,
                    dto.CurrentResponsible?.ResponsibleId);

                await _communicationService.RecordCommunicationAsync(dto, "Create");

                _logger.LogInformation("Create DTO after record: CustomerCommunicationId={Id}", dto.CustomerCommunicationId);


                if (dto.Posts != null && dto.Posts.Any())
                {
                    foreach (var post in dto.Posts)
                        await _communicationService.AddCommunicationPostAsync(dto.CustomerCommunicationId, post.Content ?? "", currentUserId ?? "");
                }

                if (dto.CurrentResponsible?.ResponsibleId != null)
                {
                    await _communicationService.AssignResponsibleAsync(dto.CustomerCommunicationId, dto.CurrentResponsible.ResponsibleId, currentUserId ?? "");
                }

                _logger.LogInformation("Created communication {Id}", dto.CustomerCommunicationId);
                return Ok(new { communicationId = dto.CustomerCommunicationId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating communication");
                return StatusCode(500, new { title = "Internal Server Error", errors = new { General = new[] { ex.Message } } });
            }
        }

        // PUT /api/customercommunication/{id} (Update)
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateCommunication(int id, [FromBody] CustomerCommunicationDto dto)
        {
            _logger.LogInformation("Create: currentUserId={User}, dto.CurrentResponsible={Resp}",
            User.FindFirstValue(ClaimTypes.NameIdentifier),
            dto.CurrentResponsible?.ResponsibleId);

            try
            {
                if (id != dto.CustomerCommunicationId)
                    return BadRequest(new { title = "Invalid ID", errors = new { General = new[] { "ID mismatch" } } });

                if (!ModelState.IsValid)
                    return BadRequest(new { title = "Validation Error", errors = ModelState });

                var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                await _communicationService.UpdateCommunicationAsync(dto);

                if (dto.Posts != null && dto.Posts.Any())
                {
                    foreach (var post in dto.Posts)
                        await _communicationService.AddCommunicationPostAsync(dto.CustomerCommunicationId, post.Content ?? "", currentUserId ?? "");
                }

                if (dto.CurrentResponsible?.ResponsibleId != null)
                {
                    await _communicationService.AssignResponsibleAsync(dto.CustomerCommunicationId, dto.CurrentResponsible.ResponsibleId, currentUserId ?? "");
                }

                _logger.LogInformation("Updated communication {Id}", id);
                return Ok(new { communicationId = id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating communication {Id}", id);
                return StatusCode(500, new { title = "Internal Server Error", errors = new { General = new[] { ex.Message } } });
            }
        }

// ============================================================
// ✅ HISTORY (AuditLog)
// GET /api/CustomerCommunicationHistory?id=123
// ============================================================
[HttpGet("/api/CustomerCommunicationHistory")]
public async Task<IActionResult> CustomerCommunicationHistory([FromQuery] int id)
{
    try
    {
        // 1) CustomerCommunication változások
        var comm = _context.Set<AuditLog>()
            .AsNoTracking()
            .Where(a => a.EntityType == "CustomerCommunication" && a.EntityId == id);

        // 2) CommunicationResponsible bejegyzések ugyanahhoz a kommunikációhoz:
        // itt a Changes szöveg tartalmazza: "Kommunikáció: #123" (ahogy az interceptorba tettük),
        // ezért így szűrünk.
        // Ha később felveszel CommunicationId mezőt az AuditLog-ba, ez kiváltható pontos join-ra.
        var resp = _context.Set<AuditLog>()
            .AsNoTracking()
            .Where(a => a.EntityType == "CommunicationResponsible" && a.Changes.Contains($"#{id}"));

        var items = await comm
            .Union(resp)
            .OrderByDescending(a => a.ChangedAt)
            .Take(200)
            .Select(a => new
            {
                a.Id,
                action = a.Action,
                changedByName = a.ChangedByName,
                changedAtText = a.ChangedAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm"),
                changes = a.Changes
            })
            .ToListAsync();

        return Ok(items);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error loading history for communication {Id}", id);
        return StatusCode(500, "Failed to load history.");
    }
}


        [HttpPost("{id}/post")]
        public async Task<IActionResult> AddPost(int id, [FromBody] PostDto dto)
        {
            try
            {
                var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                await _communicationService.AddCommunicationPostAsync(id, dto.Content, currentUserId ?? "");
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding post to {Id}", id);
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPost("{id}/assign-responsible")]
        public async Task<IActionResult> AssignResponsible(int id, [FromBody] AssignResponsibleDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new
                    {
                        title = "Validation Error",
                        errors = ModelState.ToDictionary(
                            kvp => kvp.Key,
                            kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToArray()
                        )
                    });
                }

                var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(currentUserId))
                    return Unauthorized(new { error = "Current user not authenticated." });

                await _communicationService.AssignResponsibleAsync(id, dto.ResponsibleUserId, currentUserId);
                return Ok(new { message = "Responsible assigned successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error assigning responsible to {Id}", id);
                return StatusCode(500, new { error = "An unexpected error occurred." });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCommunication(int id)
        {
            try
            {
                await _communicationService.DeleteCommunicationAsync(id);
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting communication {Id}", id);
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet]
        public IActionResult GetUsers()
        {
            var users = _context.Users
                .Select(u => new { Id = u.Id, Name = u.NormalizedUserName })
                .ToList();
            return Ok(users);
        }

        [HttpGet("select")]
        public async Task<IActionResult> GetCommunicationsSelect([FromQuery] int? partnerId, [FromQuery] string search = "")
        {
            try
            {
                var query = _context.CustomerCommunications.AsQueryable();

                if (partnerId.HasValue)
                    query = query.Where(c => c.PartnerId == partnerId);

                if (!string.IsNullOrEmpty(search))
                    query = query.Where(c => c.Subject.Contains(search));

                var result = await query
                    .OrderByDescending(c => c.CustomerCommunicationId)
                    .Select(c => new { id = c.CustomerCommunicationId, text = c.Subject })
                    .Take(50)
                    .ToListAsync();

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading communications for select");
                return StatusCode(500, "Failed to load");
            }
        }
    }
}
