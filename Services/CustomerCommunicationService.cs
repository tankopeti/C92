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
            // if (!dto.ContactId.HasValue || !await _context.Contacts.AnyAsync(c => c.ContactId == dto.ContactId))
            //     throw new ArgumentException("Invalid ContactId");
            if (string.IsNullOrWhiteSpace(dto.Subject))
                throw new ArgumentException("Subject is required");
            if (dto.Date == default)
                throw new ArgumentException("Date is required");

            if (dto.AgentId != null && !await _context.Users.AnyAsync(u => u.Id == dto.AgentId))
                throw new ArgumentException("Invalid AgentId");
            if (dto.PartnerId.HasValue && !await _context.Partners.AnyAsync(p => p.PartnerId == dto.PartnerId))
                throw new ArgumentException("Invalid PartnerId");
            // if (dto.LeadId.HasValue && !await _context.Leads.AnyAsync(l => l.LeadId == dto.LeadId))
            //     throw new ArgumentException("Invalid LeadId");
            // if (dto.QuoteId.HasValue && !await _context.Quotes.AnyAsync(q => q.QuoteId == dto.QuoteId))
            //     throw new ArgumentException("Invalid QuoteId");

            var communication = new CustomerCommunication
            {
                CommunicationTypeId = dto.CommunicationTypeId,
                Date = dto.Date,
                Subject = dto.Subject,
                Note = dto.Note,
                // ContactId = dto.ContactId,
                AgentId = dto.AgentId,
                StatusId = dto.StatusId,
                AttachmentPath = dto.AttachmentPath,
                Metadata = dto.Metadata,
                // OrderId = dto.OrderId,
                PartnerId = dto.PartnerId,
                // LeadId = dto.LeadId,
                // QuoteId = dto.QuoteId
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
            // if (!dto.ContactId.HasValue || !await _context.Contacts.AnyAsync(c => c.ContactId == dto.ContactId))
            //     throw new ArgumentException("Invalid ContactId");
            if (string.IsNullOrWhiteSpace(dto.Subject))
                throw new ArgumentException("Subject is required");
            if (dto.Date == default)
                throw new ArgumentException("Date is required");

            if (dto.AgentId != null && !await _context.Users.AnyAsync(u => u.Id == dto.AgentId))
                throw new ArgumentException("Invalid AgentId");
            if (dto.PartnerId.HasValue && !await _context.Partners.AnyAsync(p => p.PartnerId == dto.PartnerId))
                throw new ArgumentException("Invalid PartnerId");
            // if (dto.LeadId.HasValue && !await _context.Leads.AnyAsync(l => l.LeadId == dto.LeadId))
            //     throw new ArgumentException("Invalid LeadId");
            // if (dto.QuoteId.HasValue && !await _context.Quotes.AnyAsync(q => q.QuoteId == dto.QuoteId))
            //     throw new ArgumentException("Invalid QuoteId");

            communication.CommunicationTypeId = dto.CommunicationTypeId;
            communication.Date = dto.Date;
            communication.Subject = dto.Subject;
            communication.Note = dto.Note;
            // communication.ContactId = dto.ContactId;
            communication.AgentId = dto.AgentId;
            communication.StatusId = dto.StatusId;
            communication.AttachmentPath = dto.AttachmentPath;
            communication.Metadata = dto.Metadata;
            // communication.OrderId = dto.OrderId;
            communication.PartnerId = dto.PartnerId;
            // communication.LeadId = dto.LeadId;
            // communication.QuoteId = dto.QuoteId;

            await _context.SaveChangesAsync();
        }

        public async Task DeleteCommunicationAsync(int communicationId)
        {
            var communication = await _context.CustomerCommunications
                .FirstOrDefaultAsync(c => c.CustomerCommunicationId == communicationId);
            if (communication == null)
                throw new ArgumentException("Communication not found");

            // Delete related CommunicationPosts
            var relatedPosts = await _context.CommunicationPosts
                .Where(p => p.CustomerCommunicationId == communicationId)
                .ToListAsync();
            if (relatedPosts.Any())
            {
                _context.CommunicationPosts.RemoveRange(relatedPosts);
            }

            // Delete related CommunicationResponsibles
            var relatedResponsibles = await _context.CommunicationResponsibles
                .Where(r => r.CustomerCommunicationId == communicationId)
                .ToListAsync();
            if (relatedResponsibles.Any())
            {
                _context.CommunicationResponsibles.RemoveRange(relatedResponsibles);
            }

            // Delete the CustomerCommunication
            _context.CustomerCommunications.Remove(communication);
            await _context.SaveChangesAsync();
        }

        public async Task<List<CustomerCommunicationDto>> ReviewCommunicationsAsync(int? orderId = null)
        {
            var query = _context.CustomerCommunications
                .Include(c => c.CommunicationType)
                .Include(c => c.Status)
                // .Include(c => c.Contact)
                .Include(c => c.Agent)
                .Include(c => c.Partner) // <-- Add this!
                .Select(c => new CustomerCommunicationDto
                {
                    CustomerCommunicationId = c.CustomerCommunicationId,
                    CommunicationTypeId = c.CommunicationTypeId,
                    CommunicationTypeName = c.CommunicationType.Name,
                    Date = c.Date,
                    Subject = c.Subject,
                    Note = c.Note,
                    // ContactId = c.ContactId,
                    // FirstName = c.Contact != null ? c.Contact.FirstName : null,
                    // LastName = c.Contact != null ? c.Contact.LastName : null,
                    AgentId = c.AgentId,
                    AgentName = c.Agent != null ? c.Agent.UserName : null,
                    StatusId = c.StatusId,
                    StatusName = c.Status.Name,
                    AttachmentPath = c.AttachmentPath,
                    Metadata = c.Metadata,
                    // OrderId = c.OrderId,
                    PartnerId = c.PartnerId,
                    PartnerName = c.Partner != null ? c.Partner.Name : null, // <-- Add this line!
                    // LeadId = c.LeadId,
                    // QuoteId = c.QuoteId
                });

            // if (orderId.HasValue)
            //     query = query.Where(c => c.OrderId == orderId);

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

            var post = new CommunicationPost
            {
                CustomerCommunicationId = communicationId,
                Content = content,
                CreatedById = createdByUserId,
                CreatedAt = DateTime.UtcNow
            };

            _context.CommunicationPosts.Add(post);
            await _context.SaveChangesAsync();
        }

        public async Task AssignResponsibleAsync(int communicationId, string responsibleUserId, string assignedByUserId)
        {
            var communication = await _context.CustomerCommunications
                .FindAsync(communicationId);
            if (communication == null)
                throw new ArgumentException($"Communication with ID {communicationId} not found.");

            var responsibleUser = await _userManager.FindByIdAsync(responsibleUserId);
            // if (responsibleUser == null || !await _userManager.IsInRoleAsync(responsibleUser, "Admin"))   - itt lehet beállítani a user típusokat.
            //     throw new ArgumentException($"User with ID {responsibleUserId} is not an Admin user.");

            var assignedByUser = await _userManager.FindByIdAsync(assignedByUserId);
            if (assignedByUser == null)
                throw new ArgumentException($"User with ID {assignedByUserId} not found.");

            var communicationResponsible = new CommunicationResponsible
            {
                CustomerCommunicationId = communicationId,
                ResponsibleId = responsibleUserId,
                AssignedById = assignedByUserId,
                AssignedAt = DateTime.UtcNow
            };

            _context.CommunicationResponsibles.Add(communicationResponsible);
            await _context.SaveChangesAsync();
        }

public async Task<CustomerCommunicationDto> GetCommunicationHistoryAsync(int communicationId)
        {
            var communication = await _context.CustomerCommunications
                .Where(c => c.CustomerCommunicationId == communicationId)
                .Select(c => new CustomerCommunicationDto
                {
                    CustomerCommunicationId = c.CustomerCommunicationId,
                    CommunicationTypeId = c.CommunicationTypeId,
                    CommunicationTypeName = c.CommunicationType != null ? c.CommunicationType.Name : null,
                    Date = c.Date,
                    Subject = c.Subject,
                    Note = c.Note,
                    // ContactId = c.ContactId,
                    // FirstName = c.Contact != null ? c.Contact.FirstName : null,
                    // LastName = c.Contact != null ? c.Contact.LastName : null,
                    AgentId = c.AgentId,
                    AgentName = c.Agent != null ? c.Agent.UserName : null,
                    StatusId = c.StatusId,
                    StatusName = c.Status != null ? c.Status.Name : null,
                    AttachmentPath = c.AttachmentPath,
                    Metadata = c.Metadata,
                    PartnerId = c.PartnerId,
                    // LeadId = c.LeadId,
                    // QuoteId = c.QuoteId,
                    // OrderId = c.OrderId,
                    Posts = c.Posts.Select(p => new CommunicationPostDto
                    {
                        CommunicationPostId = p.CommunicationPostId,
                        Content = p.Content,
                        CreatedByName = p.CreatedBy != null 
                            ? (p.CreatedBy.UserName ?? p.CreatedBy.Email ?? "Unknown")
                            : "Unknown",
                        CreatedAt = p.CreatedAt
                    }).ToList(),
                    CurrentResponsible = c.ResponsibleHistory
                        .OrderByDescending(r => r.AssignedAt)
                        .Select(r => new CommunicationResponsibleDto
                        {
                            CommunicationResponsibleId = r.CommunicationResponsibleId,
                            ResponsibleId = r.ResponsibleId,
                            ResponsibleName = r.Responsible != null 
                                ? (r.Responsible.UserName ?? r.Responsible.Email ?? "Unknown")
                                : "Unknown",
                            AssignedById = r.AssignedById,
                            AssignedByName = r.AssignedBy != null 
                                ? (r.AssignedBy.UserName ?? r.AssignedBy.Email ?? "Unknown")
                                : "Unknown",
                            AssignedAt = r.AssignedAt
                        }).FirstOrDefault(),
                    ResponsibleHistory = c.ResponsibleHistory
                        .Select(r => new CommunicationResponsibleDto
                        {
                            CommunicationResponsibleId = r.CommunicationResponsibleId,
                            ResponsibleId = r.ResponsibleId,
                            ResponsibleName = r.Responsible != null 
                                ? (r.Responsible.UserName ?? r.Responsible.Email ?? "Unknown")
                                : "Unknown",
                            AssignedById = r.AssignedById,
                            AssignedByName = r.AssignedBy != null 
                                ? (r.AssignedBy.UserName ?? r.AssignedBy.Email ?? "Unknown")
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

        // public async Task<List<Contact>> GetContactsAsync(string term)
        // {
        //     var query = _context.Contacts.AsQueryable();
        //     if (!string.IsNullOrEmpty(term))
        //     {
        //         term = term.ToLower();
        //         query = query.Where(c => c.FirstName.ToLower().Contains(term) || c.LastName.ToLower().Contains(term));
        //     }
        //     return await query.ToListAsync();
        // }

public async Task<List<CustomerCommunication>> GetCommunicationsByUserAsync(string userId)
        {
            return await _context.CustomerCommunications
                .Where(c => c.AgentId == userId) // Changed from UserId to AgentId
                .ToListAsync();
        }

        public async Task<List<ApplicationUser>> GetAspNetUsersAsync(string searchTerm = null)
        {
            // Start with all users
            var usersQuery = _userManager.Users.AsQueryable();

            // Optionally filter by a search term (on username or email)
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                usersQuery = usersQuery.Where(u =>
                    u.UserName.Contains(searchTerm) ||
                    u.Email.Contains(searchTerm)
                );
            }

            // Fetch the users as a list asynchronously
            return await usersQuery.ToListAsync();
        }
    }
}