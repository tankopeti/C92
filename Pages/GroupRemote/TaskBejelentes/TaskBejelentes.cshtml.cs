using Cloud9_2.Data;
using Cloud9_2.Models;
using Cloud9_2.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;

namespace Cloud9_2.Pages.GroupRemote.TaskBejelentes
{
    public class TaskBejelentesModel : PageModel
    {
        private readonly TaskPMService _taskService;
        private readonly TwilioSettings _twilioSettings;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<TaskBejelentesModel> _logger;

        public TaskBejelentesModel(
            TaskPMService taskService,
            IOptions<TwilioSettings> twilioSettings,
            ApplicationDbContext context,
            ILogger<TaskBejelentesModel> logger)
        {
            _taskService = taskService;
            _twilioSettings = twilioSettings.Value;
            _context = context;
            _logger = logger;
        }

        [BindProperty(SupportsGet = true)]
    public int? StatusId { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? PriorityId { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? TaskTypeId { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? PartnerId { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? SiteId { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? AssignedToId { get; set; }

    [BindProperty(SupportsGet = true)]
    public DateTime? DueDateFrom { get; set; }

    [BindProperty(SupportsGet = true)]
    public DateTime? DueDateTo { get; set; }

    [BindProperty(SupportsGet = true)]
    public DateTime? CreatedDateFrom { get; set; }

    [BindProperty(SupportsGet = true)]
    public DateTime? CreatedDateTo { get; set; }

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
        public string? Order { get; set; } = "desc";

        // === SMS POST Data ===
        [BindProperty]
        public int TaskId { get; set; }

        [BindProperty]
        public string Message { get; set; } = string.Empty;

        // === View Data ===
        public PagedResult<TaskPMDto> TaskBejelentes { get; set; } = new();
        public int TotalPages => TaskBejelentes.TotalPages;
        public int TotalRecords => TaskBejelentes.TotalCount;

        // Státuszok és prioritások a gyors módosító modalokhoz
        public List<TaskStatusPM> Statuses { get; set; } = new();
        public List<TaskPriorityPM> Priorities { get; set; } = new();

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
        taskTypeId: TaskTypeId,
        partnerId: PartnerId,
        siteId: SiteId,
        assignedToId: AssignedToId,
        dueDateFrom: DueDateFrom,
        dueDateTo: DueDateTo,
        createdDateFrom: CreatedDateFrom,
        createdDateTo: CreatedDateTo
    );

    // --- Gyors módosító modalokhoz (maradnak, mert ott Model.Statuses-t használsz) ---
    Statuses = await _context.TaskStatusesPM
        .OrderBy(s => s.Name)
        .ToListAsync();

    Priorities = await _context.TaskPrioritiesPM
        .Where(p => p.IsActive == true)
        .OrderBy(p => p.DisplayOrder ?? 999)
        .ThenBy(p => p.Name)
        .ToListAsync();

    // --- ÚJ: Összetett szűrő + új feladat modalhoz ViewData feltöltése ---
    ViewData["Statuses"] = Statuses; // újrahasznosítjuk a már lekérdezettet

    ViewData["Priorities"] = Priorities; // ugyanígy

    ViewData["Types"] = await _context.TaskTypePMs
        .OrderBy(t => t.TaskTypePMName)
        .ToListAsync();

    ViewData["Partners"] = await _context.Partners
        .OrderBy(p => p.CompanyName ?? p.Name ?? p.NameTrim ?? "")
        .ToListAsync();



    // Sites nem kell ViewData-ba, mert TomSelect remote load-dal működik mindkét modalban
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