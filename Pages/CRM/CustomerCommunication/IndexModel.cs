using Cloud9_2.Data;
using Cloud9_2.Models;
using Cloud9_2.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Cloud9_2.Pages.CRM.CustomerCommunication
{
    public class IndexModel : PageModel
    {
        private readonly CustomerCommunicationService _communicationService;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<IndexModel> _logger;
        private readonly UserManager<ApplicationUser> _userManager;

        public IndexModel(
            CustomerCommunicationService communicationService,
            ApplicationDbContext context,
            ILogger<IndexModel> logger,
            UserManager<ApplicationUser> userManager)
        {
            _communicationService = communicationService ?? throw new ArgumentNullException(nameof(communicationService));
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
        }

        public IList<CustomerCommunicationDto> Communications { get; set; } = new List<CustomerCommunicationDto>();
        public Dictionary<string, string> StatusDisplayNames { get; set; } = new Dictionary<string, string>();
        [BindProperty(SupportsGet = true)]
        public string? SearchTerm { get; set; }
        [BindProperty(SupportsGet = true)]
        public string TypeFilter { get; set; }
        [BindProperty(SupportsGet = true)]
        public string SortBy { get; set; }
        [BindProperty(SupportsGet = true)]
        public int CurrentPage { get; set; } = 1;
        [BindProperty(SupportsGet = true)]
        public int PageSize { get; set; } = 10;
        public int TotalPages { get; set; }
        public int TotalRecords { get; set; }
        public int DistinctCommunicationIdCount { get; set; }

        [BindProperty]
        public CustomerCommunicationDto NewCommunication { get; set; }
        [BindProperty]
        public CommunicationPostDto NewPost { get; set; }
        [BindProperty]
        public string NewResponsibleId { get; set; }
        [BindProperty]
        public int CommunicationId { get; set; }

        public async Task OnGetAsync()
        {
            try
            {
                _logger.LogInformation("Loading Communications page with parameters: SearchTerm={SearchTerm}, TypeFilter={TypeFilter}, SortBy={SortBy}, CurrentPage={CurrentPage}, PageSize={PageSize}",
                    SearchTerm, TypeFilter, SortBy, CurrentPage, PageSize);

                CurrentPage = Math.Max(1, CurrentPage);

                // Fetch communications
                var allCommunications = await _communicationService.ReviewCommunicationsAsync();

                // Apply filtering
                var query = allCommunications.AsQueryable();
                if (!string.IsNullOrWhiteSpace(SearchTerm))
                {
                    SearchTerm = SearchTerm.Trim().ToLower();
                    query = query.Where(c =>
                        (c.Subject != null && c.Subject.ToLower().Contains(SearchTerm)) ||
                        (c.FirstName != null && c.FirstName.ToLower().Contains(SearchTerm)) ||
                        (c.LastName != null && c.LastName.ToLower().Contains(SearchTerm)) ||
                        (c.CommunicationTypeName != null && c.CommunicationTypeName.ToLower().Contains(SearchTerm)) ||
                        (c.Note != null && c.Note.ToLower().Contains(SearchTerm)));
                }

                if (!string.IsNullOrWhiteSpace(TypeFilter) && TypeFilter != "all")
                {
                    query = query.Where(c => c.CommunicationTypeName == TypeFilter);
                }

                TotalRecords = query.Count();
                DistinctCommunicationIdCount = query.Select(c => c.CustomerCommunicationId).Distinct().Count();

                TotalPages = (int)Math.Ceiling((double)TotalRecords / PageSize);
                CurrentPage = Math.Min(CurrentPage, TotalPages > 0 ? TotalPages : 1);

                // Apply sorting
                query = SortBy switch
                {
                    "CommunicationId" => query.OrderByDescending(c => c.CustomerCommunicationId),
                    "PartnerName" => query.OrderBy(c => (c.FirstName ?? "") + " " + (c.LastName ?? "")),
                    "CommunicationDate" => query.OrderByDescending(c => c.Date),
                    "Subject" => query.OrderBy(c => c.Subject),
                    _ => query.OrderByDescending(c => c.CustomerCommunicationId)
                };

                // Apply pagination
                var skip = (CurrentPage - 1) * PageSize;
                Communications = query.Skip(skip).Take(PageSize).ToList();

                // Dynamically populate StatusDisplayNames from CommunicationStatuses
                StatusDisplayNames = await _context.CommunicationStatuses
                    .AsNoTracking()
                    .ToDictionaryAsync(s => s.Name, s => s.Name); // Use Name as both key and value

                _logger.LogInformation("Successfully retrieved {Count} communications for page {CurrentPage}", Communications.Count, CurrentPage);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving communications: {Message}", ex.Message);
                Communications = new List<CustomerCommunicationDto>();
                TotalRecords = 0;
                TotalPages = 1;
                DistinctCommunicationIdCount = 0;
                ModelState.AddModelError("", "Error retrieving data. Please try again later.");
            }
        }

        // Existing OnPost methods remain unchanged
        public async Task<IActionResult> OnPostCreateAsync()
        {
            if (!ModelState.IsValid)
            {
                await OnGetAsync();
                return Page();
            }

            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    ModelState.AddModelError(string.Empty, "User not authenticated");
                    await OnGetAsync();
                    return Page();
                }

                NewCommunication.AgentId = userId;
                NewCommunication.Date = NewCommunication.Date != default ? NewCommunication.Date : DateTime.UtcNow;

                await _communicationService.RecordCommunicationAsync(NewCommunication, "General");
                _logger.LogInformation("Communication created successfully with ID {CommunicationId}", NewCommunication.CustomerCommunicationId);
            }
            catch (ArgumentException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                _logger.LogWarning("Failed to create communication: {Message}", ex.Message);
                await OnGetAsync();
                return Page();
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, "An error occurred while creating the communication.");
                _logger.LogError(ex, "Error creating communication");
                await OnGetAsync();
                return Page();
            }

            return RedirectToPage(new { SearchTerm, TypeFilter, SortBy, CurrentPage, PageSize });
        }

        public async Task<IActionResult> OnPostUpdateAsync()
        {
            if (!ModelState.IsValid)
            {
                await OnGetAsync();
                return Page();
            }

            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    ModelState.AddModelError(string.Empty, "User not authenticated");
                    await OnGetAsync();
                    return Page();
                }

                NewCommunication.AgentId = userId;
                await _communicationService.UpdateCommunicationAsync(NewCommunication);
                _logger.LogInformation("Communication updated successfully with ID {CommunicationId}", NewCommunication.CustomerCommunicationId);
            }
            catch (ArgumentException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                _logger.LogWarning("Failed to update communication: {Message}", ex.Message);
                await OnGetAsync();
                return Page();
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, "An error occurred while updating the communication.");
                _logger.LogError(ex, "Error updating communication with ID {CommunicationId}", NewCommunication.CustomerCommunicationId);
                await OnGetAsync();
                return Page();
            }

            return RedirectToPage(new { SearchTerm, TypeFilter, SortBy, CurrentPage, PageSize });
        }

        public async Task<IActionResult> OnPostDeleteAsync(int communicationId)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    ModelState.AddModelError(string.Empty, "User not authenticated");
                    await OnGetAsync();
                    return Page();
                }

                await _communicationService.DeleteCommunicationAsync(communicationId);
                _logger.LogInformation("Communication deleted successfully with ID {CommunicationId}", communicationId);
            }
            catch (ArgumentException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                _logger.LogWarning("Failed to delete communication: {Message}", ex.Message);
                await OnGetAsync();
                return Page();
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                _logger.LogWarning("Failed to delete communication: {Message}", ex.Message);
                await OnGetAsync();
                return Page();
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, "An error occurred while deleting the communication.");
                _logger.LogError(ex, "Error deleting communication with ID {CommunicationId}", communicationId);
                await OnGetAsync();
                return Page();
            }

            return RedirectToPage(new { SearchTerm, TypeFilter, SortBy, CurrentPage, PageSize });
        }

        public async Task<IActionResult> OnPostAddPostAsync(int communicationId, string content)
        {
            if (string.IsNullOrWhiteSpace(content))
                return BadRequest("Content is required");

            try
            {
                await _communicationService.AddCommunicationPostAsync(communicationId, content, User.Identity.Name);
                return new JsonResult(new { success = true });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An unexpected error occurred while adding the post." });
            }
        }

        public async Task<IActionResult> OnPostChangeResponsibleAsync()
        {
            if (!ModelState.IsValid)
            {
                await OnGetAsync();
                return Page();
            }

            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    ModelState.AddModelError(string.Empty, "User not authenticated");
                    await OnGetAsync();
                    return Page();
                }

                await _communicationService.AssignResponsibleAsync(
                    CommunicationId,
                    NewResponsibleId,
                    userId
                );

                _logger.LogInformation("Responsible assigned successfully for communication ID {CommunicationId}", CommunicationId);
            }
            catch (ArgumentException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                _logger.LogWarning("Failed to assign responsible: {Message}", ex.Message);
                await OnGetAsync();
                return Page();
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, "An error occurred while assigning the responsible.");
                _logger.LogError(ex, "Error assigning responsible for communication ID {CommunicationId}", CommunicationId);
                await OnGetAsync();
                return Page();
            }

            return RedirectToPage(new { SearchTerm, TypeFilter, SortBy, CurrentPage, PageSize });
        }

        public async Task<IActionResult> OnPostAssignResponsibleAsync()
        {
            if (!ModelState.IsValid)
                return Page();

            try
            {
                var userId = _userManager.GetUserId(User);
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized();

                await _communicationService.AssignResponsibleAsync(
                    CommunicationId,
                    NewResponsibleId,
                    userId
                );

                return new JsonResult(new { success = true, message = "Felelős sikeresen kijelölve!" });
            }
            catch (ArgumentException ex)
            {
                return new JsonResult(new { success = false, error = ex.Message });
            }
            catch (Exception ex)
            {
                return new JsonResult(new { success = false, error = "Hiba történt a felelős kijelölésekor." });
            }
        }

        public async Task<IActionResult> OnPostAssignAnotherResponsibleAsync()
        {
            if (!ModelState.IsValid)
                return Page();

            try
            {
                var userId = _userManager.GetUserId(User);
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized();

                await _communicationService.AssignResponsibleAsync(
                    CommunicationId,
                    NewResponsibleId,
                    userId
                );

                return new JsonResult(new { success = true, message = "Felelős sikeresen kijelölve!" });
            }
            catch (ArgumentException ex)
            {
                return new JsonResult(new { success = false, error = ex.Message });
            }
            catch (Exception ex)
            {
                return new JsonResult(new { success = false, error = "Hiba történt a felelős kijelölésekor." });
            }
        }
    }
}