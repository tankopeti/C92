using Cloud9_2.Data;
using Cloud9_2.Models;
using Cloud9_2.Services; // Add this for CustomerCommunicationService
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;
using System.Security.Claims; // For getting current user

namespace Cloud9_2.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class CustomerCommunicationController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<CustomerCommunicationController> _logger;
        private readonly CustomerCommunicationService _communicationService; // Inject the service

        public CustomerCommunicationController(
            ApplicationDbContext context,
            ILogger<CustomerCommunicationController> logger,
            CustomerCommunicationService communicationService) // Add to constructor
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _communicationService = communicationService ?? throw new ArgumentNullException(nameof(communicationService));
        }

        [HttpGet("types")]
        public async Task<IActionResult> GetCommunicationTypes()
        {
            try
            {
                var types = await _context.CommunicationTypes
                    .AsNoTracking()
                    .Select(ct => new
                    {
                        id = ct.CommunicationTypeId,
                        text = ct.Name
                    })
                    .OrderBy(ct => ct.id)
                    .ToListAsync();

                _logger.LogInformation("Fetched {Count} communication types", types.Count);
                return Ok(types.Any() ? types : new List<object>());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching communication types: {Message}", ex.Message);
                return StatusCode(500, new { title = "Internal server error", errors = new { General = new[] { $"Failed to retrieve communication types: {ex.Message}" } } });
            }
        }

        [HttpGet("statuses")]
        public async Task<IActionResult> GetCommunicationStatuses()
        {
            try
            {
                var statuses = await _context.CommunicationStatuses
                    .AsNoTracking()
                    .Select(cs => new
                    {
                        id = cs.StatusId,
                        text = cs.Name
                    })
                    .OrderBy(cs => cs.id)
                    .ToListAsync();

                _logger.LogInformation("Fetched {Count} communication statuses", statuses.Count);
                return Ok(statuses.Any() ? statuses : new List<object>());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching communication statuses: {Message}", ex.Message);
                return StatusCode(500, new { title = "Internal server error", errors = new { General = new[] { $"Failed to retrieve communication statuses: {ex.Message}" } } });
            }
        }

        // NEW: POST /api/customercommunication (Create)
        [HttpPost]
        public async Task<IActionResult> CreateCommunication([FromBody] CustomerCommunicationDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new { title = "Validation Error", errors = ModelState });
                }

                // Get current username from claims (for posts/responsible)
                var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier); // Or User.Identity.Name if using username
                var currentUserName = User.FindFirstValue(ClaimTypes.Name) ?? "System";

                // Call service to create core communication
                await _communicationService.RecordCommunicationAsync(dto, "Create");

                // Handle Posts (if any)
                if (dto.Posts != null && dto.Posts.Any())
                {
                    foreach (var post in dto.Posts)
                    {
                        await _communicationService.AddCommunicationPostAsync(dto.CustomerCommunicationId, post.Content ?? "", currentUserId ?? "");
                    }
                }

                // Handle CurrentResponsible / ResponsibleHistory (assign the latest responsible)
                if (dto.CurrentResponsible?.ResponsibleId != null)
                {
                    await _communicationService.AssignResponsibleAsync(dto.CustomerCommunicationId, dto.CurrentResponsible.ResponsibleId, currentUserId ?? "");
                }

                _logger.LogInformation("Created communication {Id}", dto.CustomerCommunicationId);
                return Ok(new { communicationId = dto.CustomerCommunicationId }); // Return the new ID
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Validation failed for create communication");
                return BadRequest(new { title = "Validation Error", errors = new { General = new[] { ex.Message } } });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating communication");
                return StatusCode(500, new { title = "Internal Server Error", errors = new { General = new[] { ex.Message } } });
            }
        }

        // NEW: PUT /api/customercommunication/{id} (Update)
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateCommunication(int id, [FromBody] CustomerCommunicationDto dto)
        {
            try
            {
                if (id != dto.CustomerCommunicationId)
                {
                    return BadRequest(new { title = "Invalid ID", errors = new { General = new[] { "ID mismatch" } } });
                }

                if (!ModelState.IsValid)
                {
                    return BadRequest(new { title = "Validation Error", errors = ModelState });
                }

                // Get current username from claims
                var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var currentUserName = User.FindFirstValue(ClaimTypes.Name) ?? "System";

                // Call service to update core communication
                await _communicationService.UpdateCommunicationAsync(dto);

                // Handle new Posts (if any in payload)
                if (dto.Posts != null && dto.Posts.Any())
                {
                    foreach (var post in dto.Posts)
                    {
                        await _communicationService.AddCommunicationPostAsync(dto.CustomerCommunicationId, post.Content ?? "", currentUserId ?? "");
                    }
                }

                // Handle updated Responsible (if changed)
                if (dto.CurrentResponsible?.ResponsibleId != null)
                {
                    await _communicationService.AssignResponsibleAsync(dto.CustomerCommunicationId, dto.CurrentResponsible.ResponsibleId, currentUserId ?? "");
                }

                _logger.LogInformation("Updated communication {Id}", id);
                return Ok(new { communicationId = id });
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Validation failed for update communication {Id}", id);
                return BadRequest(new { title = "Validation Error", errors = new { General = new[] { ex.Message } } });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating communication {Id}", id);
                return StatusCode(500, new { title = "Internal Server Error", errors = new { General = new[] { ex.Message } } });
            }
        }

        // Optional: Add other endpoints like /history, /post, /assign-responsible, /delete if missing
        // For example:
        [HttpGet("{id}/history")]
        public async Task<IActionResult> GetCommunicationHistory(int id)
        {
            try
            {
                var history = await _communicationService.GetCommunicationHistoryAsync(id);
                return Ok(history);
            }
            catch (ArgumentException ex)
            {
                return NotFound(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching history for {Id}", id);
                return StatusCode(500, new { error = ex.Message });
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
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
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
                    _logger.LogWarning("Model validation failed for AssignResponsible {Id}: {Errors}", id, ModelState);
                    return BadRequest(new
                    {
                        title = "Validation Error",
                        errors = ModelState.ToDictionary(
                            kvp => kvp.Key,
                            kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToArray()
                        )
                    });
                }

                if (id <= 0)
                {
                    return BadRequest(new { error = "Invalid Communication ID." });
                }

                var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(currentUserId))
                {
                    return Unauthorized(new { error = "Current user not authenticated." });
                }

                await _communicationService.AssignResponsibleAsync(id, dto.ResponsibleUserId, currentUserId);
                _logger.LogInformation("Responsible assigned for communication {Id} to user {UserId} by {CurrentUserId}", id, dto.ResponsibleUserId, currentUserId);
                return Ok(new { message = "Responsible assigned successfully." });
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Argument error assigning responsible to {Id}", id);
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error assigning responsible to {Id}", id);
                return StatusCode(500, new { error = "An unexpected error occurred. Please try again later." });
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
            catch (ArgumentException ex)
            {
                return NotFound(new { error = ex.Message });
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
            return Ok(users); // Returns JSON array
    }
    }
}