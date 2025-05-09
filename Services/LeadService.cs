using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Cloud9_2.Data;
using Cloud9_2.Models;
using Microsoft.EntityFrameworkCore;

namespace Cloud9_2.Services
{
    public class LeadService
    {
        private readonly ApplicationDbContext _context;

        public LeadService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task CreateLeadAsync(Lead lead, string user)
        {
            if (lead == null)
                throw new ArgumentNullException(nameof(lead));
            if (string.IsNullOrEmpty(user))
                throw new ArgumentNullException(nameof(user));

            if (lead.PartnerId.HasValue)
            {
                var partner = await _context.Partners.FindAsync(lead.PartnerId.Value);
                if (partner == null)
                    throw new InvalidOperationException($"Partner with ID {lead.PartnerId.Value} not found.");
            }

            lead.CreatedBy = user;
            lead.CreatedDate = DateTime.UtcNow;
            _context.Leads.Add(lead);
            await _context.SaveChangesAsync();

            await LogHistoryAsync(lead, user, "Created");
        }

        public async Task UpdateLeadAsync(Lead updatedLead, string user)
        {
            if (updatedLead == null)
                throw new ArgumentNullException(nameof(updatedLead));
            if (string.IsNullOrEmpty(user))
                throw new ArgumentNullException(nameof(user));

            var existingLead = await _context.Leads
                .FirstOrDefaultAsync(l => l.LeadId == updatedLead.LeadId);

            if (existingLead == null)
                throw new Exception("Lead not found");

            if (updatedLead.PartnerId.HasValue)
            {
                var partner = await _context.Partners.FindAsync(updatedLead.PartnerId.Value);
                if (partner == null)
                    throw new InvalidOperationException($"Partner with ID {updatedLead.PartnerId.Value} not found.");
            }

            await LogHistoryAsync(existingLead, user, "Updated");

            existingLead.FirstName = updatedLead.FirstName;
            existingLead.LastName = updatedLead.LastName;
            existingLead.Email = updatedLead.Email;
            existingLead.PhoneNumber = updatedLead.PhoneNumber;
            existingLead.CompanyName = updatedLead.CompanyName;
            existingLead.JobTitle = updatedLead.JobTitle;
            existingLead.Status = updatedLead.Status;
            existingLead.Notes = updatedLead.Notes;
            existingLead.PartnerId = updatedLead.PartnerId;
            existingLead.UpdatedBy = user;
            existingLead.LastContactDate = updatedLead.LastContactDate;
            existingLead.NextFollowUpDate = updatedLead.NextFollowUpDate;

            await _context.SaveChangesAsync();
        }

        public async Task<List<LeadHistory>> GetLeadHistoryAsync(int leadId)
        {
            return await _context.LeadHistories
                .Where(h => h.LeadId == leadId)
                .OrderByDescending(h => h.ChangeDate)
                .ToListAsync();
        }

        public async Task LogHistoryAsync(Lead lead, string user, string changeType)
        {
            var history = new LeadHistory
            {
                LeadId = lead.LeadId,
                FirstName = lead.FirstName,
                LastName = lead.LastName,
                Email = lead.Email,
                PhoneNumber = lead.PhoneNumber,
                CompanyName = lead.CompanyName,
                JobTitle = lead.JobTitle,
                Status = lead.Status,
                Notes = lead.Notes,
                PartnerId = lead.PartnerId,
                ChangedBy = user,
                ChangeDate = DateTime.UtcNow,
                ChangeType = changeType
            };

            _context.LeadHistories.Add(history);
            await _context.SaveChangesAsync();
        }
    }
}