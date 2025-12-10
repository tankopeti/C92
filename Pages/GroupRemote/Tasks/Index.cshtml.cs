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
    [BindProperty(SupportsGet = true, Name = "pageNumber")]
    public int CurrentPage { get; set; } = 1;

    [BindProperty(SupportsGet = true)]
    public int PageSize { get; set; } = 10;

    [BindProperty(SupportsGet = true)]
    public string? SearchTerm { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Sort { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Order { get; set; } = "desc";

    [BindProperty(SupportsGet = true)]
    public int? StatusId { get; set; }

    [BindProperty(SupportsGet = true)]
        public int? PriorityId { get; set; }
    // === ÚJ SZŰRŐK ===
[BindProperty(SupportsGet = true)] public int? TaskTypeId { get; set; }
[BindProperty(SupportsGet = true)] public int? PartnerId { get; set; }
[BindProperty(SupportsGet = true)] public int? SiteId { get; set; }
[BindProperty(SupportsGet = true)] public string? AssignedToId { get; set; }

[BindProperty(SupportsGet = true)] public DateTime? DueDateFrom { get; set; }
[BindProperty(SupportsGet = true)] public DateTime? DueDateTo { get; set; }
[BindProperty(SupportsGet = true)] public DateTime? CreatedDateFrom { get; set; }
[BindProperty(SupportsGet = true)] public DateTime? CreatedDateTo { get; set; }

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
            assignedToId: null
        );
    }
}}