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

        public async Task RecordInitialCommunicationAsync(CustomerCommunicationDto dto)
        {
            if (!await _context.CommunicationTypes.AnyAsync(ct => ct.CommunicationTypeId == dto.CommunicationTypeId))
                throw new ArgumentException("Invalid CommunicationTypeId");
            if (!await _context.CommunicationStatuses.AnyAsync(s => s.StatusId == dto.StatusId))
                throw new ArgumentException("Invalid StatusId");
            if (!await _context.Contacts.AnyAsync(c => c.ContactId == dto.ContactId))
                throw new ArgumentException("Invalid ContactId");

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

        public async Task RecordEscalationAsync(CustomerCommunicationDto dto)
        {
            if (!await _context.CommunicationTypes.AnyAsync(ct => ct.CommunicationTypeId == dto.CommunicationTypeId))
                throw new ArgumentException("Invalid CommunicationTypeId");
            if (!await _context.CommunicationStatuses.AnyAsync(s => s.StatusId == dto.StatusId))
                throw new ArgumentException("Invalid StatusId");
            if (!await _context.Contacts.AnyAsync(c => c.ContactId == dto.ContactId))
                throw new ArgumentException("Invalid ContactId");

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

        public async Task RecordFollowUpAsync(CustomerCommunicationDto dto)
        {
            if (!await _context.CommunicationTypes.AnyAsync(ct => ct.CommunicationTypeId == dto.CommunicationTypeId))
                throw new ArgumentException("Invalid CommunicationTypeId");
            if (!await _context.CommunicationStatuses.AnyAsync(s => s.StatusId == dto.StatusId))
                throw new ArgumentException("Invalid StatusId");
            if (!await _context.Contacts.AnyAsync(c => c.ContactId == dto.ContactId))
                throw new ArgumentException("Invalid ContactId");

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

        public async Task RecordResolutionAsync(CustomerCommunicationDto dto)
        {
            if (!await _context.CommunicationTypes.AnyAsync(ct => ct.CommunicationTypeId == dto.CommunicationTypeId))
                throw new ArgumentException("Invalid CommunicationTypeId");
            if (!await _context.CommunicationStatuses.AnyAsync(s => s.StatusId == dto.StatusId))
                throw new ArgumentException("Invalid StatusId");
            if (!await _context.Contacts.AnyAsync(c => c.ContactId == dto.ContactId))
                throw new ArgumentException("Invalid ContactId");

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

        public async Task<List<CustomerCommunicationDto>> ReviewCommunicationsAsync(int? orderId = null)
        {
            var query = _context.CustomerCommunications
                .Join(_context.CommunicationStatuses,
                    c => c.StatusId,
                    s => s.StatusId,
                    (c, s) => new { Communication = c, StatusName = s.Name })
                .Join(_context.CommunicationTypes,
                    cs => cs.Communication.CommunicationTypeId,
                    ct => ct.CommunicationTypeId,
                    (cs, ct) => new { cs.Communication, cs.StatusName, CommunicationTypeName = ct.Name })
                .Join(_context.Contacts,
                    csc => csc.Communication.ContactId,
                    contact => contact.ContactId,
                    (csc, contact) => new { csc.Communication, csc.StatusName, csc.CommunicationTypeName, contact.FirstName, contact.LastName })
                .GroupJoin(_context.Users,
                    csc => csc.Communication.AgentId,
                    user => user.Id,
                    (csc, users) => new { csc.Communication, csc.StatusName, csc.CommunicationTypeName, csc.FirstName, csc.LastName, Users = users })
                .SelectMany(csc => csc.Users.DefaultIfEmpty(),
                    (csc, user) => new CustomerCommunicationDto
                    {
                        CustomerCommunicationId = csc.Communication.CustomerCommunicationId,
                        CommunicationTypeId = csc.Communication.CommunicationTypeId,
                        CommunicationTypeName = csc.CommunicationTypeName,
                        Date = csc.Communication.Date,
                        Subject = csc.Communication.Subject,
                        Note = csc.Communication.Note,
                        ContactId = csc.Communication.ContactId,
                        FirstName = csc.FirstName,
                        LastName = csc.LastName,
                        AgentId = csc.Communication.AgentId,
                        AgentName = user != null ? user.UserName : null,
                        StatusId = csc.Communication.StatusId,
                        StatusName = csc.StatusName,
                        AttachmentPath = csc.Communication.AttachmentPath,
                        Metadata = csc.Communication.Metadata,
                        OrderId = csc.Communication.OrderId,
                        PartnerId = csc.Communication.PartnerId,
                        LeadId = csc.Communication.LeadId,
                        QuoteId = csc.Communication.QuoteId
                    });

            if (orderId.HasValue)
            {
                query = query.Where(c => c.OrderId == orderId);
            }

            return await query.OrderBy(c => c.Date).ToListAsync();
        }

        public async Task AddCommunicationPostAsync(int communicationId, string content, string createdByUserId)
        {
            if (!await _context.CustomerCommunications.AnyAsync(c => c.CustomerCommunicationId == communicationId))
                throw new ArgumentException("Invalid CommunicationId");

            var user = await _userManager.FindByIdAsync(createdByUserId);
            if (user == null)
                throw new ArgumentException("Invalid UserId");

            // Find a contact associated with the communication's PartnerId (fallback to primary contact)
            var communication = await _context.CustomerCommunications
                .FirstOrDefaultAsync(c => c.CustomerCommunicationId == communicationId);
            if (communication == null)
                throw new ArgumentException("Communication not found");

            var contact = await _context.Contacts
                .Where(c => c.PartnerId == communication.PartnerId && c.IsPrimary)
                .FirstOrDefaultAsync();
            if (contact == null)
                throw new ArgumentException("No primary contact found for the communication's partner");

            var post = new CommunicationPost
            {
                CustomerCommunicationId = communicationId,
                Content = content,
                CreatedById = contact.ContactId,
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

            var assignedByUser = await _userManager.FindByIdAsync(assignedByUserId);
            if (assignedByUser == null)
                throw new ArgumentException("Invalid AssignedBy UserId");

            // Find a contact associated with the communication's PartnerId (fallback to primary contact)
            var communication = await _context.CustomerCommunications
                .FirstOrDefaultAsync(c => c.CustomerCommunicationId == communicationId);
            if (communication == null)
                throw new ArgumentException("Communication not found");

            var assignedByContact = await _context.Contacts
                .Where(c => c.PartnerId == communication.PartnerId && c.IsPrimary)
                .FirstOrDefaultAsync();
            if (assignedByContact == null)
                throw new ArgumentException("No primary contact found for the communication's partner");

            var responsible = new CommunicationResponsible
            {
                CustomerCommunicationId = communicationId,
                ResponsibleId = responsibleContactId,
                AssignedById = assignedByContact.ContactId,
                AssignedAt = DateTime.UtcNow
            };

            _context.CommunicationResponsibles.Add(responsible);
            await _context.SaveChangesAsync();
        }

        public async Task<CustomerCommunicationDto> GetCommunicationHistoryAsync(int communicationId)
        {
            var communication = await _context.CustomerCommunications
                .Join(_context.CommunicationStatuses,
                    c => c.StatusId,
                    s => s.StatusId,
                    (c, s) => new { Communication = c, StatusName = s.Name })
                .Join(_context.CommunicationTypes,
                    cs => cs.Communication.CommunicationTypeId,
                    ct => ct.CommunicationTypeId,
                    (cs, ct) => new { cs.Communication, cs.StatusName, CommunicationTypeName = ct.Name })
                .Join(_context.Contacts,
                    csc => csc.Communication.ContactId,
                    contact => contact.ContactId,
                    (csc, contact) => new { csc.Communication, csc.StatusName, csc.CommunicationTypeName, contact.FirstName, contact.LastName })
                .GroupJoin(_context.CommunicationPosts,
                    csc => csc.Communication.CustomerCommunicationId,
                    p => p.CustomerCommunicationId,
                    (csc, posts) => new { csc.Communication, csc.StatusName, csc.CommunicationTypeName, csc.FirstName, csc.LastName, Posts = posts })
                .GroupJoin(_context.CommunicationResponsibles,
                    csc => csc.Communication.CustomerCommunicationId,
                    r => r.CustomerCommunicationId,
                    (csc, responsibles) => new { csc.Communication, csc.StatusName, csc.CommunicationTypeName, csc.FirstName, csc.LastName, csc.Posts, Responsibles = responsibles })
                .Where(c => c.Communication.CustomerCommunicationId == communicationId)
                .Select(c => new CustomerCommunicationDto
                {
                    CustomerCommunicationId = c.Communication.CustomerCommunicationId,
                    CommunicationTypeId = c.Communication.CommunicationTypeId,
                    CommunicationTypeName = c.CommunicationTypeName,
                    Date = c.Communication.Date,
                    Subject = c.Communication.Subject,
                    Note = c.Communication.Note,
                    ContactId = c.Communication.ContactId,
                    FirstName = c.FirstName,
                    LastName = c.LastName,
                    AgentId = c.Communication.AgentId,
                    StatusId = c.Communication.StatusId,
                    StatusName = c.StatusName,
                    AttachmentPath = c.Communication.AttachmentPath,
                    Metadata = c.Communication.Metadata,
                    OrderId = c.Communication.OrderId,
                    PartnerId = c.Communication.PartnerId,
                    LeadId = c.Communication.LeadId,
                    QuoteId = c.Communication.QuoteId,
                    Posts = c.Posts.Select(p => new CommunicationPostDto
                    {
                        CommunicationPostId = p.CommunicationPostId,
                        Content = p.Content,
                        CreatedByName = _context.Contacts
                            .Where(c => c.ContactId == p.CreatedById)
                            .Select(c => c.FirstName + " " + c.LastName)
                            .FirstOrDefault(),
                        CreatedAt = p.CreatedAt
                    }).ToList(),
                    CurrentResponsible = c.Responsibles
                        .OrderByDescending(r => r.AssignedAt)
                        .Select(r => new CommunicationResponsibleDto
                        {
                            CommunicationResponsibleId = r.CommunicationResponsibleId,
                            ResponsibleName = _context.Contacts
                                .Where(c => c.ContactId == r.ResponsibleId)
                                .Select(c => c.FirstName + " " + c.LastName)
                                .FirstOrDefault(),
                            AssignedByName = _context.Contacts
                                .Where(c => c.ContactId == r.AssignedById)
                                .Select(c => c.FirstName + " " + c.LastName)
                                .FirstOrDefault(),
                            AssignedAt = r.AssignedAt
                        }).FirstOrDefault(),
                    ResponsibleHistory = c.Responsibles
                        .Select(r => new CommunicationResponsibleDto
                        {
                            CommunicationResponsibleId = r.CommunicationResponsibleId,
                            ResponsibleName = _context.Contacts
                                .Where(c => c.ContactId == r.ResponsibleId)
                                .Select(c => c.FirstName + " " + c.LastName)
                                .FirstOrDefault(),
                            AssignedByName = _context.Contacts
                                .Where(c => c.ContactId == r.AssignedById)
                                .Select(c => c.FirstName + " " + c.LastName)
                                .FirstOrDefault(),
                            AssignedAt = r.AssignedAt
                        }).ToList()
                })
                .FirstOrDefaultAsync();

            if (communication == null)
                throw new ArgumentException("Communication not found");

            return communication;
        }
    }
}