using Cloud9_2.Models;
using Cloud9_2.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using Cloud9_2.Data;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Cloud9_2.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class CustomerCommunicationController : ControllerBase
    {
        private readonly CustomerCommunicationService _service;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;

        public CustomerCommunicationController(CustomerCommunicationService service, UserManager<ApplicationUser> userManager)
        {
            _service = service;
            _userManager = userManager;
        }

        [HttpPost]
        public async Task<IActionResult> RecordCommunication([FromBody] CustomerCommunicationDto dto, [FromQuery] string communicationPurpose = "General")
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var userId = _userManager.GetUserId(User);
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized("User not found.");

                dto.AgentId = userId;
                dto.Date = dto.Date != default ? dto.Date : DateTime.UtcNow;

                await _service.RecordCommunicationAsync(dto, communicationPurpose);
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

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateCommunication(int id, [FromBody] CustomerCommunicationDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (id != dto.CustomerCommunicationId)
                return BadRequest("Communication ID mismatch.");

            try
            {
                var userId = _userManager.GetUserId(User);
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized("User not found.");

                dto.AgentId = userId;
                await _service.UpdateCommunicationAsync(dto);
                return Ok(new { communicationId = dto.CustomerCommunicationId });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception)
            {
                return StatusCode(500, "An error occurred while updating the communication.");
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCommunication(int id)
        {
            try
            {
                var userId = _userManager.GetUserId(User);
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized("User not found.");

                await _service.DeleteCommunicationAsync(id);
                return NoContent();
            }
            catch (ArgumentException ex)
            {
                return NotFound(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception)
            {
                return StatusCode(500, "An error occurred while deleting the communication.");
            }
        }

        [HttpPost("{id}/post")]
        public async Task<IActionResult> AddPost(int id, [FromBody] PostDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var userId = _userManager.GetUserId(User);
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized("User not found.");

                await _service.AddCommunicationPostAsync(id, dto.Content, userId);
                return Ok();
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                // Log exception (use ILogger in production)
                return StatusCode(500, new { error = "An unexpected error occurred while adding the post." });
            }
        }

        [HttpPost("{id}/assign-responsible")]
        public async Task<IActionResult> AssignResponsible(int id, [FromBody] AssignResponsibleDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var userId = _userManager.GetUserId(User);
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized("User not found.");

                await _service.AssignResponsibleAsync(id, dto.ResponsibleUserId, userId);
                return Ok();
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An unexpected error occurred while assigning the responsible." });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetUsers()
        {
            var licensedUsers = await _userManager.GetUsersInRoleAsync("Admin");
            var users = licensedUsers.Select(u => new
            {
                id = u.Id,
                text = u.UserName
            });

            return Ok(users);
        }

[HttpGet("{id}/history")]
        public async Task<IActionResult> GetCommunicationHistory(int id)
        {
            try
            {
                var communication = await _service.GetCommunicationHistoryAsync(id);
                return Ok(communication);
            }
            catch (ArgumentException ex)
            {
                return NotFound(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An unexpected error occurred.", details = ex.Message });
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
            catch (Exception ex)
            {
                // Log exception
                return StatusCode(500, new { error = "An error occurred while retrieving the communications." });
            }
        }
    }

    public class PostDto
    {
        public string Content { get; set; }
    }

    public class AssignResponsibleDto
    {
        public string ResponsibleUserId { get; set; }
    }
}