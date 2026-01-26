using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Cloud9_2.Data;
using Cloud9_2.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Cloud9_2.Pages.CRM.Partners
{
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<IndexModel> _logger;

        public IndexModel(ApplicationDbContext context, ILogger<IndexModel> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        // A cshtml ezt használja a breadcrumbban – JS fogja felülírni később, de kell, hogy ne törjön
        public IList<Partner> Partners { get; set; } = new List<Partner>();

        public int TotalRecords { get; set; } = 0;

        [BindProperty(SupportsGet = true)]
        public int CurrentPage { get; set; } = 1;

        [BindProperty(SupportsGet = true)]
        public int PageSize { get; set; } = 50;

        public int TotalPages => PageSize > 0
            ? (int)Math.Ceiling((double)TotalRecords / PageSize)
            : 0;

        [BindProperty(SupportsGet = true)]
        public string? SearchTerm { get; set; }

        // Modalokhoz hasznos lehet (ha a JS nem külön endpointból tölti)
        public IList<Status> AvailableStatuses { get; set; } = new List<Status>();

        public async Task<IActionResult> OnGetAsync()
        {
            // Ha a controller intézi az auth-ot és ide csak bejutott user jön,
            // itt nem csinálunk semmit. (Ha nem: tedd az oldalt [Authorize]-al)
            // A page paramétereket csak "stabilizáljuk".
            if (CurrentPage < 1) CurrentPage = 1;
            if (PageSize < 10) PageSize = 50;
            if (PageSize > 200) PageSize = 200;

            // A táblát a JS tölti: Partners / TotalRecords marad 0.
            // Státuszok: ha a loadStatuses.js úgyis API-ról tölti, ezt akár ki is veheted.
            try
            {
                AvailableStatuses = await _context.PartnerStatuses
                    .AsNoTracking()
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load PartnerStatuses for Partners page.");
                AvailableStatuses = new List<Status>();
            }

            return Page();
        }
    }
}
