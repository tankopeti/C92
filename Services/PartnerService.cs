using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Cloud9_2.Models;
using Cloud9_2.Data;
using Microsoft.AspNetCore.Http; // Add for IHttpContextAccessor

namespace Cloud9_2.Services
{
    public interface IPartnerService
    {
        Task<PartnerDto> GetPartnerAsync(int id);
        Task<List<PartnerDto>> GetPartnersAsync(string searchTerm, string statusFilter, string sortBy, int skip, int take);
        Task<PartnerDto> CreatePartnerAsync(PartnerDto partner);
        Task<PartnerDto> UpdatePartnerAsync(int partnerId, PartnerDto partnerUpdate);
        Task<bool> DeletePartnerAsync(int partnerId);
        Task<SiteDto> AddOrUpdateSiteAsync(int partnerId, SiteDto siteDto);
        Task<bool> DeleteSiteAsync(int partnerId, int siteId);
        Task<ContactDto> AddOrUpdateContactAsync(int partnerId, ContactDto contactDto);
        Task<bool> DeleteContactAsync(int partnerId, int contactId);
    }

    public class PartnerService : IPartnerService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<PartnerService> _logger;
        private readonly IHttpContextAccessor _httpContextAccessor; // Add for user context

        public PartnerService(ApplicationDbContext context, ILogger<PartnerService> logger, IHttpContextAccessor httpContextAccessor)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
        }

        // Helper method to get current user
        private string GetCurrentUser()
        {
            return _httpContextAccessor.HttpContext?.User?.Identity?.Name ?? "System";
        }

public async Task<PartnerDto> GetPartnerAsync(int id)
{
    try
    {
        _logger.LogInformation("Fetching partner {PartnerId}", id);

        var partner = await _context.Partners
            .AsNoTracking()
            .Where(p => p.IsActive) // CSAK aktív partnerek!
            .Include(p => p.Sites)
            .Include(p => p.Contacts)
            .Include(p => p.Orders)
            .Include(p => p.Quotes)
            // .Include(p => p.Documents)
            // .ThenInclude(d => d.DocumentType)
            .Include(p => p.Status)
            .FirstOrDefaultAsync(p => p.PartnerId == id);

        if (partner == null)
        {
            _logger.LogWarning("Partner {PartnerId} not found or inactive", id);
            return null;
        }

        // Null biztonság
        partner.Sites ??= new List<Site>();
        partner.Contacts ??= new List<Contact>();
        partner.Documents ??= new List<Document>();
        partner.Orders ??= new List<Order>();
        partner.Quotes ??= new List<Quote>();

        return MapToDto(partner);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error fetching partner {PartnerId}", id);
        throw; // marad a 500, de legalább logolva van
    }
}

public async Task<List<PartnerDto>> GetPartnersAsync(string searchTerm, string statusFilter, string sortBy, int skip, int take)
        {
            try
            {
                var query = _context.Partners
                .AsNoTracking()
                        .Where(p => p.IsActive)
                        .Include(p => p.Status) // Státusz szűréshez kell
                        .Include(p => p.Sites)
                        .Include(p => p.Contacts)
                        .Include(p => p.Orders)
                        .Include(p => p.Quotes)
                        .Include(p => p.Documents).ThenInclude(d => d.DocumentType)
                        .AsQueryable();

                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    query = query.Where(p => (p.Name != null && p.Name.Contains(searchTerm)) ||
                                            (p.CompanyName != null && p.CompanyName.Contains(searchTerm)) ||
                                            (p.Email != null && p.Email.Contains(searchTerm)));
                }

                if (!string.IsNullOrWhiteSpace(statusFilter))
                {
                    // Validate statusFilter against PartnerStatuses
                    var validStatus = await _context.PartnerStatuses.AnyAsync(s => s.Name == statusFilter);
                    if (!validStatus)
                    {
                        _logger.LogWarning($"Invalid status filter: {statusFilter}");
                        // Optionally return empty list or throw exception
                        return new List<PartnerDto>();
                    }
                    query = query.Where(p => p.Status != null && p.Status.Name == statusFilter);
                    // Alternative: query = query.Where(p => p.StatusId == _context.PartnerStatuses
                    //     .Where(s => s.Name == statusFilter).Select(s => s.Id).FirstOrDefault());
                }

                string sortByLower = sortBy?.ToLower() ?? "createddate";
                query = sortByLower switch
                {
                    "partnerid" => query.OrderByDescending(p => p.PartnerId),
                    "name" => query.OrderBy(p => p.Name),
                    _ => query.OrderByDescending(p => p.CreatedDate)
                };

                var partners = await query
                    .Skip(skip)
                    .Take(take)
                    .Select(p => MapToDto(p))
                    .ToListAsync();

                _logger.LogInformation("Fetched {PartnerCount} partners with skip={Skip}, take={Take}", partners.Count, skip, take);
                return partners;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching partners with skip={Skip}, take={Take}", skip, take);
                throw;
            }
        }

        public async Task<PartnerDto> CreatePartnerAsync(PartnerDto partnerDto)
        {
            if (partnerDto == null)
            {
                _logger.LogError("CreatePartnerAsync received null partnerDto");
                throw new ArgumentNullException(nameof(partnerDto));
            }

            if (_context == null)
            {
                _logger.LogError("Database context is null for CreatePartnerAsync");
                throw new InvalidOperationException("Adatbázis kapcsolat nem érhető el");
            }

            _logger.LogInformation("Validating partner: Name={Name}, Email={Email}, StatusId={StatusId}",
                partnerDto.Name, partnerDto.Email, partnerDto.StatusId);

            // Validate required fields
            if (string.IsNullOrWhiteSpace(partnerDto.Name))
            {
                _logger.LogError("Invalid Name: Name is required");
                throw new ArgumentException("A név megadása kötelező");
            }

            // Validate StatusId
            int? statusId = partnerDto.StatusId;
            if (statusId.HasValue)
            {
                var statusExists = await _context.PartnerStatuses.AnyAsync(s => s.Id == statusId.Value);
                if (!statusExists)
                {
                    _logger.LogError("Invalid StatusId: {StatusId}", statusId);
                    throw new ArgumentException($"Érvénytelen státusz azonosító: {statusId}");
                }
            }
            else
            {
                // Default to Prospect (Id = 3)
                var prospectStatus = await _context.PartnerStatuses
                    .FirstOrDefaultAsync(s => s.Name == "Prospect");
                statusId = prospectStatus?.Id ?? 3; // Fallback to 3
            }

            var currentUser = GetCurrentUser();
            var currentTime = DateTime.UtcNow;

            var partner = new Partner
            {
                Name = partnerDto.Name,
                Email = partnerDto.Email,
                PhoneNumber = partnerDto.PhoneNumber,
                AlternatePhone = partnerDto.AlternatePhone,
                Website = partnerDto.Website,
                CompanyName = partnerDto.CompanyName,
                TaxId = partnerDto.TaxId,
                IntTaxId = partnerDto.IntTaxId,
                Industry = partnerDto.Industry,
                AddressLine1 = partnerDto.AddressLine1,
                AddressLine2 = partnerDto.AddressLine2,
                City = partnerDto.City,
                State = partnerDto.State,
                PostalCode = partnerDto.PostalCode,
                Country = partnerDto.Country,
                StatusId = statusId,
                LastContacted = partnerDto.LastContacted,
                Notes = partnerDto.Notes,
                AssignedTo = partnerDto.AssignedTo,
                BillingContactName = partnerDto.BillingContactName,
                BillingEmail = partnerDto.BillingEmail,
                PaymentTerms = partnerDto.PaymentTerms,
                CreditLimit = partnerDto.CreditLimit,
                PreferredCurrency = partnerDto.PreferredCurrency,
                IsTaxExempt = partnerDto.IsTaxExempt ?? false,
                PartnerGroupId = partnerDto.PartnerGroupId,
                CreatedDate = currentTime,
                CreatedBy = currentUser,
                UpdatedDate = currentTime,
                UpdatedBy = currentUser
            };

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                _context.Partners.Add(partner);
                _logger.LogInformation("Saving partner: Name={Name}", partner.Name);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                var createdPartner = await _context.Partners
                    .AsNoTracking()
                    .Include(p => p.Status) // Include Status for DTO
                    .Include(p => p.Sites)
                    .Include(p => p.Contacts)
                    .FirstOrDefaultAsync(p => p.PartnerId == partner.PartnerId);

                if (createdPartner == null)
                {
                    _logger.LogError("Failed to retrieve created partner for PartnerId: {PartnerId}", partner.PartnerId);
                    throw new InvalidOperationException($"Nem sikerült lekérni a létrehozott partnert: PartnerId {partner.PartnerId}");
                }

                var resultDto = MapToDto(createdPartner);
                _logger.LogInformation("Created partner with PartnerId: {PartnerId}, Name: {Name}", createdPartner.PartnerId, createdPartner.Name);
                return resultDto;
            }
            catch (DbUpdateException ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Database error creating partner: {Message}", ex.InnerException?.Message ?? ex.Message);
                throw new InvalidOperationException($"Adatbázis hiba a partner létrehozásakor: {ex.InnerException?.Message ?? ex.Message}", ex);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Unexpected error creating partner: {Message}", ex.Message);
                throw new InvalidOperationException($"Hiba a partner létrehozásakor: {ex.Message}", ex);
            }
        }

public async Task<PartnerDto> UpdatePartnerAsync(int partnerId, PartnerDto partnerUpdate)
{
    _logger.LogInformation("UpdatePartnerAsync called for PartnerId: {PartnerId}", partnerId);

    if (partnerUpdate == null)
    {
        throw new ArgumentNullException(nameof(partnerUpdate));
    }

    // CSAK a partner rekord kell, a kapcsolódó entitások nem szükségesek frissítéskor
    var partner = await _context.Partners
        .FirstOrDefaultAsync(p => p.PartnerId == partnerId);

    if (partner == null)
    {
        _logger.LogWarning("Partner not found for PartnerId: {PartnerId}", partnerId);
        return null;
    }

    using var transaction = await _context.Database.BeginTransactionAsync();
    try
    {
        // Frissítendő mezők
        partner.Name = partnerUpdate.Name ?? partner.Name;
        partner.Email = partnerUpdate.Email ?? partner.Email;
        partner.PhoneNumber = partnerUpdate.PhoneNumber ?? partner.PhoneNumber;
        partner.AlternatePhone = partnerUpdate.AlternatePhone ?? partner.AlternatePhone;
        partner.Website = partnerUpdate.Website ?? partner.Website;
        partner.CompanyName = partnerUpdate.CompanyName ?? partner.CompanyName;
        partner.TaxId = partnerUpdate.TaxId ?? partner.TaxId;
        partner.AddressLine1 = partnerUpdate.AddressLine1 ?? partner.AddressLine1;
        partner.AddressLine2 = partnerUpdate.AddressLine2 ?? partner.AddressLine2;
        partner.City = partnerUpdate.City ?? partner.City;
        partner.State = partnerUpdate.State ?? partner.State;
        partner.PostalCode = partnerUpdate.PostalCode ?? partner.PostalCode;
        partner.Country = partnerUpdate.Country ?? partner.Country;
        partner.Notes = partnerUpdate.Notes ?? partner.Notes;

        // Státusz csak ha változott
        if (partnerUpdate.StatusId.HasValue)
        {
            var statusExists = await _context.PartnerStatuses.AnyAsync(s => s.Id == partnerUpdate.StatusId.Value);
            if (!statusExists)
            {
                throw new ArgumentException($"Érvénytelen státusz azonosító: {partnerUpdate.StatusId}");
            }
            partner.StatusId = partnerUpdate.StatusId.Value;
        }

        partner.UpdatedBy = GetCurrentUser();
        partner.UpdatedDate = DateTime.UtcNow;

        if (string.IsNullOrWhiteSpace(partner.Name))
        {
            throw new ArgumentException("A név megadása kötelező");
        }

        await _context.SaveChangesAsync();
        await transaction.CommitAsync();

        // Friss DTO visszaadása (most már Include-okkal, mert olvasunk)
        var refreshedPartner = await _context.Partners
            .AsNoTracking()
            .Include(p => p.Sites)
            .Include(p => p.Contacts)
            .Include(p => p.Status)
            .FirstOrDefaultAsync(p => p.PartnerId == partnerId);

        return MapToDto(refreshedPartner!);
    }
    catch (Exception ex)
    {
        await transaction.RollbackAsync();
        _logger.LogError(ex, "Error updating partner for PartnerId: {PartnerId}", partnerId);
        throw;
    }
}
        public async Task<ContactDto> AddOrUpdateContactAsync(int partnerId, ContactDto contactDto)
        {
            _logger.LogInformation("AddOrUpdateContactAsync called for PartnerId: {PartnerId}, ContactId: {ContactId}", partnerId, contactDto.ContactId);

            var partner = await _context.Partners
                .Include(p => p.Contacts)
                .FirstOrDefaultAsync(p => p.PartnerId == partnerId);

            if (partner == null)
            {
                _logger.LogWarning("Partner not found for PartnerId: {PartnerId}", partnerId);
                throw new ArgumentException($"Partner {partnerId} not found");
            }

            Contact contact;
            if (contactDto.ContactId == 0)
            {
                // New contact
                contact = new Contact
                {
                    FirstName = contactDto.FirstName ?? string.Empty,
                    LastName = contactDto.LastName ?? string.Empty,
                    Email = contactDto.Email,
                    PhoneNumber = contactDto.PhoneNumber,
                    JobTitle = contactDto.JobTitle,
                    Comment = contactDto.Comment,
                    IsPrimary = contactDto.IsPrimary,
                    PartnerId = partnerId
                };
                partner.Contacts.Add(contact);
            }
            else
            {
                // Update existing
                contact = partner.Contacts.FirstOrDefault(c => c.ContactId == contactDto.ContactId);
                if (contact == null)
                {
                    _logger.LogWarning("Contact not found for ContactId: {ContactId}", contactDto.ContactId);
                    throw new ArgumentException($"Contact {contactDto.ContactId} not found for Partner {partnerId}");
                }

                contact.FirstName = contactDto.FirstName ?? string.Empty;
                contact.LastName = contactDto.LastName ?? string.Empty;
                contact.Email = contactDto.Email;
                contact.PhoneNumber = contactDto.PhoneNumber;
                contact.JobTitle = contactDto.JobTitle;
                contact.Comment = contactDto.Comment;
                contact.IsPrimary = contactDto.IsPrimary;
            }

            await _context.SaveChangesAsync();

            return new ContactDto
            {
                ContactId = contact.ContactId,
                FirstName = contact.FirstName,
                LastName = contact.LastName,
                Email = contact.Email,
                PhoneNumber = contact.PhoneNumber,
                JobTitle = contact.JobTitle,
                Comment = contact.Comment,
                IsPrimary = contact.IsPrimary
            };
        }

        public async Task<bool> DeleteContactAsync(int partnerId, int contactId)
        {
            _logger.LogInformation("DeleteContactAsync called for PartnerId: {PartnerId}, ContactId: {ContactId}", partnerId, contactId);

            var contact = await _context.Contacts
                .FirstOrDefaultAsync(c => c.ContactId == contactId && c.PartnerId == partnerId);

            if (contact == null)
            {
                _logger.LogWarning("Contact not found for ContactId: {ContactId}", contactId);
                return false;
            }

            _context.Contacts.Remove(contact);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<SiteDto> AddOrUpdateSiteAsync(int partnerId, SiteDto siteDto)
        {
            _logger.LogInformation("AddOrUpdateSiteAsync called for PartnerId: {PartnerId}, SiteId: {SiteId}", partnerId, siteDto.SiteId);

            var partner = await _context.Partners
                .Include(p => p.Sites)
                .FirstOrDefaultAsync(p => p.PartnerId == partnerId);

            if (partner == null)
            {
                _logger.LogWarning("Partner not found for PartnerId: {PartnerId}", partnerId);
                throw new ArgumentException($"Partner {partnerId} not found");
            }

            Site site;
            if (siteDto.SiteId == 0)
            {
                site = new Site
                {
                    SiteName = siteDto.SiteName,
                    AddressLine1 = siteDto.AddressLine1,
                    AddressLine2 = siteDto.AddressLine2,
                    City = siteDto.City,
                    State = siteDto.State,
                    PostalCode = siteDto.PostalCode,
                    Country = siteDto.Country,
                    IsPrimary = siteDto.IsPrimary,
                    PartnerId = partnerId,
                    CreatedDate = DateTime.UtcNow,
                    CreatedById = GetCurrentUser(),
                    LastModifiedDate = DateTime.UtcNow,
                    LastModifiedById = GetCurrentUser()
                };
                partner.Sites.Add(site);
            }
            else
            {
                site = partner.Sites.FirstOrDefault(s => s.SiteId == siteDto.SiteId);
                if (site == null)
                {
                    _logger.LogWarning("Site not found for SiteId: {SiteId}", siteDto.SiteId);
                    throw new ArgumentException($"Site {siteDto.SiteId} not found for Partner {partnerId}");
                }

                site.SiteName = siteDto.SiteName;
                site.AddressLine1 = siteDto.AddressLine1;
                site.AddressLine2 = siteDto.AddressLine2;
                site.City = siteDto.City;
                site.State = siteDto.State;
                site.PostalCode = siteDto.PostalCode;
                site.Country = siteDto.Country;
                site.IsPrimary = siteDto.IsPrimary;
                site.LastModifiedDate = DateTime.UtcNow;
                site.LastModifiedById = GetCurrentUser();
            }

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error saving site for PartnerId: {PartnerId}, SiteId: {SiteId}", partnerId, siteDto.SiteId);
                throw;
            }

            return new SiteDto
            {
                SiteId = site.SiteId,
                SiteName = site.SiteName,
                AddressLine1 = site.AddressLine1,
                AddressLine2 = site.AddressLine2,
                City = site.City,
                State = site.State,
                PostalCode = site.PostalCode,
                Country = site.Country,
                IsPrimary = site.IsPrimary
            };
        }

        public async Task<bool> DeleteSiteAsync(int partnerId, int siteId)
        {
            try
            {
                var site = await _context.Sites
                    .FirstOrDefaultAsync(s => s.SiteId == siteId && s.PartnerId == partnerId);

                if (site == null)
                {
                    _logger.LogWarning("DeleteSiteAsync: Site {SiteId} not found for Partner {PartnerId}", siteId, partnerId);
                    return false;
                }

                // Already soft-deleted? → success (idempotent)
                if (!site.IsActive)
                {
                    _logger.LogInformation("Site {SiteId} is already soft-deleted", siteId);
                    return true;
                }

                // Soft delete
                site.IsActive = false;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Site {SiteId} (Partner {PartnerId}) soft-deleted by {User}",
                    siteId, partnerId, GetCurrentUser());

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error soft-deleting Site {SiteId} for Partner {PartnerId}", siteId, partnerId);
                return false;
            }
        }

        public async Task<bool> DeletePartnerAsync(int partnerId)
        {
            try
            {
                // We only need the partner row itself – no need to load all related entities
                var partner = await _context.Partners
                    .FirstOrDefaultAsync(p => p.PartnerId == partnerId);

                if (partner == null)
                {
                    _logger.LogWarning("DeletePartnerAsync: Partner with ID {PartnerId} not found.", partnerId);
                    return false;
                }

                // If already soft-deleted, consider it success (idempotent)
                if (!partner.IsActive)
                {
                    _logger.LogInformation("DeletePartnerAsync: Partner {PartnerId} is already soft-deleted.", partnerId);
                    return true;
                }

                // Perform soft delete
                partner.IsActive = false;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Partner {PartnerId} was successfully soft-deleted by {User}", partnerId, GetCurrentUser());
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during soft-delete of Partner {PartnerId}", partnerId);
                return false; // Still return false on unexpected error
            }
        }


        private PartnerDto MapToDto(Partner p)
        {
            return new PartnerDto
            {
                PartnerId = p.PartnerId,
                Name = p.Name,
                Email = p.Email,
                PhoneNumber = p.PhoneNumber,
                AlternatePhone = p.AlternatePhone,
                Website = p.Website,
                CompanyName = p.CompanyName,
                TaxId = p.TaxId,
                IntTaxId = p.IntTaxId,
                Industry = p.Industry,
                AddressLine1 = p.AddressLine1,
                AddressLine2 = p.AddressLine2,
                City = p.City,
                State = p.State,
                PostalCode = p.PostalCode,
                Country = p.Country,
                LastContacted = p.LastContacted,
                Notes = p.Notes,
                AssignedTo = p.AssignedTo,
                BillingContactName = p.BillingContactName,
                BillingEmail = p.BillingEmail,
                PaymentTerms = p.PaymentTerms,
                CreditLimit = p.CreditLimit,
                PreferredCurrency = p.PreferredCurrency,
                IsTaxExempt = p.IsTaxExempt,
                PartnerGroupId = p.PartnerGroupId,
                StatusId = p.StatusId,
                Status = p.Status,
                Sites = p.Sites.Select(s => new SiteDto
                {
                    SiteId = s.SiteId,
                    SiteName = s.SiteName,
                    AddressLine1 = s.AddressLine1,
                    AddressLine2 = s.AddressLine2,
                    City = s.City,
                    State = s.State,
                    PostalCode = s.PostalCode,
                    Country = s.Country,
                    IsPrimary = s.IsPrimary,
                    ContactPerson1 = s.ContactPerson1,
                    ContactPerson2 = s.ContactPerson2,
                    ContactPerson3 = s.ContactPerson3,
                    Comment1 = s.Comment1,
                    Comment2 = s.Comment2,
                    StatusId = s.StatusId,
                }).ToList(),
                Contacts = p.Contacts.Select(c => new ContactDto
                {
                    ContactId = c.ContactId,
                    FirstName = c.FirstName,
                    LastName = c.LastName,
                    Email = c.Email,
                    PhoneNumber = c.PhoneNumber,
                    PhoneNumber2 = c.PhoneNumber2,
                    JobTitle = c.JobTitle,
                    Comment = c.Comment,
                    Comment2 = c.Comment2,
                    IsPrimary = c.IsPrimary,
                    StatusId = c.StatusId,
                    Status = c.Status != null ? new Status { Id = c.Status.Id, Name = c.Status.Name } : null,
                    CreatedDate = c.CreatedDate,
                    UpdatedDate = c.UpdatedDate
                }).ToList(),
                Documents = p.Documents.Select(d => new DocumentDto
                {
                    DocumentId = d.DocumentId,
                    FileName = d.FileName,
                    FilePath = d.FilePath,
                    UploadDate = d.UploadDate
                }).ToList()
            };
        }

    }
}