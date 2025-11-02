// File: Pages/GroupRemote/Resources/Index.cshtml.cs
using Cloud9_2.Models;
using Cloud9_2.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Collections.Generic;

namespace Cloud9_2.Pages.GroupRemote.Resources
{
    public class IndexModel : PageModel
    {
        private readonly ResourceService _service;

        public IndexModel(ResourceService service) => _service = service;

        public IEnumerable<ResourceDto> Resources { get; set; } = new List<ResourceDto>();
        public int CurrentPage { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public int TotalRecords { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalRecords / PageSize);
        public string? SearchTerm { get; set; }
        public string? Sort { get; set; }
        public string? Order { get; set; }

        [BindProperty]
        public CreateResourceDto CreateDto { get; set; } = new();

        [BindProperty]
        public UpdateResourceDto? UpdateDto { get; set; } // Nullable to fix CS8603

        public async Task OnGetAsync(int? pageNumber, int? pageSize, string? searchTerm, string? sort, string? order)
        {
            CurrentPage = pageNumber ?? 1;
            PageSize = pageSize ?? 20;
            SearchTerm = searchTerm;
            Sort = sort;
            Order = order;

            var result = await _service.GetPagedResourcesAsync(CurrentPage, PageSize, SearchTerm, Sort, Order);
            Resources = result.Items ?? new List<ResourceDto>();
            TotalRecords = result.TotalCount;
        }

        public async Task<IActionResult> OnPostCreateAsync()
        {
            if (!ModelState.IsValid) return Page();
            await _service.CreateResourceAsync(CreateDto, User.Identity?.Name ?? "system");
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostUpdateAsync()
        {
            if (!ModelState.IsValid || UpdateDto == null) return Page();
            await _service.UpdateResourceAsync(UpdateDto, User.Identity?.Name ?? "system");
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            await _service.DeactivateResourceAsync(id, User.Identity?.Name ?? "system");
            return RedirectToPage();
        }

        public async Task<IActionResult> OnGetEditAsync(int id)
        {
            var res = await _service.GetResourceByIdAsync(id);
            if (res == null) return NotFound();

            UpdateDto = new UpdateResourceDto
            {
                ResourceId = res.ResourceId,
                Name = res.Name ?? string.Empty,
                Serial = res.Serial,
                ResourceTypeId = res.ResourceTypeId,
                ResourceStatusId = res.ResourceStatusId,
                Price = res.Price,
                DateOfPurchase = res.DateOfPurchase,
                Comment1 = res.Comment1
            };

            return Partial("_EditForm", UpdateDto);
        }
    }
}