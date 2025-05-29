using Cloud9_2.Data;
using Cloud9_2.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Cloud9_2.Services
{
    public class CustomerCommunicationService
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public CustomerCommunicationService(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task RecordCommunicationAsync(CustomerCommunicationDto dto, string communicationPurpose)
        {
            if (dto.CommunicationTypeId <= 0 || !await _context.CommunicationTypes.AnyAsync(ct => ct.CommunicationTypeId == dto.CommunicationTypeId))
                throw new ArgumentException("Invalid CommunicationTypeId");
            if (dto.StatusId <= 0 || !await _context.CommunicationStatuses.AnyAsync(s => s.StatusId == dto.StatusId))
                throw new ArgumentException("Invalid StatusId");
            if (!dto.ContactId.HasValue || !await _context.Contacts.AnyAsync(c => c.ContactId == dto.ContactId))
                throw new ArgumentException("Invalid ContactId");
            if (string.IsNullOrWhiteSpace(dto.Subject))
                throw new ArgumentException("Subject is required");
            if (dto.Date == default)
                throw new ArgumentException("Date is required");

            if (dto.AgentId != null && !await _context.Users.AnyAsync(u => u.Id == dto.AgentId))
                throw new ArgumentException("Invalid AgentId");
            if (dto.PartnerId.HasValue && !await _context.Partners.AnyAsync(p => p.PartnerId == dto.PartnerId))
                throw new ArgumentException("Invalid PartnerId");
            if (dto.LeadId.HasValue && !await _context.Leads.AnyAsync(l => l.LeadId == dto.LeadId))
                throw new ArgumentException("Invalid LeadId");
            if (dto.QuoteId.HasValue && !await _context.Quotes.AnyAsync(q => q.QuoteId == dto.QuoteId))
                throw new ArgumentException("Invalid QuoteId");

            var communication = new CustomerCommunication
            {
                CommunicationTypeId = dto.CommunicationTypeId,
                Date = dto.Date,
                Subject = dto.Subject,
                Note = dto.Note,
                ContactId = dto.ContactId,
                AgentId = dto.AgentId,
                StatusId = dto.StatusId,
                AttachmentPath = dto.AttachmentPath,
                Metadata = dto.Metadata,
                OrderId = dto.OrderId,
                PartnerId = dto.PartnerId,
                LeadId = dto.LeadId,
                QuoteId = dto.QuoteId
            };

            _context.CustomerCommunications.Add(communication);
            await _context.SaveChangesAsync();
            dto.CustomerCommunicationId = communication.CustomerCommunicationId;
        }

        public async Task UpdateCommunicationAsync(CustomerCommunicationDto dto)
        {
            if (dto.CustomerCommunicationId <= 0)
                throw new ArgumentException("Invalid CustomerCommunicationId");

            var communication = await _context.CustomerCommunications
                .FirstOrDefaultAsync(c => c.CustomerCommunicationId == dto.CustomerCommunicationId);
            if (communication == null)
                throw new ArgumentException("Communication not found");

            if (dto.CommunicationTypeId <= 0 || !await _context.CommunicationTypes.AnyAsync(ct => ct.CommunicationTypeId == dto.CommunicationTypeId))
                throw new ArgumentException("Invalid CommunicationTypeId");
            if (dto.StatusId <= 0 || !await _context.CommunicationStatuses.AnyAsync(s => s.StatusId == dto.StatusId))
                throw new ArgumentException("Invalid StatusId");
            if (!dto.ContactId.HasValue || !await _context.Contacts.AnyAsync(c => c.ContactId == dto.ContactId))
                throw new ArgumentException("Invalid ContactId");
            if (string.IsNullOrWhiteSpace(dto.Subject))
                throw new ArgumentException("Subject is required");
            if (dto.Date == default)
                throw new ArgumentException("Date is required");

            if (dto.AgentId != null && !await _context.Users.AnyAsync(u => u.Id == dto.AgentId))
                throw new ArgumentException("Invalid AgentId");
            if (dto.PartnerId.HasValue && !await _context.Partners.AnyAsync(p => p.PartnerId == dto.PartnerId))
                throw new ArgumentException("Invalid PartnerId");
            if (dto.LeadId.HasValue && !await _context.Leads.AnyAsync(l => l.LeadId == dto.LeadId))
                throw new ArgumentException("Invalid LeadId");
            if (dto.QuoteId.HasValue && !await _context.Quotes.AnyAsync(q => q.QuoteId == dto.QuoteId))
                throw new ArgumentException("Invalid QuoteId");

            communication.CommunicationTypeId = dto.CommunicationTypeId;
            communication.Date = dto.Date;
            communication.Subject = dto.Subject;
            communication.Note = dto.Note;
            communication.ContactId = dto.ContactId;
            communication.AgentId = dto.AgentId;
            communication.StatusId = dto.StatusId;
            communication.AttachmentPath = dto.AttachmentPath;
            communication.Metadata = dto.Metadata;
            communication.OrderId = dto.OrderId;
            communication.PartnerId = dto.PartnerId;
            communication.LeadId = dto.LeadId;
            communication.QuoteId = dto.QuoteId;

            await _context.SaveChangesAsync();
        }

        public async Task DeleteCommunicationAsync(int communicationId)
        {
            var communication = await _context.CustomerCommunications
                .FirstOrDefaultAsync(c => c.CustomerCommunicationId == communicationId);
            if (communication == null)
                throw new ArgumentException("Communication not found");

            var deletedStatus = await _context.CommunicationStatuses
                .FirstOrDefaultAsync(s => s.Name == "Deleted");
            if (deletedStatus == null)
                throw new InvalidOperationException("Deleted status not configured");

            communication.StatusId = deletedStatus.StatusId;
            await _context.SaveChangesAsync();
        }

        public async Task<List<CustomerCommunicationDto>> ReviewCommunicationsAsync(int? orderId = null)
        {
            var query = _context.CustomerCommunications
                .Include(c => c.CommunicationType)
                .Include(c => c.Status)
                .Include(c => c.Contact)
                .Include(c => c.Agent)
                .Select(c => new CustomerCommunicationDto
                {
                    CustomerCommunicationId = c.CustomerCommunicationId,
                    CommunicationTypeId = c.CommunicationTypeId,
                    CommunicationTypeName = c.CommunicationType.Name,
                    Date = c.Date,
                    Subject = c.Subject,
                    Note = c.Note,
                    ContactId = c.ContactId,
                    FirstName = c.Contact != null ? c.Contact.FirstName : null,
                    LastName = c.Contact != null ? c.Contact.LastName : null,
                    AgentId = c.AgentId,
                    AgentName = c.Agent != null ? c.Agent.UserName : null,
                    StatusId = c.StatusId,
                    StatusName = c.Status.Name,
                    AttachmentPath = c.AttachmentPath,
                    Metadata = c.Metadata,
                    OrderId = c.OrderId,
                    PartnerId = c.PartnerId,
                    LeadId = c.LeadId,
                    QuoteId = c.QuoteId
                });

            if (orderId.HasValue)
                query = query.Where(c => c.OrderId == orderId);

            return await query.OrderBy(c => c.Date).ToListAsync();
        }

        public async Task AddCommunicationPostAsync(int communicationId, string content, string createdByUserId)
        {
            if (!await _context.CustomerCommunications.AnyAsync(c => c.CustomerCommunicationId == communicationId))
                throw new ArgumentException("Invalid CommunicationId");

            var user = await _userManager.FindByIdAsync(createdByUserId) ?? 
                       await _userManager.FindByEmailAsync(createdByUserId);
            if (user == null)
                throw new ArgumentException("Invalid UserId");

            var communication = await _context.CustomerCommunications
                .Include(c => c.Contact)
                .Include(c => c.Partner)
                .ThenInclude(p => p.Contacts)
                .FirstOrDefaultAsync(c => c.CustomerCommunicationId == communicationId);
            if (communication == null)
                throw new ArgumentException("Communication not found");

            var contact = communication.Contact ??
                          communication.Partner?.Contacts.FirstOrDefault(c => c.IsPrimary) ??
                          communication.Partner?.Contacts.FirstOrDefault();

            var post = new CommunicationPost
            {
                CustomerCommunicationId = communicationId,
                Content = content,
                CreatedById = contact?.ContactId, // Nullable
                CreatedAt = DateTime.UtcNow
            };

            _context.CommunicationPosts.Add(post);
            await _context.SaveChangesAsync();
        }

        public async Task AssignResponsibleAsync(int communicationId, int responsibleContactId, string assignedByUserId)
        {
            if (!await _context.CustomerCommunications.AnyAsync(c => c.CustomerCommunicationId == communicationId))
                throw new ArgumentException("Invalid CommunicationId");
            if (!await _context.Contacts.AnyAsync(c => c.ContactId == responsibleContactId))
                throw new ArgumentException("Invalid Responsible ContactId");

            var assignedByUser = await _userManager.FindByIdAsync(assignedByUserId) ?? 
                                 await _userManager.FindByEmailAsync(assignedByUserId);
            if (assignedByUser == null)
                throw new ArgumentException("Invalid AssignedBy UserId");

            var communication = await _context.CustomerCommunications
                .Include(c => c.Contact)
                .Include(c => c.Partner)
                .ThenInclude(p => p.Contacts)
                .FirstOrDefaultAsync(c => c.CustomerCommunicationId == communicationId);
            if (communication == null)
                throw new ArgumentException("Communication not found");

            var assignedByContact = communication.Contact ??
                                   communication.Partner?.Contacts.FirstOrDefault(c => c.IsPrimary) ??
                                   communication.Partner?.Contacts.FirstOrDefault();

            var responsible = new CommunicationResponsible
            {
                CustomerCommunicationId = communicationId,
                ResponsibleId = responsibleContactId,
                AssignedById = assignedByContact?.ContactId, // Nullable
                AssignedAt = DateTime.UtcNow
            };

            _context.CommunicationResponsibles.Add(responsible);
            await _context.SaveChangesAsync();
        }

        public async Task<CustomerCommunicationDto> GetCommunicationHistoryAsync(int communicationId)
        {
            var communication = await _context.CustomerCommunications
                .Include(c => c.CommunicationType)
                .Include(c => c.Status)
                .Include(c => c.Contact)
                .Include(c => c.Agent)
                .Include(c => c.Posts)
                .ThenInclude(p => p.CreatedBy)
                .Include(c => c.ResponsibleHistory)
                .ThenInclude(r => r.Responsible)
                .Include(c => c.ResponsibleHistory)
                .ThenInclude(r => r.AssignedBy)
                .Where(c => c.CustomerCommunicationId == communicationId)
                .Select(c => new CustomerCommunicationDto
                {
                    CustomerCommunicationId = c.CustomerCommunicationId,
                    CommunicationTypeId = c.CommunicationTypeId,
                    CommunicationTypeName = c.CommunicationType.Name,
                    Date = c.Date,
                    Subject = c.Subject,
                    Note = c.Note,
                    ContactId = c.ContactId,
                    FirstName = c.Contact != null ? c.Contact.FirstName : null,
                    LastName = c.Contact != null ? c.Contact.LastName : null,
                    AgentId = c.AgentId,
                    AgentName = c.Agent != null ? c.Agent.UserName : null,
                    StatusId = c.StatusId,
                    StatusName = c.Status.Name,
                    AttachmentPath = c.AttachmentPath,
                    Metadata = c.Metadata,
                    OrderId = c.OrderId,
                    PartnerId = c.PartnerId,
                    LeadId = c.LeadId,
                    QuoteId = c.QuoteId,
                    Posts = c.Posts.Select(p => new CommunicationPostDto
                    {
                        CommunicationPostId = p.CommunicationPostId,
                        Content = p.Content,
                        CreatedByName = p.CreatedBy != null 
                            ? p.CreatedBy.FirstName + " " + p.CreatedBy.LastName 
                            : "Unknown",
                        CreatedAt = p.CreatedAt
                    }).ToList(),
                    CurrentResponsible = c.ResponsibleHistory
                        .OrderByDescending(r => r.AssignedAt)
                        .Select(r => new CommunicationResponsibleDto
                        {
                            CommunicationResponsibleId = r.CommunicationResponsibleId,
                            ResponsibleName = r.Responsible != null 
                                ? r.Responsible.FirstName + " " + r.Responsible.LastName 
                                : "Unknown",
                            AssignedByName = r.AssignedBy != null 
                                ? r.AssignedBy.FirstName + " " + r.AssignedBy.LastName 
                                : "Unknown",
                            AssignedAt = r.AssignedAt
                        }).FirstOrDefault(),
                    ResponsibleHistory = c.ResponsibleHistory
                        .Select(r => new CommunicationResponsibleDto
                        {
                            CommunicationResponsibleId = r.CommunicationResponsibleId,
                            ResponsibleName = r.Responsible != null 
                                ? r.Responsible.FirstName + " " + r.Responsible.LastName 
                                : "Unknown",
                            AssignedByName = r.AssignedBy != null 
                                ? r.AssignedBy.FirstName + " " + r.AssignedBy.LastName 
                                : "Unknown",
                            AssignedAt = r.AssignedAt
                        }).ToList()
                })
                .FirstOrDefaultAsync();

            if (communication == null)
                throw new ArgumentException("Communication not found");

            communication.Posts = communication.Posts ?? new List<CommunicationPostDto>();
            communication.ResponsibleHistory = communication.ResponsibleHistory ?? new List<CommunicationResponsibleDto>();
            communication.CurrentResponsible = communication.CurrentResponsible ?? new CommunicationResponsibleDto();

            return communication;
        }

        public async Task<List<Contact>> GetContactsAsync(string term)
        {
            var query = _context.Contacts.AsQueryable();
            if (!string.IsNullOrEmpty(term))
            {
                term = term.ToLower();
                query = query.Where(c => c.FirstName.ToLower().Contains(term) || c.LastName.ToLower().Contains(term));
            }
            return await query.ToListAsync();
        }
    }
}