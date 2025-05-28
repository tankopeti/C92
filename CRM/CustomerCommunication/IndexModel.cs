using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Cloud9_2.Data;
using Cloud9_2.Models;
using Microsoft.Extensions.Logging;

namespace Cloud9_2.Pages.CRM.CustomerCommunication
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

        public IList<CustomerCommunicationDto> Communications { get; set; } = new List<CustomerCommunicationDto>();
        public Dictionary<string, string> StatusDisplayNames { get; set; }
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

        public async Task OnGetAsync()
        {
            try
            {
                _logger.LogInformation("Loading Communications page with parameters: SearchTerm={SearchTerm}, TypeFilter={TypeFilter}, SortBy={SortBy}, CurrentPage={CurrentPage}, PageSize={PageSize}",
                    SearchTerm, TypeFilter, SortBy, CurrentPage, PageSize);

                CurrentPage = Math.Max(1, CurrentPage);

                var query = _context.CustomerCommunications
                    .Include(c => c.CommunicationType)
                    .Include(c => c.Contact)
                    .Include(c => c.Status)
                    .Include(c => c.Agent)
                    .AsQueryable();

                if (!string.IsNullOrWhiteSpace(SearchTerm))
                {
                    SearchTerm = SearchTerm.Trim().ToLower();
                    query = query.Where(c =>
                        (c.Subject != null && c.Subject.ToLower().Contains(SearchTerm)) ||
                        (c.Contact != null && c.Contact.FirstName != null && c.Contact.FirstName.ToLower().Contains(SearchTerm)) ||
                        (c.Contact != null && c.Contact.LastName != null && c.Contact.LastName.ToLower().Contains(SearchTerm)) ||
                        (c.CommunicationType != null && c.CommunicationType.Name != null && c.CommunicationType.Name.ToLower().Contains(SearchTerm)) ||
                        (c.Note != null && c.Note.ToLower().Contains(SearchTerm)));
                }

                if (!string.IsNullOrWhiteSpace(TypeFilter) && TypeFilter != "all")
                {
                    query = query.Where(c => c.CommunicationType != null && c.CommunicationType.Name == TypeFilter);
                }

                TotalRecords = await query.CountAsync();
                DistinctCommunicationIdCount = await query.Select(c => c.CustomerCommunicationId).Distinct().CountAsync();

                TotalPages = (int)Math.Ceiling((double)TotalRecords / PageSize);
                CurrentPage = Math.Min(CurrentPage, TotalPages > 0 ? TotalPages : 1);

                query = SortBy switch
                {
                    "CommunicationId" => query.OrderByDescending(c => c.CustomerCommunicationId),
                    "PartnerName" => query.OrderBy(c => c.Contact != null ? c.Contact.FirstName + " " + c.Contact.LastName : ""),
                    "CommunicationDate" => query.OrderByDescending(c => c.Date),
                    "Subject" => query.OrderBy(c => c.Subject),
                    _ => query.OrderByDescending(c => c.Date)
                };

                var skip = (CurrentPage - 1) * PageSize;
                query = query.Skip(skip).Take(PageSize);

                Communications = await query
                    .Select(c => new CustomerCommunicationDto
                    {
                        CustomerCommunicationId = c.CustomerCommunicationId,
                        CommunicationTypeId = c.CommunicationTypeId,
                        CommunicationTypeName = c.CommunicationType != null ? c.CommunicationType.Name : null,
                        Date = c.Date,
                        Subject = c.Subject,
                        Note = c.Note,
                        ContactId = c.ContactId,
                        FirstName = c.Contact != null ? c.Contact.FirstName : null,
                        LastName = c.Contact != null ? c.Contact.LastName : null,
                        AgentId = c.AgentId,
                        AgentName = c.Agent != null ? c.Agent.UserName : null, // Use UserName or other property from ApplicationUser
                        StatusId = c.StatusId,
                        StatusName = c.Status != null ? c.Status.Name : null,
                        AttachmentPath = c.AttachmentPath,
                        Metadata = c.Metadata,
                        PartnerId = c.PartnerId,
                        LeadId = c.LeadId,
                        QuoteId = c.QuoteId,
                        OrderId = c.OrderId
                    })
                    .ToListAsync();

                    // Populate StatusDisplayNames from database
            StatusDisplayNames = await _context.CommunicationStatuses
                .ToDictionaryAsync(
                    s => s.Name,
                    s => s.Name switch
                    {
                        "Escalated" => "Eskalálva",
                        "InProgress" => "Folyamatban",
                        "Open" => "Nyitott",
                        "Resolved" => "Megoldva",
                        _ => s.Name
                    }
                );

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
    }
}