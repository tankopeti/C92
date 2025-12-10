using Cloud9_2.Models;
using Cloud9_2.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;

namespace Cloud9_2.Pages.GroupRemote.TaskBejelentes
{
    public class TaskBejelentesModel : PageModel
    {
        private readonly TaskPMService _taskService;
        private readonly TwilioSettings _twilioSettings;

        public TaskBejelentesModel(
            TaskPMService taskService,
            IOptions<TwilioSettings> twilioSettings)
        {
            _taskService = taskService;
            _twilioSettings = twilioSettings.Value;
        }

        // === Query Parameters ===
        [BindProperty(SupportsGet = true, Name = "pageNumber")]
        public int CurrentPage { get; set; } = 1;

        [BindProperty(SupportsGet = true)]
        public int PageSize { get; set; } = 20;

        [BindProperty(SupportsGet = true)]
        public string? SearchTerm { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? Sort { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? Order { get; set; } = "desc";   // ← FIXED: newest first!

        [BindProperty(SupportsGet = true)]
        public int? StatusId { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? PriorityId { get; set; }

        // === SMS POST Data ===
        [BindProperty]
        public int TaskId { get; set; }

        [BindProperty]
        public string Message { get; set; } = string.Empty;

        // === View Data ===
        public PagedResult<TaskPMDto> TaskBejelentes { get; set; } = new();
        public int TotalPages => TaskBejelentes.TotalPages;
        public int TotalRecords => TaskBejelentes.TotalCount;

        public async Task OnGetAsync()
        {
            TaskBejelentes = await _taskService.GetPagedTasksAsync(
                page: CurrentPage,
                pageSize: PageSize,
                searchTerm: SearchTerm,
                sort: Sort,
                order: Order,
                statusId: StatusId,
                priorityId: PriorityId,
                assignedToId: null
            );
        }

        public async Task<IActionResult> OnPostSendSmsAsync()
        {
            try
            {
                var body = string.IsNullOrWhiteSpace(Message)
                    ? "Új feladat bejelentve a Cloud9 rendszerben!"
                    : Message;

                var message = await MessageResource.CreateAsync(
                    body: body,
                    from: new PhoneNumber(_twilioSettings.PhoneNumber),
                    to: new PhoneNumber("+36707737490")
                );

                TempData["Success"] = $"SMS elküldve! (#{message.Sid.Substring(0, 10)}...)";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"SMS hiba: {ex.Message}";
            }

            // ← PRESERVE ALL FILTERS & PAGINATION
            return RedirectToPage(new
            {
                CurrentPage,
                PageSize,
                SearchTerm,
                Sort,
                Order,
                StatusId,
                PriorityId
            });
        }
    }
}