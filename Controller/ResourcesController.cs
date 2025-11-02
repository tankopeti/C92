using Cloud9_2.Data;
using Cloud9_2.Models;
using Cloud9_2.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Cloud9_2.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ResourcesController : ControllerBase
    {
        private readonly ResourceService _resourceService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ResourcesController> _logger;

        public ResourcesController(
            ResourceService resourceService,
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext context,
            ILogger<ResourcesController> logger)
        {
            _resourceService = resourceService ?? throw new ArgumentNullException(nameof(resourceService));
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        // GET: api/resources/select?partnerId=1&search=abc
        [HttpGet("select")]
        public async Task<IActionResult> GetResourcesForSelect([FromQuery] int? partnerId, [FromQuery] string search = "")
        {
            try
            {
                var query = _context.Resources
                    .AsNoTracking()
                    .Where(r => (r.IsActive == true || r.IsActive == null));

                if (partnerId.HasValue)
                    query = query.Where(r => r.PartnerId == partnerId.Value);

                if (!string.IsNullOrEmpty(search))
                    query = query.Where(r => r.Name.Contains(search) || r.Serial.Contains(search));

                var resources = await query
                    .OrderBy(r => r.Name)
                    .Select(r => new
                    {
                        id = r.ResourceId,
                        text = $"{r.Name} ({r.Name})"
                    })
                    .Take(50)
                    .ToListAsync();

                _logger.LogInformation("Fetched {Count} resources for select (PartnerId: {PartnerId}, Search: '{Search}')",
                    resources.Count, partnerId, search);
                return Ok(resources);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching resources for select (PartnerId: {PartnerId})", partnerId);
                return StatusCode(500, new { title = "Internal server error", errors = new { General = new[] { "Failed to retrieve resources" } } });
            }
        }

        // POST: api/resources
        [HttpPost]
        public async Task<ActionResult<ResourceDto>> CreateResource([FromBody] CreateResourceDto createResourceDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { message = "Invalid data provided.", errors = ModelState });
            }

            try
            {
                var user = await _userManager.GetUserAsync(User);
                var modifiedById = user?.Id;

                var resource = await _resourceService.CreateResourceAsync(createResourceDto, modifiedById);
                return CreatedAtAction(nameof(GetResource), new { id = resource.ResourceId }, resource);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating resource");
                return StatusCode(500, new { message = $"An error occurred while creating the resource: {ex.Message}" });
            }
        }

        // PUT: api/resources/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateResource(int id, [FromBody] UpdateResourceDto updateResourceDto)
        {
            if (id != updateResourceDto.ResourceId)
            {
                return BadRequest(new { message = "Resource ID in the body must match the ID in the URL." });
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(new { message = "Invalid data provided.", errors = ModelState });
            }

            try
            {
                var user = await _userManager.GetUserAsync(User);
                var modifiedById = user?.Id;

                var resource = await _resourceService.UpdateResourceAsync(updateResourceDto, modifiedById);
                if (resource == null)
                {
                    return NotFound(new { message = $"Resource with ID {id} not found or inactive." });
                }

                return Ok(resource);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating resource {ResourceId}", id);
                return StatusCode(500, new { message = $"An error occurred while updating the resource: {ex.Message}" });
            }
        }

        // GET: api/resources/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetResource(int id)
        {
            try
            {
                var resource = await _resourceService.GetResourceByIdAsync(id);
                if (resource == null)
                {
                    return NotFound(new { message = $"Resource with ID {id} not found or inactive." });
                }

                return Ok(resource);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving resource {ResourceId}", id);
                return StatusCode(500, new { message = $"An error occurred while retrieving the resource: {ex.Message}" });
            }
        }

        // GET: api/resources
        [HttpGet]
        public async Task<IActionResult> GetAllResources()
        {
            try
            {
                var resources = await _resourceService.GetAllResourcesAsync();
                return Ok(resources);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all resources");
                return StatusCode(500, new { message = $"An error occurred while retrieving resources: {ex.Message}" });
            }
        }

        // DELETE: api/resources/{id} (Soft Delete)
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeactivateResource(int id)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                var modifiedById = user?.Id;

                var result = await _resourceService.DeactivateResourceAsync(id, modifiedById);
                if (!result)
                {
                    return NotFound(new { message = $"Resource with ID {id} not found or already inactive." });
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deactivating resource {ResourceId}", id);
                return StatusCode(500, new { message = $"An error occurred while deactivating the resource: {ex.Message}" });
            }
        }

        // POST: api/resources/{id}/history
        [HttpPost("{id}/history")]
        public async Task<ActionResult<ResourceHistoryDto>> AddHistory(int id, [FromBody] ResourceHistoryDto historyDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { message = "Invalid history data.", errors = ModelState });
            }

            try
            {
                var user = await _userManager.GetUserAsync(User);
                var modifiedById = user?.Id ?? historyDto.ModifiedById;

                var history = await _resourceService.AddHistoryAsync(id, historyDto, modifiedById);
                return CreatedAtAction(nameof(GetResourceHistory), new { resourceId = id }, history);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding history for resource {ResourceId}", id);
                return StatusCode(500, new { message = $"An error occurred while adding history: {ex.Message}" });
            }
        }

        // GET: api/resources/{id}/history
        [HttpGet("{id}/history")]
        public async Task<IActionResult> GetResourceHistory(int id)
        {
            try
            {
                var history = await _resourceService.GetHistoryAsync(id);
                return Ok(history);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving history for resource {ResourceId}", id);
                return StatusCode(500, new { message = $"An error occurred while retrieving history: {ex.Message}" });
            }
        }

        // GET: api/resources/types
        [HttpGet("types")]
        public async Task<IActionResult> GetResourceTypes()
        {
            var types = await _context.ResourceTypes
                .Where(t => t.IsActive == true)
                .Select(t => new { id = t.ResourceTypeId, text = t.Name })
                .ToListAsync();
            return Ok(types);
        }

        // GET: api/resources/statuses
        [HttpGet("statuses")]
        public async Task<IActionResult> GetResourceStatuses()
        {
            var statuses = await _context.ResourceStatuses
                .Where(s => s.IsActive == true)
                .Select(s => new { id = s.ResourceStatusId, text = s.Name })
                .ToListAsync();
            return Ok(statuses);
        }

    }
}