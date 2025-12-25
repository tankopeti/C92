using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Cloud9_2.Models;
using Cloud9_2.Data;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.AspNetCore.Identity;
using System;
using Microsoft.Extensions.Logging;
using Cloud9_2.Services;

namespace Cloud9_2.Pages.CRM.Leads
{
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<IndexModel> _logger;
        private readonly LeadService _leadService;

        public IndexModel(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            ILogger<IndexModel> logger,
            LeadService leadService)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
            _leadService = leadService;
        }

        public IList<Lead> Leads { get; set; } = new List<Lead>();
        public int CurrentPage { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public int TotalRecords { get; set; }
        public int TotalPages { get; set; }
        public string SearchTerm { get; set; } = string.Empty;

        public async Task OnGetAsync(int pageNumber = 1, int pageSize = 10, string searchTerm = "")
        {
            CurrentPage = pageNumber;
            PageSize = pageSize;
            SearchTerm = searchTerm;

            IQueryable<Lead> leadsQuery = _context.Leads
                .Where(l => l.IsActive)
                .Include(l => l.LeadHistories)
                .AsQueryable();

            if (!string.IsNullOrEmpty(searchTerm))
            {
                leadsQuery = leadsQuery.Where(l =>
                    (l.FirstName != null && l.FirstName.Contains(searchTerm)) ||
                    (l.LastName != null && l.LastName.Contains(searchTerm)) ||
                    (l.Email != null && l.Email.Contains(searchTerm)) ||
                    (l.CompanyName != null && l.CompanyName.Contains(searchTerm)) ||
                    (l.PhoneNumber != null && l.PhoneNumber.Contains(searchTerm)));
            }

            TotalRecords = await leadsQuery.CountAsync();
            TotalPages = (int)Math.Ceiling(TotalRecords / (double)pageSize);

            Leads = await leadsQuery
                .OrderByDescending(l => l.CreatedDate)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<IActionResult> OnPostCreateLeadAsync(
            string firstName, string lastName, string email,
            string phoneNumber, string companyName, string jobTitle,
            string status, string notes)
        {
            var user = await _userManager.GetUserAsync(User);
            var lead = new Lead
            {
                FirstName = firstName,
                LastName = lastName,
                Email = email,
                PhoneNumber = phoneNumber,
                CompanyName = companyName,
                JobTitle = jobTitle,
                Status = status,
                Notes = notes
            };

            try
            {
                await _leadService.CreateLeadAsync(lead, user?.Id);
                TempData["SuccessMessage"] = "Lead created successfully!";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating lead");
                TempData["ErrorMessage"] = "Failed to create lead. Please try again.";
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostEditLeadAsync(
            int leadId, string firstName, string lastName, string email,
            string phoneNumber, string companyName, string jobTitle,
            string status, string notes, int? partnerId)
        {
            var user = await _userManager.GetUserAsync(User);
            var lead = new Lead
            {
                LeadId = leadId,
                FirstName = firstName,
                LastName = lastName,
                Email = email,
                PhoneNumber = phoneNumber,
                CompanyName = companyName,
                JobTitle = jobTitle,
                Status = status,
                Notes = notes,
                PartnerId = partnerId
            };

            try
            {
                await _leadService.UpdateLeadAsync(lead, user?.Id);
                TempData["SuccessMessage"] = "Lead updated successfully!";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating lead with ID {LeadId}", leadId);
                TempData["ErrorMessage"] = "Failed to update lead. Please try again.";
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostDeleteLeadAsync(int leadId)
        {
            var userId = (await _userManager.GetUserAsync(User))?.Id ?? "Unknown";

            try
            {
                var success = await _leadService.DeleteLeadAsync(leadId, userId);

                if (!success)
                {
                    TempData["ErrorMessage"] = "Lead not found or already deleted.";
                }
                else
                {
                    TempData["SuccessMessage"] = "Lead has been deleted successfully.";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "An error occurred while deleting the lead.";
            }

            return RedirectToPage();
        }




        public async Task<IActionResult> OnPostCreatePartnerAsync(
            int? leadId, string name, string email, string phoneNumber, string alternatePhone,
            string website, string companyName, string taxId, string intTaxId, string industry,
            string addressLine1, string addressLine2, string city, string state, string postalCode,
            string country, string status, DateTime? lastContacted, string notes, string assignedTo,
            string billingContactName, string billingEmail, string paymentTerms, decimal? creditLimit,
            string preferredCurrency, bool isTaxExempt, DateTime createdDate, string createdBy)
        {
            if (string.IsNullOrEmpty(name))
            {
                TempData["ErrorMessage"] = "Partner name is required!";
                return RedirectToPage();
            }

            // Map status string to StatusId
            int? statusId = null;
            if (!string.IsNullOrEmpty(status))
            {
                var statusEntity = await _context.PartnerStatuses
                    .FirstOrDefaultAsync(s => s.Name == status);
                if (statusEntity == null)
                {
                    TempData["ErrorMessage"] = $"Invalid status: {status}. Valid statuses are Active, Inactive, or Prospect.";
                    return RedirectToPage();
                }
                statusId = statusEntity.Id;
            }
            else
            {
                // Default to "Prospect" (Id = 3)
                var prospectStatus = await _context.PartnerStatuses
                    .FirstOrDefaultAsync(s => s.Name == "Prospect");
                statusId = prospectStatus?.Id ?? 3; // Fallback to 3 if not found
            }

            var user = await _userManager.GetUserAsync(User);
            var partner = new Partner
            {
                Name = name,
                Email = email,
                PhoneNumber = phoneNumber,
                AlternatePhone = alternatePhone,
                Website = website,
                CompanyName = companyName,
                TaxId = taxId,
                IntTaxId = intTaxId,
                Industry = industry,
                AddressLine1 = addressLine1,
                AddressLine2 = addressLine2,
                City = city,
                State = state,
                PostalCode = postalCode,
                Country = country,
                StatusId = statusId,
                LastContacted = lastContacted,
                Notes = notes,
                AssignedTo = assignedTo,
                BillingContactName = billingContactName,
                BillingEmail = billingEmail,
                PaymentTerms = paymentTerms,
                CreditLimit = creditLimit,
                PreferredCurrency = preferredCurrency,
                IsTaxExempt = isTaxExempt,
                CreatedDate = createdDate,
                CreatedBy = user?.Id ?? createdBy,
                UpdatedDate = DateTime.UtcNow,
                UpdatedBy = user?.Id
            };

            try
            {
                _context.Partners.Add(partner);
                await _context.SaveChangesAsync();

                if (leadId.HasValue)
                {
                    var lead = await _context.Leads.FindAsync(leadId.Value);
                    if (lead != null)
                    {
                        lead.PartnerId = partner.PartnerId;
                        lead.Status = "Qualified";
                        await _leadService.UpdateLeadAsync(lead, user?.Id);
                        await _leadService.LogHistoryAsync(lead, user?.Id, "Converted to Partner");
                    }
                }

                TempData["SuccessMessage"] = "Partner created successfully!";
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Error creating partner for LeadId {LeadId}", leadId);
                TempData["ErrorMessage"] = "Failed to create partner. Please try again.";
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnGetLeadHistoryAsync(int leadId)
        {
            var history = await _leadService.GetLeadHistoryAsync(leadId);
            if (history == null || !history.Any())
            {
                return Content("<p>Nincs előzmény ehhez a leadhez.</p>");
            }

            var html = "<ul class='list-group list-group-flush'>";
            foreach (var entry in history)
            {
                html += "<li class='list-group-item'>";
                html += $"<div><strong>Művelet:</strong> {entry.ChangeType}</div>";
                html += $"<div><strong>Dátum:</strong> {entry.ChangeDate.ToString("g")}</div>";
                html += $"<div><strong>Módosította:</strong> {entry.ChangedBy ?? "Unknown"}</div>";
                if (!string.IsNullOrEmpty(entry.FirstName) || !string.IsNullOrEmpty(entry.LastName))
                    html += $"<div><strong>Név:</strong> {entry.FirstName} {entry.LastName}</div>";
                if (!string.IsNullOrEmpty(entry.Email))
                    html += $"<div><strong>Email:</strong> {entry.Email}</div>";
                if (!string.IsNullOrEmpty(entry.PhoneNumber))
                    html += $"<div><strong>Telefon:</strong> {entry.PhoneNumber}</div>";
                if (!string.IsNullOrEmpty(entry.CompanyName))
                    html += $"<div><strong>Cég:</strong> {entry.CompanyName}</div>";
                if (!string.IsNullOrEmpty(entry.JobTitle))
                    html += $"<div><strong>Beosztás:</strong> {entry.JobTitle}</div>";
                if (!string.IsNullOrEmpty(entry.Status))
                    html += $"<div><strong>Státusz:</strong> {entry.Status}</div>";
                if (entry.PartnerId.HasValue)
                    html += $"<div><strong>Partner ID:</strong> {entry.PartnerId}</div>";
                if (!string.IsNullOrEmpty(entry.Notes))
                    html += $"<div><strong>Jegyzetek:</strong> {entry.Notes}</div>";
                html += "</li>";
            }
            html += "</ul>";

            return Content(html);
        }

        public async Task<IActionResult> OnGetDetailsPartialAsync(int id)
        {
            var lead = await _context.Leads.FindAsync(id);
            if (lead == null) return NotFound();

            return Partial("_LeadDetailsPartial", lead);
        }
    }
    
}