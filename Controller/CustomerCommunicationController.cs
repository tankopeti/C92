using Cloud9_2.Models;
using Cloud9_2.Data;
using Cloud9_2.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Cloud9_2.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class CustomerCommunicationController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly CustomerCommunicationService _service;
        private readonly UserManager<ApplicationUser> _userManager;

        public CustomerCommunicationController(ApplicationDbContext context, CustomerCommunicationService service, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _service = service;
            _userManager = userManager;
        }

        [HttpPost]
        public async Task<IActionResult> SaveCommunication([FromBody] CustomerCommunicationDto model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var validTypes = await _context.CommunicationTypes
                    .Select(t => t.Name)
                    .ToListAsync();
                if (!validTypes.Contains(model.CommunicationTypeName))
                {
                    return BadRequest(new { error = $"Invalid CommunicationTypeName. Valid values: {string.Join(", ", validTypes)}" });
                }

                var communication = new CustomerCommunication
                {
                    CustomerCommunicationId = model.CustomerCommunicationId,
                    ContactId = model.PartnerId, // Use PartnerId from form
                    CommunicationTypeId = await _context.CommunicationTypes
                        .Where(t => t.Name == model.CommunicationTypeName)
                        .Select(t => t.CommunicationTypeId)
                        .FirstOrDefaultAsync(),
                    Subject = model.Subject,
                    Date = model.Date != default ? model.Date : DateTime.UtcNow, // Use UTC
                    Note = model.Note,
                    StatusId = await _context.CommunicationStatuses
                        .Where(s => s.Name == model.StatusName)
                        .Select(s => s.StatusId)
                        .FirstOrDefaultAsync(),
                    Metadata = model.Metadata,
                    AttachmentPath = model.AttachmentPath,
                    PartnerId = model.PartnerId,
                    LeadId = model.LeadId,
                    QuoteId = model.QuoteId,
                    OrderId = model.OrderId,
                    AgentId = _userManager.GetUserId(User) // Set AgentId
                };

                if (communication.CommunicationTypeId == 0)
                {
                    return BadRequest(new { error = "CommunicationType not found" });
                }

                if (communication.StatusId == 0)
                {
                    return BadRequest(new { error = "Status not found" });
                }

                if (model.CustomerCommunicationId == 0)
                {
                    _context.CustomerCommunications.Add(communication);
                }
                else
                {
                    _context.CustomerCommunications.Update(communication);
                }

                await _context.SaveChangesAsync();
                return Ok(new { communicationId = communication.CustomerCommunicationId });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to save communication", details = ex.Message });
            }
        }

        [HttpPost("initial")]
        public async Task<IActionResult> RecordInitialCommunication([FromBody] CustomerCommunicationDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var userId = _userManager.GetUserId(User);
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized("User not found.");

                dto.AgentId = userId;
                dto.Date = DateTime.UtcNow;

                await _service.RecordInitialCommunicationAsync(dto);
                return CreatedAtAction(nameof(GetCommunication), new { id = dto.CustomerCommunicationId }, dto);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception)
            {
                return StatusCode(500, "An error occurred while recording the communication.");
            }
        }

        [HttpPost("escalation")]
        public async Task<IActionResult> RecordEscalation([FromBody] CustomerCommunicationDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var userId = _userManager.GetUserId(User);
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized("User not found.");

                dto.AgentId = userId;
                dto.Date = DateTime.UtcNow;

                await _service.RecordEscalationAsync(dto);
                return CreatedAtAction(nameof(GetCommunication), new { id = dto.CustomerCommunicationId }, dto);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception)
            {
                return StatusCode(500, "An error occurred while recording the escalation.");
            }
        }

        [HttpPost("follow-up")]
        public async Task<IActionResult> RecordFollowUp([FromBody] CustomerCommunicationDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var userId = _userManager.GetUserId(User);
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized("User not found.");

                dto.AgentId = userId;
                dto.Date = DateTime.UtcNow;

                await _service.RecordFollowUpAsync(dto);
                return CreatedAtAction(nameof(GetCommunication), new { id = dto.CustomerCommunicationId }, dto);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception)
            {
                return StatusCode(500, "An error occurred while recording the follow-up.");
            }
        }

        [HttpPost("resolution")]
        public async Task<IActionResult> RecordResolution([FromBody] CustomerCommunicationDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var userId = _userManager.GetUserId(User);
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized("User not found.");

                dto.AgentId = userId;
                dto.Date = DateTime.UtcNow;

                await _service.RecordResolutionAsync(dto);
                return CreatedAtAction(nameof(GetCommunication), new { id = dto.CustomerCommunicationId }, dto);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception)
            {
                return StatusCode(500, "An error occurred while recording the resolution.");
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetCommunication(int id)
        {
            try
            {
                var communications = await _service.ReviewCommunicationsAsync();
                var communication = communications.Find(c => c.CustomerCommunicationId == id);

                if (communication == null)
                    return NotFound($"Communication with ID {id} not found.");

                return Ok(communication);
            }
            catch (Exception)
            {
                return StatusCode(500, "An error occurred while retrieving the communication.");
            }
        }

        [HttpGet("order/{orderId}")]
        public async Task<IActionResult> GetCommunicationsByOrder(int orderId)
        {
            try
            {
                var communications = await _service.ReviewCommunicationsAsync(orderId);
                return Ok(communications);
            }
            catch (Exception)
            {
                return StatusCode(500, "An error occurred while retrieving communications.");
            }
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAllCommunications()
        {
            try
            {
                var communications = await _service.ReviewCommunicationsAsync();
                return Ok(communications);
            }
            catch (Exception)
            {
                return StatusCode(500, "An error occurred while retrieving communications.");
            }
        }
    }
}