using Cloud9_2.Models;
using Cloud9_2.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Cloud9_2.Pages.GroupRemote.Tasks
{
    public class IndexModel : PageModel
    {
        private readonly TaskPMService _taskService;

        public IndexModel(TaskPMService taskService)
        {
            _taskService = taskService;
        }

        // === Query Parameters ===
        [BindProperty(SupportsGet = true)]
        public int CurrentPage { get; set; } = 1;

        [BindProperty(SupportsGet = true)]
        public int PageSize { get; set; } = 20;

        [BindProperty(SupportsGet = true)]
        public string? SearchTerm { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? Sort { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? Order { get; set; } = "asc";

        [BindProperty(SupportsGet = true)]
        public int? StatusId { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? PriorityId { get; set; }

        [BindProperty(SupportsGet = true)]
        // public int? ProjectPMId { get; set; }

        // === View Data ===
        public PagedResult<TaskPMDto> Tasks { get; set; } = new();
        public int TotalPages => Tasks.TotalPages;
        public int TotalRecords => Tasks.TotalCount;

        public async Task OnGetAsync()
        {
            Tasks = await _taskService.GetPagedTasksAsync(
                page: CurrentPage,
                pageSize: PageSize,
                searchTerm: SearchTerm,
                sort: Sort,
                order: Order,
                statusId: StatusId,
                priorityId: PriorityId,
                // projectPMId: ProjectPMId,
                assignedToId: null
            );
        }
    }
}