using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Cloud9_2.Data;
using Cloud9_2.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Cloud9_2.Services
{
    public class PartnerService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<PartnerService> _logger;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public PartnerService(
            ApplicationDbContext context,
            ILogger<PartnerService> logger,
            IHttpContextAccessor httpContextAccessor)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
        }

        private string GetCurrentUser()
            => _httpContextAccessor.HttpContext?.User?.Identity?.Name ?? "System";

        private string? GetCurrentUserId()
            => _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);

        // --------------------------------------------------------------------
        // LISTA: api/Partners?page=..&pageSize=..&filters...
        // --------------------------------------------------------------------
        public async Task<(List<PartnerDto> Items, int TotalCount)> GetPartnersAsync(
    string? search = null,
    string? name = null,
    string? taxId = null,
    int? statusId = null,
    int? gfoId = null,
    int? partnerTypeId = null,
    string? partnerCode = null,
    string? ownId = null,
    string? city = null,
    string? postalCode = null,
    string? emailDomain = null,
    bool activeOnly = true,
    int page = 1,
    int pageSize = 50)
        {
            // Validálás
            if (page < 1) page = 1;
            if (pageSize < 10) pageSize = 50;
            if (pageSize > 200) pageSize = 200;

            try
            {
                var query = _context.Partners
                    .AsNoTracking()
                    .Include(p => p.Status)
                    .Include(p => p.GFO)              // kell a listában a GFOName-hez
                                                      // .Include(p => p.PartnerType)   // csak akkor, ha van nav propod; nálad most nincs a PartnerType-hoz
                    .Where(p => !activeOnly || p.IsActive)
                    .AsQueryable();


                // Quick search
                if (!string.IsNullOrWhiteSpace(search))
                {
                    var term = search.Trim().ToLowerInvariant();
                    query = query.Where(p =>
                        EF.Functions.Like((p.Name ?? "").ToLower(), $"%{term}%") ||
                        EF.Functions.Like((p.CompanyName ?? "").ToLower(), $"%{term}%") ||
                        EF.Functions.Like((p.Email ?? "").ToLower(), $"%{term}%") ||
                        EF.Functions.Like((p.TaxId ?? ""), $"%{term}%") ||
                        EF.Functions.Like((p.City ?? "").ToLower(), $"%{term}%")
                    );
                }

                // Advanced filters
                if (!string.IsNullOrWhiteSpace(name))
                {
                    var nameTerm = name.Trim().ToLowerInvariant();
                    query = query.Where(p =>
                        EF.Functions.Like((p.Name ?? "").ToLower(), $"%{nameTerm}%") ||
                        EF.Functions.Like((p.CompanyName ?? "").ToLower(), $"%{nameTerm}%")
                    );
                }

                // PartnerCode
                if (!string.IsNullOrWhiteSpace(partnerCode))
                {
                    var code = partnerCode.Trim();
                    query = query.Where(p => p.PartnerCode != null && EF.Functions.Like(p.PartnerCode, $"%{code}%"));
                }

                // OwnId
                if (!string.IsNullOrWhiteSpace(ownId))
                {
                    var oid = ownId.Trim();
                    query = query.Where(p => p.OwnId != null && EF.Functions.Like(p.OwnId, $"%{oid}%"));
                }

                // GFOId
                if (gfoId.HasValue)
                {
                    query = query.Where(p => p.GFOId == gfoId.Value);
                }

                // PartnerTypeId
                if (partnerTypeId.HasValue)
                {
                    query = query.Where(p => p.PartnerTypeId == partnerTypeId.Value);
                }


                if (!string.IsNullOrWhiteSpace(taxId))
                {
                    var t = taxId.Trim();
                    query = query.Where(p => p.TaxId != null && EF.Functions.Like(p.TaxId, $"%{t}%"));
                }

                if (statusId.HasValue)
                {
                    query = query.Where(p => p.StatusId == statusId.Value);
                }

                if (!string.IsNullOrWhiteSpace(city))
                {
                    var cityTerm = city.Trim().ToLowerInvariant();
                    query = query.Where(p => p.City != null && EF.Functions.Like(p.City.ToLower(), $"%{cityTerm}%"));
                }

                if (!string.IsNullOrWhiteSpace(postalCode))
                {
                    var pc = postalCode.Trim();
                    query = query.Where(p => p.PostalCode != null && EF.Functions.Like(p.PostalCode, $"%{pc}%"));

                }

                // Ha vissza akarod kapcsolni:
                // if (!string.IsNullOrWhiteSpace(emailDomain))
                // {
                //     var dom = emailDomain.Trim();
                //     query = query.Where(p => p.Email != null && p.Email.EndsWith(dom));
                // }

                var totalCount = await query.CountAsync();

                var items = await query
                    .OrderByDescending(p => p.PartnerId)
                    .ThenByDescending(p => p.CreatedDate)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(p => new PartnerDto
                    {
                        PartnerId = p.PartnerId,
                        Name = p.Name,
                        Email = p.Email,
                        PhoneNumber = p.PhoneNumber,
                        AlternatePhone = p.AlternatePhone,
                        Website = p.Website,
                        CompanyName = p.CompanyName,
                        ShortName = p.ShortName,
                        PartnerCode = p.PartnerCode,
                        OwnId = p.OwnId,
                        GFOId = p.GFOId,
                        GFOName = p.GFO != null ? p.GFO.GFOName : null,
                        TaxId = p.TaxId,
                        IndividualTaxId = p.IndividualTaxId,
                        IntTaxId = p.IntTaxId,
                        Industry = p.Industry,
                        AddressLine1 = p.AddressLine1,
                        AddressLine2 = p.AddressLine2,
                        City = p.City,
                        State = p.State,
                        PostalCode = p.PostalCode,
                        Country = p.Country,
                        StatusId = p.StatusId,
                        Status = p.Status != null
                            ? new StatusDto
                            {
                                Id = p.Status.Id,
                                Name = p.Status.Name ?? "N/A",
                                Color = p.Status.Color ?? "#6c757d"
                            }
                            : null,
                        LastContacted = p.LastContacted,
                        Notes = p.Notes,
                        AssignedTo = p.AssignedTo,
                        BillingContactName = p.BillingContactName,
                        BillingEmail = p.BillingEmail,
                        PaymentTerms = p.PaymentTerms,
                        CreditLimit = p.CreditLimit,
                        PreferredCurrency = p.PreferredCurrency,
                        Comment1 = p.Comment1,
                        Comment2 = p.Comment2,
                        IsTaxExempt = p.IsTaxExempt,
                        PartnerGroupId = p.PartnerGroupId,
                        PartnerTypeId = p.PartnerTypeId,
                        IsActive = p.IsActive
                    })
                    .ToListAsync();

                return (items, totalCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetPartnersAsync failed (page={Page}, pageSize={PageSize})", page, pageSize);
                throw;
            }
        }

        // --------------------------------------------------------------------
        // TomSelect: api/Partners/select?search=...
        // --------------------------------------------------------------------
        public async Task<List<object>> GetPartnersForSelectAsync(string search = "")
        {
            try
            {
                const int MaxResults = 300;

                var query = _context.Partners
                    .AsNoTracking()
                    .Where(p => p.IsActive)
                    .AsQueryable();

                if (!string.IsNullOrWhiteSpace(search))
                {
                    var term = search.Trim().ToLowerInvariant();
                    query = query.Where(p =>
                        EF.Functions.Like((p.NameTrim ?? "").ToLower(), $"%{term}%") ||
                        EF.Functions.Like((p.CompanyNameTrim ?? "").ToLower(), $"%{term}%") ||
                        EF.Functions.Like((p.TaxIdTrim ?? ""), $"%{term}%") ||
                        EF.Functions.Like((p.City ?? "").ToLower(), $"%{term}%") ||
                        EF.Functions.Like((p.Email ?? "").ToLower(), $"%{term}%")
                    );
                }

                var partners = await query
                    .OrderByDescending(p => p.PartnerId)
                    .Take(MaxResults)
                    .Select(p => (object)new
                    {
                        id = p.PartnerId,
                        text = string.IsNullOrWhiteSpace(p.CompanyName)
                            ? p.Name ?? "Névtelen partner"
                            : $"{p.CompanyName} ({p.Name ?? "nincs magánnév"})",
                        partnerName = string.IsNullOrWhiteSpace(p.CompanyName) ? p.Name : p.CompanyName,
                        partnerDetails =
                            $"{(string.IsNullOrWhiteSpace(p.CompanyName) ? p.Name : p.CompanyName)} " +
                            $"{(string.IsNullOrWhiteSpace(p.City) ? "" : $"– {p.City}")}" +
                            $"{(string.IsNullOrWhiteSpace(p.TaxId) ? "" : $" ({p.TaxId})")}".Trim()
                    })
                    .ToListAsync();

                return partners;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetPartnersForSelectAsync failed (search='{Search}')", search);
                throw;
            }
        }

        // --------------------------------------------------------------------
        // Statusok: api/Partners/statuses
        // --------------------------------------------------------------------
        public async Task<List<object>> GetStatusesAsync()
        {
            try
            {
                var statuses = await _context.PartnerStatuses
                    .AsNoTracking()
                    .OrderBy(s => s.Name)
                    .Select(s => (object)new { s.Id, s.Name })
                    .ToListAsync();

                return statuses;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetStatusesAsync failed");
                throw;
            }
        }

        // --------------------------------------------------------------------
        // History: api/Partners/{id}/history  (interceptor -> AuditLogs táblában)
        // --------------------------------------------------------------------
        public async Task<List<AuditLogDto>> GetPartnerHistoryAsync(int partnerId)
        {
            try
            {
                var history = await _context.AuditLogs
                    .AsNoTracking()
                    .Where(a => a.EntityType == "Partner" && a.EntityId == partnerId)
                    .OrderByDescending(a => a.ChangedAt)
                    .Select(a => new AuditLogDto
                    {
                        Action = a.Action,
                        ChangedByName = a.ChangedByName,
                        ChangedAt = a.ChangedAt,
                        Changes = a.Changes
                    })
                    .ToListAsync();

                return history;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetPartnerHistoryAsync failed (partnerId={PartnerId})", partnerId);
                throw;
            }
        }

        // --------------------------------------------------------------------
        // GetPartner (részletes)
        // --------------------------------------------------------------------
        public async Task<PartnerDto?> GetPartnerAsync(int id)
        {
            try
            {
                var partner = await _context.Partners
                    .AsNoTracking()
                    .Where(p => p.IsActive)
                    .Include(p => p.Sites)
                    .Include(p => p.Contacts)
                    .Include(p => p.Orders)
                    .Include(p => p.Quotes)
                    .Include(p => p.Documents).ThenInclude(d => d.DocumentType)
                    .Include(p => p.Status)
                    .FirstOrDefaultAsync(p => p.PartnerId == id);

                if (partner == null) return null;

                partner.Sites ??= new List<Site>();
                partner.Contacts ??= new List<Contact>();
                partner.Documents ??= new List<Document>();
                partner.Orders ??= new List<Order>();
                partner.Quotes ??= new List<Quote>();

                return MapToDto(partner);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetPartnerAsync failed (id={Id})", id);
                throw;
            }
        }

        // --------------------------------------------------------------------
        // Create
        // --------------------------------------------------------------------
        public async Task<PartnerDto> CreatePartnerAsync(PartnerDto dto)
        {
            if (dto == null) throw new ArgumentNullException(nameof(dto));
            if (string.IsNullOrWhiteSpace(dto.Name)) throw new ArgumentException("A név megadása kötelező");

            int? statusId = dto.StatusId;
            if (statusId.HasValue)
            {
                var ok = await _context.PartnerStatuses.AnyAsync(s => s.Id == statusId.Value);
                if (!ok) throw new ArgumentException($"Érvénytelen státusz azonosító: {statusId}");
            }
            else
            {
                var prospect = await _context.PartnerStatuses.FirstOrDefaultAsync(s => s.Name == "Prospect");
                statusId = prospect?.Id ?? 3;
            }

            var now = DateTime.UtcNow;
            var user = GetCurrentUser();

            var partner = new Partner
            {
                PartnerId = 0,
                Name = dto.Name,
                Email = dto.Email,
                PhoneNumber = dto.PhoneNumber,
                AlternatePhone = dto.AlternatePhone,
                Website = dto.Website,
                CompanyName = dto.CompanyName,
                ShortName = dto.ShortName,
                PartnerCode = dto.PartnerCode,
                OwnId = dto.OwnId,
                GFOId = dto.GFOId,
                TaxId = dto.TaxId,
                IndividualTaxId = dto.IndividualTaxId,
                IntTaxId = dto.IntTaxId,
                Industry = dto.Industry,
                AddressLine1 = dto.AddressLine1,
                AddressLine2 = dto.AddressLine2,
                City = dto.City,
                State = dto.State,
                PostalCode = dto.PostalCode,
                Country = dto.Country,
                StatusId = statusId,
                LastContacted = dto.LastContacted,
                Notes = dto.Notes,
                AssignedTo = dto.AssignedTo,
                BillingContactName = dto.BillingContactName,
                BillingEmail = dto.BillingEmail,
                PaymentTerms = dto.PaymentTerms,
                CreditLimit = dto.CreditLimit,
                PreferredCurrency = dto.PreferredCurrency,
                Comment1 = dto.Comment1,
                Comment2 = dto.Comment2,
                IsTaxExempt = dto.IsTaxExempt ?? false,
                PartnerGroupId = dto.PartnerGroupId,
                PartnerTypeId = dto.PartnerTypeId,
                IsActive = dto.IsActive,

                CreatedDate = now,
                CreatedBy = user,
                UpdatedDate = now,
                UpdatedBy = user
            };

            using var tx = await _context.Database.BeginTransactionAsync();
            try
            {
                _context.Partners.Add(partner);
                await _context.SaveChangesAsync();
                await tx.CommitAsync();

                var created = await _context.Partners
                    .AsNoTracking()
                    .Include(p => p.Status)
                    .Include(p => p.Sites)
                    .Include(p => p.Contacts)
                    .FirstAsync(p => p.PartnerId == partner.PartnerId);

                return MapToDto(created);
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }

        // --------------------------------------------------------------------
        // Update
        // --------------------------------------------------------------------
        public async Task<PartnerDto?> UpdatePartnerAsync(int partnerId, PartnerDto update)
        {
            if (update == null) throw new ArgumentNullException(nameof(update));

            var partner = await _context.Partners.FirstOrDefaultAsync(p => p.PartnerId == partnerId);
            if (partner == null) return null;

            using var tx = await _context.Database.BeginTransactionAsync();
            try
            {
                partner.Name = update.Name ?? partner.Name;

                partner.Email = update.Email ?? partner.Email;
                partner.PhoneNumber = update.PhoneNumber ?? partner.PhoneNumber;
                partner.AlternatePhone = update.AlternatePhone ?? partner.AlternatePhone;
                partner.Website = update.Website ?? partner.Website;

                partner.CompanyName = update.CompanyName ?? partner.CompanyName;
                partner.ShortName = update.ShortName ?? partner.ShortName;

                partner.PartnerCode = update.PartnerCode ?? partner.PartnerCode;
                partner.OwnId = update.OwnId ?? partner.OwnId;
                partner.GFOId = update.GFOId ?? partner.GFOId;

                partner.TaxId = update.TaxId ?? partner.TaxId;
                partner.IndividualTaxId = update.IndividualTaxId ?? partner.IndividualTaxId;
                partner.IntTaxId = update.IntTaxId ?? partner.IntTaxId;

                partner.Industry = update.Industry ?? partner.Industry;

                partner.AddressLine1 = update.AddressLine1 ?? partner.AddressLine1;
                partner.AddressLine2 = update.AddressLine2 ?? partner.AddressLine2;
                partner.City = update.City ?? partner.City;
                partner.State = update.State ?? partner.State;
                partner.PostalCode = update.PostalCode ?? partner.PostalCode;
                partner.Country = update.Country ?? partner.Country;

                partner.LastContacted = update.LastContacted ?? partner.LastContacted;
                partner.Notes = update.Notes ?? partner.Notes;
                partner.AssignedTo = update.AssignedTo ?? partner.AssignedTo;

                partner.BillingContactName = update.BillingContactName ?? partner.BillingContactName;
                partner.BillingEmail = update.BillingEmail ?? partner.BillingEmail;

                partner.PaymentTerms = update.PaymentTerms ?? partner.PaymentTerms;
                partner.CreditLimit = update.CreditLimit ?? partner.CreditLimit;
                partner.PreferredCurrency = update.PreferredCurrency ?? partner.PreferredCurrency;

                partner.Comment1 = update.Comment1 ?? partner.Comment1;
                partner.Comment2 = update.Comment2 ?? partner.Comment2;

                partner.IsTaxExempt = update.IsTaxExempt ?? partner.IsTaxExempt;

                partner.PartnerGroupId = update.PartnerGroupId ?? partner.PartnerGroupId;
                partner.PartnerTypeId = update.PartnerTypeId ?? partner.PartnerTypeId;

                partner.IsActive = update.IsActive;

                if (update.StatusId.HasValue)
                {
                    var ok = await _context.PartnerStatuses.AnyAsync(s => s.Id == update.StatusId.Value);
                    if (!ok) throw new ArgumentException($"Érvénytelen státusz azonosító: {update.StatusId}");
                    partner.StatusId = update.StatusId.Value;
                }

                if (string.IsNullOrWhiteSpace(partner.Name))
                    throw new ArgumentException("A név megadása kötelező");

                partner.UpdatedBy = GetCurrentUser();
                partner.UpdatedDate = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                await tx.CommitAsync();

                var refreshed = await _context.Partners
                    .AsNoTracking()
                    .Include(p => p.Status)
                    .Include(p => p.Sites)
                    .Include(p => p.Contacts)
                    .Include(p => p.Documents)
                    .FirstAsync(p => p.PartnerId == partnerId);

                return MapToDto(refreshed);
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }

        // --------------------------------------------------------------------
        // Copy
        // --------------------------------------------------------------------
        public async Task<PartnerDto?> CopyPartnerAsync(int partnerId)
        {
            var existing = await GetPartnerAsync(partnerId);
            if (existing == null) return null;

            var copy = new PartnerDto
            {
                Name = (existing.Name ?? "") + " (másolat)",
                Email = existing.Email,
                PhoneNumber = existing.PhoneNumber,
                AlternatePhone = existing.AlternatePhone,
                Website = existing.Website,
                CompanyName = existing.CompanyName,
                ShortName = existing.ShortName,
                PartnerCode = existing.PartnerCode,
                OwnId = existing.OwnId,
                GFOId = existing.GFOId,
                TaxId = existing.TaxId,
                IndividualTaxId = existing.IndividualTaxId,
                IntTaxId = existing.IntTaxId,
                Industry = existing.Industry,
                AddressLine1 = existing.AddressLine1,
                AddressLine2 = existing.AddressLine2,
                City = existing.City,
                State = existing.State,
                PostalCode = existing.PostalCode,
                Country = existing.Country,
                StatusId = existing.StatusId,
                LastContacted = existing.LastContacted,
                Notes = (existing.Notes ?? "") +
                        (string.IsNullOrEmpty(existing.Notes) ? "" : "\n\n") +
                        $"--- Másolat a(z) {existing.PartnerId} azonosítójú partnerből ({DateTime.Now:yyyy-MM-dd HH:mm}) ---",
                AssignedTo = existing.AssignedTo,
                BillingContactName = existing.BillingContactName,
                BillingEmail = existing.BillingEmail,
                PaymentTerms = existing.PaymentTerms,
                CreditLimit = existing.CreditLimit,
                PreferredCurrency = existing.PreferredCurrency,
                IsTaxExempt = existing.IsTaxExempt,
                PartnerGroupId = existing.PartnerGroupId,
                PartnerTypeId = existing.PartnerTypeId,
                IsActive = existing.IsActive,
                Comment1 = existing.Comment1,
                Comment2 = existing.Comment2,

                // Relációk: itt most üresen hagyjuk (safe default)
                Sites = new List<SiteDto>(),
                Contacts = new List<ContactDto>(),
                Documents = new List<DocumentDto>()
            };

            return await CreatePartnerAsync(copy);
        }

        // --------------------------------------------------------------------
        // Delete (soft)
        // --------------------------------------------------------------------
        public async Task<bool> DeletePartnerAsync(int partnerId)
        {
            try
            {
                var partner = await _context.Partners.FirstOrDefaultAsync(p => p.PartnerId == partnerId);
                if (partner == null) return false;

                if (!partner.IsActive) return true;

                partner.IsActive = false;
                partner.UpdatedBy = GetCurrentUser();
                partner.UpdatedDate = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "DeletePartnerAsync failed (partnerId={PartnerId})", partnerId);
                return false;
            }
        }

        public async Task<SiteDto> AddOrUpdateSiteAsync(int partnerId, SiteDto siteDto)
        {
            if (siteDto == null) throw new ArgumentNullException(nameof(siteDto));

            // Bejelentkezett user Id – lehet null
            var userId = GetCurrentUserId();

            // CREATE
            if (siteDto.SiteId == 0)
            {
                var targetPartnerId = siteDto.PartnerId != 0 ? siteDto.PartnerId : partnerId;

                var partner = await _context.Partners
                    .FirstOrDefaultAsync(p => p.PartnerId == targetPartnerId);

                if (partner == null)
                    throw new ArgumentException($"Partner {targetPartnerId} not found");

                var site = new Site
                {
                    PartnerId = targetPartnerId,

                    SiteName = siteDto.SiteName,
                    AddressLine1 = siteDto.AddressLine1,
                    AddressLine2 = siteDto.AddressLine2,
                    City = siteDto.City,
                    State = siteDto.State,
                    PostalCode = siteDto.PostalCode,
                    Country = siteDto.Country,

                    IsPrimary = siteDto.IsPrimary,
                    StatusId = siteDto.StatusId,

                    ContactPerson1 = siteDto.ContactPerson1,
                    ContactPerson2 = siteDto.ContactPerson2,
                    ContactPerson3 = siteDto.ContactPerson3,
                    Comment1 = siteDto.Comment1,
                    Comment2 = siteDto.Comment2,

                    Phone1 = siteDto.Phone1,
                    Phone2 = siteDto.Phone2,
                    Phone3 = siteDto.Phone3,
                    MobilePhone1 = siteDto.MobilePhone1,
                    MobilePhone2 = siteDto.MobilePhone2,
                    MobilePhone3 = siteDto.MobilePhone3,
                    eMail1 = siteDto.eMail1,
                    eMail2 = siteDto.eMail2,
                    messagingApp1 = siteDto.messagingApp1,
                    messagingApp2 = siteDto.messagingApp2,
                    messagingApp3 = siteDto.messagingApp3,

                    IsActive = true,

                    CreatedDate = DateTime.UtcNow,
                    CreatedById = userId,
                    LastModifiedDate = DateTime.UtcNow,
                    LastModifiedById = userId
                };

                _context.Sites.Add(site);
                await _context.SaveChangesAsync();

                siteDto.SiteId = site.SiteId;
                siteDto.PartnerId = site.PartnerId;
                return siteDto;
            }

            // UPDATE
            var existing = await _context.Sites
                .FirstOrDefaultAsync(s => s.SiteId == siteDto.SiteId);

            if (existing == null)
                throw new ArgumentException($"Site {siteDto.SiteId} not found");

            // opcionális partner check (ha szeretnéd szigorítani)
            // if (partnerId != 0 && existing.PartnerId != partnerId)
            //     throw new ArgumentException($"Site {siteDto.SiteId} not found for Partner {partnerId}");

            existing.SiteName = siteDto.SiteName;
            existing.AddressLine1 = siteDto.AddressLine1;
            existing.AddressLine2 = siteDto.AddressLine2;
            existing.City = siteDto.City;
            existing.State = siteDto.State;
            existing.PostalCode = siteDto.PostalCode;
            existing.Country = siteDto.Country;

            existing.IsPrimary = siteDto.IsPrimary;
            existing.StatusId = siteDto.StatusId;

            existing.ContactPerson1 = siteDto.ContactPerson1;
            existing.ContactPerson2 = siteDto.ContactPerson2;
            existing.ContactPerson3 = siteDto.ContactPerson3;
            existing.Comment1 = siteDto.Comment1;
            existing.Comment2 = siteDto.Comment2;

            existing.Phone1 = siteDto.Phone1;
            existing.Phone2 = siteDto.Phone2;
            existing.Phone3 = siteDto.Phone3;
            existing.MobilePhone1 = siteDto.MobilePhone1;
            existing.MobilePhone2 = siteDto.MobilePhone2;
            existing.MobilePhone3 = siteDto.MobilePhone3;
            existing.eMail1 = siteDto.eMail1;
            existing.eMail2 = siteDto.eMail2;
            existing.messagingApp1 = siteDto.messagingApp1;
            existing.messagingApp2 = siteDto.messagingApp2;
            existing.messagingApp3 = siteDto.messagingApp3;

            // ha a modal küld partnerId-t és engeded a mozgatást:
            if (siteDto.PartnerId != 0)
                existing.PartnerId = siteDto.PartnerId;

            existing.LastModifiedDate = DateTime.UtcNow;
            if (!string.IsNullOrWhiteSpace(userId))
                existing.LastModifiedById = userId;

            await _context.SaveChangesAsync();

            return siteDto;
        }

        public async Task<bool> DeleteSiteAsync(int partnerId, int siteId)
        {
            try
            {
                var site = await _context.Sites
                    .FirstOrDefaultAsync(s => s.SiteId == siteId && s.PartnerId == partnerId);

                if (site == null) return false;

                // idempotens soft delete
                if (!site.IsActive) return true;

                site.IsActive = false;
                site.LastModifiedDate = DateTime.UtcNow;

                var userId = GetCurrentUserId();
                if (!string.IsNullOrWhiteSpace(userId))
                    site.LastModifiedById = userId;

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "DeleteSiteAsync failed (partnerId={PartnerId}, siteId={SiteId})", partnerId, siteId);
                return false;
            }
        }


        // --------------------------------------------------------------------
        // MapToDto (teljes)
        // --------------------------------------------------------------------
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
                ShortName = p.ShortName,
                PartnerCode = p.PartnerCode,
                OwnId = p.OwnId,
                GFOId = p.GFOId,
                TaxId = p.TaxId,
                IndividualTaxId = p.IndividualTaxId,
                IntTaxId = p.IntTaxId,
                Industry = p.Industry,
                AddressLine1 = p.AddressLine1,
                AddressLine2 = p.AddressLine2,
                City = p.City,
                State = p.State,
                PostalCode = p.PostalCode,
                Country = p.Country,
                StatusId = p.StatusId,
                Status = p.Status != null
                    ? new StatusDto { Id = p.Status.Id, Name = p.Status.Name, Color = p.Status.Color }
                    : null,
                LastContacted = p.LastContacted,
                Notes = p.Notes,
                AssignedTo = p.AssignedTo,
                BillingContactName = p.BillingContactName,
                BillingEmail = p.BillingEmail,
                PaymentTerms = p.PaymentTerms,
                CreditLimit = p.CreditLimit,
                PreferredCurrency = p.PreferredCurrency,
                Comment1 = p.Comment1,
                Comment2 = p.Comment2,
                IsTaxExempt = p.IsTaxExempt,
                PartnerGroupId = p.PartnerGroupId,
                PartnerTypeId = p.PartnerTypeId,
                IsActive = p.IsActive,

                Sites = p.Sites?.Select(s => new SiteDto
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
                    Phone1 = s.Phone1,
                    Phone2 = s.Phone2,
                    Phone3 = s.Phone3,
                    MobilePhone1 = s.MobilePhone1,
                    MobilePhone2 = s.MobilePhone2,
                    MobilePhone3 = s.MobilePhone3,
                    eMail1 = s.eMail1,
                    eMail2 = s.eMail2,
                    messagingApp1 = s.messagingApp1,
                    messagingApp2 = s.messagingApp2,
                    messagingApp3 = s.messagingApp3,
                    PartnerId = s.PartnerId
                }).ToList() ?? new List<SiteDto>(),

                Contacts = p.Contacts?.Select(c => new ContactDto
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
                }).ToList() ?? new List<ContactDto>(),

                Documents = p.Documents?.Select(d => new DocumentDto
                {
                    DocumentId = d.DocumentId,
                    FileName = d.FileName,
                    FilePath = d.FilePath,
                    UploadDate = d.UploadDate
                }).ToList() ?? new List<DocumentDto>()
            };
        }
    }
}
