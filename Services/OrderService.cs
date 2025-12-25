using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Cloud9_2.Models;
using Cloud9_2.Data;
using System.ComponentModel.DataAnnotations;

namespace Cloud9_2.Services
{
    public class OrderService
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<OrderService> _logger;

        public OrderService(ApplicationDbContext context, UserManager<ApplicationUser> userManager, ILogger<OrderService> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        // Helper method to calculate UnitPrice for an OrderItem
        private async Task<decimal> CalculateUnitPriceAsync(int productId, decimal quantity, int partnerId, int currencyId)
        {
            // Check for PartnerProductPrice first
            var partnerPrice = await _context.PartnerProductPrice
                .Where(ppp => ppp.PartnerId == partnerId && ppp.ProductId == productId)
                .FirstOrDefaultAsync();

            if (partnerPrice != null)
            {
                return partnerPrice.PartnerUnitPrice;
            }

            // Fallback to ProductPrice
            var productPrice = await _context.ProductPrices
                .Where(pp => pp.ProductId == productId
                             && pp.CurrencyId == currencyId
                             && pp.IsActive
                             && pp.StartDate <= DateTime.UtcNow
                             && (pp.EndDate == null || pp.EndDate >= DateTime.UtcNow))
                .FirstOrDefaultAsync();

            if (productPrice == null)
            {
                throw new ValidationException($"No valid price found for ProductId: {productId} in CurrencyId: {currencyId}.");
            }

            // Determine price based on volume thresholds
            // Convert quantity to int for volume comparisons, assuming fractional quantities use the same tier
            int quantityForThreshold = (int)Math.Floor(quantity);
            decimal basePrice;
            if (quantityForThreshold >= productPrice.Volume3 && productPrice.Volume3 > 0)
                basePrice = productPrice.Volume3Price;
            else if (quantityForThreshold >= productPrice.Volume2 && productPrice.Volume2 > 0)
                basePrice = productPrice.Volume2Price;
            else if (quantityForThreshold >= productPrice.Volume1 && productPrice.Volume1 > 0)
                basePrice = productPrice.Volume1Price;
            else
                basePrice = productPrice.SalesPrice;

            // Apply discount if applicable
            if (productPrice.DiscountPercentage > 0)
            {
                basePrice *= (1 - productPrice.DiscountPercentage / 100);
            }

            return Math.Round(basePrice, 2);
        }

        public async Task<Order> CreateOrderAsync(OrderCreateDTO orderDto, string userId)
        {
            // Validation
            if (orderDto.OrderItems != null && orderDto.OrderItems.Any(i => i.Quantity <= 0))
                throw new ValidationException("A rendelési tételek mennyisége pozitív kell legyen.");
            if (!await _context.Partners.AnyAsync(p => p.PartnerId == orderDto.PartnerId))
                throw new ValidationException("Érvénytelen PartnerId.");
            if (!await _context.Currencies.AnyAsync(c => c.CurrencyId == orderDto.CurrencyId))
                throw new ValidationException("Érvénytelen CurrencyId.");
            if (orderDto.SiteId.HasValue && !await _context.Sites.AnyAsync(s => s.SiteId == orderDto.SiteId))
                throw new ValidationException("Érvénytelen SiteId.");
            if (orderDto.ShippingMethodId.HasValue && !await _context.OrderShippingMethods.AnyAsync(s => s.ShippingMethodId == orderDto.ShippingMethodId))
                throw new ValidationException("Érvénytelen ShippingMethodId.");
            if (orderDto.PaymentTermId.HasValue && !await _context.PaymentTerms.AnyAsync(p => p.PaymentTermId == orderDto.PaymentTermId))
                throw new ValidationException("Érvénytelen PaymentTermId.");
            if (orderDto.ContactId.HasValue && !await _context.Contacts.AnyAsync(c => c.ContactId == orderDto.ContactId))
                throw new ValidationException("Érvénytelen ContactId.");
            if (orderDto.QuoteId.HasValue && !await _context.Quotes.AnyAsync(q => q.QuoteId == orderDto.QuoteId))
                throw new ValidationException("Érvénytelen QuoteId.");
            if (orderDto.OrderItems != null)
            {
                foreach (var item in orderDto.OrderItems)
                {
                    if (!await _context.Products.AnyAsync(p => p.ProductId == item.ProductId))
                        throw new ValidationException($"Érvénytelen ProductId: {item.ProductId}.");
                    if (item.VatTypeId.HasValue && !await _context.VatTypes.AnyAsync(v => v.VatTypeId == item.VatTypeId))
                        throw new ValidationException($"Érvénytelen VatTypeId: {item.VatTypeId}.");
                }
            }

            // Generate unique order number
            string orderNumber;
            do
            {
                orderNumber = $"R-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString().Substring(0, 4)}";
            } while (await _context.Orders.AnyAsync(o => o.OrderNumber == orderNumber && o.IsDeleted != true));

            var user = await _userManager.FindByIdAsync(userId);
            var userName = user?.UserName ?? "System";

            var order = new Order
            {
                OrderNumber = orderNumber,
                OrderDate = orderDto.OrderDate,
                Deadline = orderDto.Deadline,
                Description = orderDto.Description,
                SalesPerson = orderDto.SalesPerson,
                DeliveryDate = orderDto.DeliveryDate,
                PlannedDelivery = orderDto.PlannedDelivery,
                DiscountPercentage = orderDto.DiscountPercentage,
                DiscountAmount = orderDto.DiscountAmount,
                CompanyName = orderDto.CompanyName,
                Subject = orderDto.Subject,
                DetailedDescription = orderDto.DetailedDescription,
                Status = orderDto.Status ?? "Pending",
                PartnerId = orderDto.PartnerId,
                SiteId = orderDto.SiteId,
                CurrencyId = orderDto.CurrencyId,
                ShippingMethodId = orderDto.ShippingMethodId,
                PaymentTermId = orderDto.PaymentTermId,
                ContactId = orderDto.ContactId,
                OrderType = orderDto.OrderType,
                ReferenceNumber = orderDto.ReferenceNumber,
                QuoteId = orderDto.QuoteId,
                IsDeleted = orderDto.IsDeleted ?? false,
                OrderStatusTypes = orderDto.OrderStatusTypes ?? 1,
                CreatedBy = userName,
                CreatedDate = DateTime.UtcNow,
                ModifiedBy = userName,
                ModifiedDate = DateTime.UtcNow,
                OrderItems = orderDto.OrderItems?.Select(item => new OrderItem
                {
                    Description = item.Description,
                    Quantity = item.Quantity,
                    UnitPrice = 0, // Will be calculated below
                    DiscountAmount = item.DiscountAmount,
                    DiscountType = item.DiscountType,
                    ProductId = item.ProductId,
                    VatTypeId = item.VatTypeId,
                    CreatedBy = userName,
                    CreatedDate = DateTime.UtcNow,
                    ModifiedBy = userName,
                    ModifiedDate = DateTime.UtcNow
                }).ToList() ?? new List<OrderItem>()
            };

            // Calculate UnitPrice for each OrderItem
            if (order.OrderItems != null)
            {
                foreach (var item in order.OrderItems)
                {
                    item.UnitPrice = await CalculateUnitPriceAsync(item.ProductId, item.Quantity, order.PartnerId, order.CurrencyId);
                }
            }

            // Calculate TotalAmount
            order.TotalAmount = order.OrderItems?.Sum(i => i.Quantity * i.UnitPrice - (i.DiscountAmount ?? 0)) ?? orderDto.TotalAmount;

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                _context.Orders.Add(order);
                await _context.SaveChangesAsync();

                foreach (var item in order.OrderItems)
                {
                    item.OrderId = order.OrderId;
                }
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();
                _logger.LogInformation("Sikeresen létrehozva a rendelés, ID: {OrderId}", order.OrderId);
                return order;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Hiba a rendelés létrehozása során, OrderNumber: {OrderNumber}", orderNumber);
                throw;
            }
        }

        // Get an order by ID
public async Task<Order?> GetOrderByIdAsync(int orderId)
{
    return await _context.Orders
        .Include(o => o.OrderItems).ThenInclude(oi => oi.Product)
        .Include(o => o.OrderItems).ThenInclude(oi => oi.VatType)
        .Include(o => o.Partner) // csak egyszer!
        .Include(o => o.Site)
        .Include(o => o.Currency)
        .Include(o => o.ShippingMethod)
        .Include(o => o.OrderStatusType)
        .Include(o => o.PaymentTerm)
        .Include(o => o.Contact)
        .Include(o => o.Quote)
        .FirstOrDefaultAsync(o => o.OrderId == orderId);
}

        // Get all orders
public async Task<List<Order>> GetAllOrdersAsync()
{
    return await _context.Orders
        .Include(o => o.OrderItems).ThenInclude(oi => oi.Product)
        .Include(o => o.OrderItems).ThenInclude(oi => oi.VatType)
        .Include(o => o.Partner)
        .Include(o => o.Site)
        .Include(o => o.Currency)
        .Include(o => o.ShippingMethod)
        .Include(o => o.OrderStatusType)
        .Include(o => o.PaymentTerm)
        .Include(o => o.Contact)
        .Include(o => o.Quote)
        .ToListAsync();
}


        public async Task<Order?> UpdateOrderAsync(OrderUpdateDTO orderDto, string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            var userName = user?.UserName ?? "System";

            var order = await _context.Orders
                .Include(o => o.OrderItems)
                .FirstOrDefaultAsync(o => o.OrderId == orderDto.OrderId);

            if (order == null)
            {
                return null;
            }

            // Update order properties
            order.OrderNumber = orderDto.OrderNumber;
            order.OrderDate = orderDto.OrderDate;
            order.Deadline = orderDto.Deadline;
            order.Description = orderDto.Description;
            order.SalesPerson = orderDto.SalesPerson;
            order.DeliveryDate = orderDto.DeliveryDate;
            order.PlannedDelivery = orderDto.PlannedDelivery;
            order.DiscountPercentage = orderDto.DiscountPercentage;
            order.DiscountAmount = orderDto.DiscountAmount;
            order.CompanyName = orderDto.CompanyName;
            order.Subject = orderDto.Subject;
            order.DetailedDescription = orderDto.DetailedDescription;
            order.Status = orderDto.Status;
            order.PartnerId = orderDto.PartnerId;
            order.SiteId = orderDto.SiteId;
            order.CurrencyId = orderDto.CurrencyId;
            order.ShippingMethodId = orderDto.ShippingMethodId;
            order.PaymentTermId = orderDto.PaymentTermId;
            order.ContactId = orderDto.ContactId;
            order.OrderType = orderDto.OrderType;
            order.ReferenceNumber = orderDto.ReferenceNumber;
            order.QuoteId = orderDto.QuoteId;
            order.ModifiedBy = userName;
            order.ModifiedDate = DateTime.UtcNow;
            order.OrderStatusTypes = orderDto.OrderStatusTypes ?? 1;

            // Update OrderItems
            if (orderDto.OrderItems != null)
            {
                // Remove existing items not in the updated list
                var existingItemIds = order.OrderItems.Select(i => i.OrderItemId).ToList();
                var updatedItemIds = orderDto.OrderItems.Select(i => i.OrderItemId).ToList();
                var itemsToRemove = existingItemIds.Except(updatedItemIds).ToList();

                foreach (var itemId in itemsToRemove)
                {
                    var itemToRemove = order.OrderItems.FirstOrDefault(i => i.OrderItemId == itemId);
                    if (itemToRemove != null)
                    {
                        _context.OrderItems.Remove(itemToRemove);
                    }
                }

                // Add or update items
                foreach (var itemDto in orderDto.OrderItems)
                {
                    var existingItem = order.OrderItems.FirstOrDefault(i => i.OrderItemId == itemDto.OrderItemId);
                    if (existingItem != null)
                    {
                        // Update existing item
                        existingItem.Description = itemDto.Description;
                        existingItem.Quantity = itemDto.Quantity;
                        existingItem.UnitPrice = await CalculateUnitPriceAsync(itemDto.ProductId, itemDto.Quantity, order.PartnerId, order.CurrencyId);
                        existingItem.DiscountAmount = itemDto.DiscountAmount;
                        existingItem.DiscountType = itemDto.DiscountType;
                        existingItem.ProductId = itemDto.ProductId;
                        existingItem.VatTypeId = itemDto.VatTypeId;
                        existingItem.ModifiedBy = userName;
                        existingItem.ModifiedDate = DateTime.UtcNow;
                    }
                    else
                    {
                        // Add new item
                        order.OrderItems.Add(new OrderItem
                        {
                            OrderId = order.OrderId,
                            Description = itemDto.Description,
                            Quantity = itemDto.Quantity,
                            UnitPrice = await CalculateUnitPriceAsync(itemDto.ProductId, itemDto.Quantity, order.PartnerId, order.CurrencyId),
                            DiscountAmount = itemDto.DiscountAmount,
                            DiscountType = itemDto.DiscountType,
                            ProductId = itemDto.ProductId,
                            VatTypeId = itemDto.VatTypeId,
                            CreatedBy = userName,
                            CreatedDate = DateTime.UtcNow,
                            ModifiedBy = userName,
                            ModifiedDate = DateTime.UtcNow
                        });
                    }
                }
            }

            // Recalculate TotalAmount
            order.TotalAmount = order.OrderItems?.Sum(i => i.Quantity * i.UnitPrice - (i.DiscountAmount ?? 0)) ?? orderDto.TotalAmount;

            await _context.SaveChangesAsync();
            return order;
        }

        // Delete an order
        public async Task<bool> DeleteOrderAsync(int orderId)
        {
            var order = await _context.Orders
                .Include(o => o.OrderItems)
                .FirstOrDefaultAsync(o => o.OrderId == orderId);

            if (order == null)
            {
                return false;
            }

            _context.Orders.Remove(order);
            await _context.SaveChangesAsync();
            return true;
        }
        
    }
}