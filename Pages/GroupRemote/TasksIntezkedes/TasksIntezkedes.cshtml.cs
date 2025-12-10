using Cloud9_2.Models;
using Cloud9_2.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;

namespace Cloud9_2.Pages.GroupRemote.TasksIntezkedes
{
    public class TasksIntezkedesModel : PageModel
    {
        private readonly TaskPMService _taskService;
        private readonly TwilioSettings _twilioSettings;

        public TasksIntezkedesModel(
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
        [BindProperty]  // automatically binds from form POST
        public int TaskId { get; set; }

        [BindProperty]
        public string Message { get; set; } = string.Empty;

        // === View Data ===
        public PagedResult<TaskPMDto> TasksIntezkedes { get; set; } = new();
        public int TotalPages => TasksIntezkedes.TotalPages;
        public int TotalRecords => TasksIntezkedes.TotalCount;

        public async Task OnGetAsync()
        {
            TasksIntezkedes = await _taskService.GetPagedTasksAsync(
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
            // Optional: validate TaskId exists
            var task = await _taskService.GetTaskByIdAsync(TaskId);
            if (task == null)
            {
                TempData["Error"] = "Task not found.";
                return RedirectToPage();
            }

            try
            {
                var body = string.IsNullOrWhiteSpace(Message)
                    ? $"Intézkedés szükséges: #{TaskId} – {task.Title}"
                    : Message;

                var message = await MessageResource.CreateAsync(
                    body: body,
                    from: new PhoneNumber(_twilioSettings.PhoneNumber),
                    to: new PhoneNumber("+36707737490") // Make this dynamic later!
                );

                TempData["Success"] = $"SMS elküldve! ({message.Sid})";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"SMS hiba: {ex.Message}";
            }

            return RedirectToPage(new { CurrentPage, PageSize, SearchTerm, Sort, Order });
        }
    }
}