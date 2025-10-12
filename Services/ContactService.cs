
using Cloud9_2.Data;
using Cloud9_2.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Cloud9_2.Services
{
    public class ContactService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ContactService> _logger;

        public ContactService(ApplicationDbContext context, ILogger<ContactService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<List<ContactDto>> GetAllAsync()
        {
            var contacts = await _context.Contacts
                .Include(c => c.Status)
                .ToListAsync();

            var dtos = contacts.Select(c => new ContactDto
            {
                ContactId = c.ContactId,
                FirstName = c.FirstName ?? string.Empty,
                LastName = c.LastName ?? string.Empty,
                Email = c.Email,
                PhoneNumber = c.PhoneNumber,
                PhoneNumber2 = c.PhoneNumber2,
                JobTitle = c.JobTitle,
                Comment = c.Comment,
                Comment2 = c.Comment2,
                IsPrimary = c.IsPrimary,
                StatusId = c.StatusId,
                Status = c.Status,
                PartnerId = c.PartnerId,
                CreatedDate = c.CreatedDate,
                UpdatedDate = c.UpdatedDate
            }).ToList();

            return dtos;
        }

        public async Task<ContactDto?> GetByIdAsync(int id)
        {
            var contact = await _context.Contacts
                .Include(c => c.Status)
                .FirstOrDefaultAsync(c => c.ContactId == id);

            if (contact == null)
            {
                _logger.LogWarning("Contact with ID {Id} not found", id);
                return null;
            }

            var dto = new ContactDto
            {
                ContactId = contact.ContactId,
                FirstName = contact.FirstName ?? string.Empty,
                LastName = contact.LastName ?? string.Empty,
                Email = contact.Email,
                PhoneNumber = contact.PhoneNumber,
                PhoneNumber2 = contact.PhoneNumber2,
                JobTitle = contact.JobTitle,
                Comment = contact.Comment,
                Comment2 = contact.Comment2,
                IsPrimary = contact.IsPrimary,
                StatusId = contact.StatusId,
                Status = contact.Status,
                PartnerId = contact.PartnerId,
                CreatedDate = contact.CreatedDate,
                UpdatedDate = contact.UpdatedDate
            };

            return dto;
        }

        public async Task<ContactDto> CreateAsync(CreateContactDto dto)
        {
            if (dto.PartnerId.HasValue && !await _context.Partners.AnyAsync(p => p.PartnerId == dto.PartnerId))
                throw new ArgumentException("Érvénytelen PartnerId");
            if (dto.StatusId.HasValue && !await _context.PartnerStatuses.AnyAsync(s => s.Id == dto.StatusId))
                throw new ArgumentException("Érvénytelen StatusId");

            var contact = new Contact
            {
                FirstName = dto.FirstName,
                LastName = dto.LastName,
                Email = dto.Email,
                PhoneNumber = dto.PhoneNumber,
                PhoneNumber2 = dto.PhoneNumber2,
                JobTitle = dto.JobTitle,
                Comment = dto.Comment,
                Comment2 = dto.Comment2,
                IsPrimary = dto.IsPrimary,
                StatusId = dto.StatusId,
                PartnerId = dto.PartnerId,
                CreatedDate = DateTime.UtcNow,
                UpdatedDate = null
            };

            _context.Contacts.Add(contact);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Contact created with ID {ContactId} at {CreatedDate}", contact.ContactId, contact.CreatedDate);

            return new ContactDto
            {
                ContactId = contact.ContactId,
                FirstName = contact.FirstName,
                LastName = contact.LastName,
                Email = contact.Email,
                PhoneNumber = contact.PhoneNumber,
                PhoneNumber2 = contact.PhoneNumber2,
                JobTitle = contact.JobTitle,
                Comment = contact.Comment,
                Comment2 = contact.Comment2,
                IsPrimary = contact.IsPrimary,
                StatusId = contact.StatusId,
                PartnerId = contact.PartnerId,
                CreatedDate = contact.CreatedDate,
                UpdatedDate = contact.UpdatedDate
            };
        }

        public async Task<ContactDto?> UpdateAsync(int id, UpdateContactDto dto)
        {
            var contact = await _context.Contacts.FindAsync(id);
            if (contact == null)
            {
                _logger.LogWarning("Contact with ID {Id} not found for update", id);
                return null;
            }

            if (dto.PartnerId != 0 && !await _context.Partners.AnyAsync(p => p.PartnerId == dto.PartnerId))
                throw new ArgumentException("Érvénytelen PartnerId");
            if (dto.StatusId.HasValue && !await _context.PartnerStatuses.AnyAsync(s => s.Id == dto.StatusId))
                throw new ArgumentException("Érvénytelen StatusId");

            contact.FirstName = dto.FirstName;
            contact.LastName = dto.LastName;
            contact.Email = dto.Email;
            contact.PhoneNumber = dto.PhoneNumber;
            contact.PhoneNumber2 = dto.PhoneNumber2;
            contact.JobTitle = dto.JobTitle;
            contact.Comment = dto.Comment;
            contact.Comment2 = dto.Comment2;
            contact.IsPrimary = dto.IsPrimary;
            contact.StatusId = dto.StatusId;
            contact.PartnerId = dto.PartnerId;
            contact.UpdatedDate = DateTime.UtcNow; // Set update date

            await _context.SaveChangesAsync();

            _logger.LogInformation("Contact updated with ID {ContactId} at {UpdatedDate}", contact.ContactId, contact.UpdatedDate);

            return new ContactDto
            {
                ContactId = contact.ContactId,
                FirstName = contact.FirstName,
                LastName = contact.LastName,
                Email = contact.Email,
                PhoneNumber = contact.PhoneNumber,
                PhoneNumber2 = contact.PhoneNumber2,
                JobTitle = contact.JobTitle,
                Comment = contact.Comment,
                Comment2 = contact.Comment2,
                IsPrimary = contact.IsPrimary,
                StatusId = contact.StatusId,
                PartnerId = contact.PartnerId,
                CreatedDate = contact.CreatedDate,
                UpdatedDate = contact.UpdatedDate
            };
        }

        public async Task<bool> DeleteAsync(int id)
        {
            try
            {
                var contact = await _context.Contacts.FindAsync(id);
                if (contact == null)
                {
                    _logger.LogWarning("Contact with ID {Id} not found for deletion", id);
                    return false;
                }

                _context.Contacts.Remove(contact);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Contact deleted with ID {ContactId}", id);
                return true;
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error deleting contact with ID {Id}", id);
                throw; // Let the controller handle the response
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error deleting contact with ID {Id}", id);
                throw;
            }
        }


        public async Task<(List<ContactDto> Contacts, int TotalCount)> GetPagedAsync(int pageNumber, int pageSize, string searchTerm, string filter = "")
        {
            var query = _context.Contacts
                .Include(c => c.Status)
                .AsQueryable();

            if (!string.IsNullOrEmpty(searchTerm))
            {
                searchTerm = searchTerm.ToLower();
                query = query.Where(c => 
                    (c.FirstName != null && c.FirstName.ToLower().Contains(searchTerm)) ||
                    (c.LastName != null && c.LastName.ToLower().Contains(searchTerm)) ||
                    (c.Email != null && c.Email.ToLower().Contains(searchTerm)) ||
                    (c.PhoneNumber != null && c.PhoneNumber.Contains(searchTerm)));
            }

            if (!string.IsNullOrEmpty(filter))
            {
                if (filter == "active")
                    query = query.Where(c => c.StatusId == 1); // Adjust based on StatusId for "Aktív"
                else if (filter == "inactive")
                    query = query.Where(c => c.StatusId != 1);
            }

            var totalCount = await query.CountAsync();

            var contacts = await query
                .OrderByDescending(c => c.UpdatedDate ?? c.CreatedDate) // Sort by UpdatedDate DESC, fallback to CreatedDate
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var dtos = contacts.Select(c => new ContactDto
            {
                ContactId = c.ContactId,
                FirstName = c.FirstName ?? string.Empty,
                LastName = c.LastName ?? string.Empty,
                Email = c.Email,
                PhoneNumber = c.PhoneNumber,
                PhoneNumber2 = c.PhoneNumber2,
                JobTitle = c.JobTitle,
                Comment = c.Comment,
                Comment2 = c.Comment2,
                IsPrimary = c.IsPrimary,
                StatusId = c.StatusId,
                Status = c.Status,
                PartnerId = c.PartnerId,
                CreatedDate = c.CreatedDate,
                UpdatedDate = c.UpdatedDate
            }).ToList();

            return (dtos, totalCount);
        }
    }
}