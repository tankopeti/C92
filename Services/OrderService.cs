using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Cloud9_2.Models;
using Cloud9_2.Data;

namespace Cloud9_2.Services
{


public interface IOrderService
    {
        Task<string> GetNextOrderNumberAsync();
        Task<bool> OrderExistsAsync(int OrderId);
        Task<List<Partner>> GetPartnersAsync();
        Task<List<OrderItem>> GetOrderItemsAsync(int OrderId);
        Task<Order> CreateOrderAsync(Order order);
        Task<Order> GetOrderByIdAsync(int OrderId);
        Task<Order?> UpdateOrderAsync(int orderId, Order orderUpdate);
        Task<bool> DeleteOrderAsync(int OrderId);
        Task<OrderItem?> CreateOrderItemAsync(int OrderId, OrderItem item);
        Task<OrderItem?> UpdateOrderItemAsync(int OrderId, int OrderItemId, OrderItem itemUpdate);
        Task<bool> DeleteOrderItemAsync(int OrderId, int OrderItemId);
        Task<Order> CopyOrderAsync(int OrderId);
        Task<List<Order>> GetOrdersAsync(string searchTerm, string statusFilter, string sortBy, int skip, int take);
        Task<OrderDto> GetOrderAsync(int orderId);
    }

public class OrderService : IOrderService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<OrderService> _logger;

        public OrderService(ApplicationDbContext context, ILogger<OrderService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<string> GetNextOrderNumberAsync()
        {
            var year = DateTime.UtcNow.DayOfYear;
            var count = await _context.Orders.CountAsync(o => o.OrderNumber != null && o.OrderNumber.Contains($"TestOrder-{year}"));
            return $"TestOrder-{year}-{count + 1:D4}";
        }

        public async Task<bool> OrderExistsAsync(int OrderId)
        {
            return await _context.Orders.AnyAsync(o => o.OrderId == OrderId);
        }

        public async Task<List<Partner>> GetPartnersAsync()
        {
            return await _context.Partners.ToListAsync();
        }

        public async Task<List<OrderItem>> GetOrderItemsAsync(int OrderId)
        {
            return await _context.OrderItems
                .Where(oi => oi.OrderId == OrderId)
                .Include(oi => oi.Product)
                .Include(oi => oi.VatType)
                .ToListAsync();
        }

public async Task<Order> CreateOrderAsync(Order order)
{
    if (order == null)
    {
        _logger.LogError("CreateOrderAsync received null order");
        throw new ArgumentNullException(nameof(order));
    }

    if (_context == null)
    {
        _logger.LogError("Database context is null for CreateOrderAsync");
        throw new InvalidOperationException("Adatbázis kapcsolat nem érhető el");
    }

    _logger.LogInformation("Validating order: PartnerId={PartnerId}, CurrencyId={CurrencyId}, SiteId={SiteId}, QuoteId={QuoteId}, OrderNumber={OrderNumber}, Status={Status}, TotalAmount={TotalAmount}", 
        order.PartnerId, order.CurrencyId, order.SiteId, order.QuoteId, order.OrderNumber, order.Status, order.TotalAmount);

    if (order.PartnerId <= 0 || !await _context.Partners.AnyAsync(p => p.PartnerId == order.PartnerId))
    {
        _logger.LogError("Invalid PartnerId: {PartnerId}", order.PartnerId);
        throw new ArgumentException($"Érvénytelen PartnerId: {order.PartnerId}");
    }

    if (order.CurrencyId <= 0 || !await _context.Currencies.AnyAsync(c => c.CurrencyId == order.CurrencyId))
    {
        _logger.LogError("Invalid CurrencyId: {CurrencyId}", order.CurrencyId);
        throw new ArgumentException($"Érvénytelen CurrencyId: {order.CurrencyId}");
    }

    if (order.SiteId.HasValue && !await _context.Sites.AnyAsync(s => s.SiteId == order.SiteId))
    {
        _logger.LogError("Invalid SiteId: {SiteId}", order.SiteId);
        throw new ArgumentException($"Érvénytelen SiteId: {order.SiteId}");
    }

    if (order.QuoteId.HasValue && !await _context.Quotes.AnyAsync(q => q.QuoteId == order.QuoteId))
    {
        _logger.LogError("Invalid QuoteId: {QuoteId}", order.QuoteId);
        throw new ArgumentException($"Érvénytelen QuoteId: {order.QuoteId}");
    }

    if (order.OrderItems == null || !order.OrderItems.Any())
    {
        _logger.LogError("No order items provided");
        throw new ArgumentException("Legalább egy tétel szükséges");
    }

    foreach (var item in order.OrderItems)
    {
        _logger.LogInformation("Validating OrderItem: ProductId={ProductId}, VatTypeId={VatTypeId}, OrderItemId={OrderItemId}", 
            item.ProductId, item.VatTypeId, item.OrderItemId);

        if (item.OrderItemId != 0)
        {
            _logger.LogWarning("OrderItemId {OrderItemId} provided for new OrderItem, resetting to 0", item.OrderItemId);
            item.OrderItemId = 0; // Ensure IDENTITY column is not set
        }

        if (item.ProductId <= 0 || !await _context.Products.AnyAsync(p => p.ProductId == item.ProductId))
        {
            _logger.LogError("Invalid ProductId in OrderItem: {ProductId}", item.ProductId);
            throw new ArgumentException($"Érvénytelen ProductId: {item.ProductId}");
        }
        if (item.Quantity <= 0)
        {
            _logger.LogError("Invalid Quantity in OrderItem: {Quantity}", item.Quantity);
            throw new ArgumentException($"Érvénytelen mennyiség: {item.Quantity}");
        }
        if (item.UnitPrice < 0)
        {
            _logger.LogError("Invalid UnitPrice in OrderItem: {UnitPrice}", item.UnitPrice);
            throw new ArgumentException($"Érvénytelen egységár: {item.UnitPrice}");
        }
        if (item.DiscountAmount < 0)
        {
            _logger.LogError("Invalid DiscountAmount in OrderItem: {DiscountAmount}", item.DiscountAmount);
            throw new ArgumentException($"Érvénytelen kedvezmény összeg: {item.DiscountAmount}");
        }
        if (item.VatTypeId.HasValue && item.VatTypeId <= 0)
        {
            _logger.LogError("Invalid VatTypeId in OrderItem: {VatTypeId}", item.VatTypeId);
            throw new ArgumentException($"Érvénytelen ÁFA típus azonosító: {item.VatTypeId}");
        }
        if (item.VatTypeId.HasValue && !await _context.VatTypes.AnyAsync(v => v.VatTypeId == item.VatTypeId))
        {
            _logger.LogError("VAT type not found for VatTypeId: {VatTypeId}", item.VatTypeId);
            throw new ArgumentException($"Érvénytelen ÁFA típus azonosító: {item.VatTypeId}");
        }
    }

    using var transaction = await _context.Database.BeginTransactionAsync();
    try
    {
        order.OrderNumber = await GetNextOrderNumberAsync(); // Always generate unique OrderNumber
        order.CreatedDate = DateTime.UtcNow;
        order.ModifiedDate = DateTime.UtcNow;
        order.CreatedBy ??= "System";
        order.ModifiedBy ??= "System";
        order.Status ??= "Pending";

        _context.Orders.Add(order);
        await _context.SaveChangesAsync();

        foreach (var item in order.OrderItems)
        {
            item.OrderId = order.OrderId;
            item.OrderItemId = 0; // Ensure IDENTITY column is not set
            item.CreatedDate = DateTime.UtcNow;
            item.ModifiedDate = DateTime.UtcNow;
            item.CreatedBy ??= "System";
            item.ModifiedBy ??= "System";
            _context.OrderItems.Add(item);
        }

        await _context.SaveChangesAsync();
        await transaction.CommitAsync();

        var createdOrder = await _context.Orders
            .Include(o => o.OrderItems)
            .FirstOrDefaultAsync(o => o.OrderId == order.OrderId);
        return createdOrder ?? throw new Exception("Failed to retrieve created order");
    }
    catch (DbUpdateException ex)
    {
        await transaction.RollbackAsync();
        _logger.LogError(ex, "Database error creating order for PartnerId: {PartnerId}", order.PartnerId);
        throw new InvalidOperationException($"Adatbázis hiba a megrendelés létrehozásakor: {ex.InnerException?.Message ?? ex.Message}", ex);
    }
    catch (Exception ex)
    {
        await transaction.RollbackAsync();
        _logger.LogError(ex, "Unexpected error creating order for PartnerId: {PartnerId}", order.PartnerId);
        throw new InvalidOperationException($"Hiba a megrendelés létrehozásakor: {ex.Message}", ex);
    }
}

public async Task<Order> GetOrderByIdAsync(int OrderId)
        {
            var order = await _context.Orders
                .Include(o => o.OrderItems)
                .FirstOrDefaultAsync(o => o.OrderId == OrderId);
            if (order == null)
            {
                _logger.LogWarning("Order not found for OrderId: {OrderId}", OrderId);
                throw new ArgumentException($"Érvénytelen OrderId: {OrderId}");
            }
            return order;
        }

public async Task<OrderDto> GetOrderAsync(int id)
        {
            try
            {
                var order = await _context.Orders
                    .AsNoTracking()
                    .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                    .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.VatType)
                    .Include(o => o.Partner)
                    .Include(o => o.Currency)
                    .Include(o => o.Site)
                    .Include(o => o.Quote)
                    .FirstOrDefaultAsync(o => o.OrderId == id);

                if (order == null)
                {
                    _logger.LogWarning("Order {id} not found in database", id);
                    return null;
                }

                var orderDto = new OrderDto
                {
                    OrderId = order.OrderId,
                    OrderNumber = order.OrderNumber,
                    OrderDate = order.OrderDate,
                    Deadline = order.Deadline,
                    Status = order.Status,
                    SalesPerson = order.SalesPerson,
                    DeliveryDate = order.DeliveryDate,
                    DiscountPercentage = order.DiscountPercentage,
                    DiscountAmount = order.DiscountAmount,
                    CompanyName = order.CompanyName,
                    Subject = order.Subject,
                    PaymentTerms = order.PaymentTerms,
                    ShippingMethod = order.ShippingMethod,
                    OrderType = order.OrderType,
                    ReferenceNumber = order.ReferenceNumber,
                    TotalAmount = order.TotalAmount,
                    Description = order.Description,
                    DetailedDescription = order.DetailedDescription,
                    PartnerId = order.PartnerId,
                    SiteId = order.SiteId,
                    CurrencyId = order.CurrencyId,
                    QuoteId = order.QuoteId,
                    CreatedBy = order.CreatedBy,
                    CreatedDate = order.CreatedDate,
                    ModifiedBy = order.ModifiedBy,
                    ModifiedDate = order.ModifiedDate,
                    OrderItems = order.OrderItems?.Select(oi => new OrderItemDto
                    {
                        OrderItemId = oi.OrderItemId,
                        ProductId = oi.ProductId,
                        ProductName = oi.Product?.Name ?? "Unknown",
                        Quantity = oi.Quantity,
                        UnitPrice = oi.UnitPrice,
                        DiscountTypeId = oi.DiscountType,
                        DiscountAmount = oi.DiscountAmount,
                        VatTypeId = oi.VatTypeId,
                        VatRate = oi.VatType?.Rate, // Handle null case
                        Description = oi.Description,
                        CreatedBy = oi.CreatedBy,
                        CreatedDate = oi.CreatedDate,
                        ModifiedBy = oi.ModifiedBy,
                        ModifiedDate = oi.ModifiedDate
                    }).ToList() ?? new List<OrderItemDto>()
                };

                _logger.LogInformation("Fetched order {id} with {ItemCount} items: {OrderItems}", id, orderDto.OrderItems.Count, orderDto.OrderItems.Select(oi => new { oi.OrderItemId, oi.ProductId, oi.VatTypeId }));
                return orderDto;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching order {id}", id);
                throw;
            }
        }

public async Task<Order?> UpdateOrderAsync(int orderId, Order orderUpdate)
        {
            _logger.LogInformation("UpdateOrderAsync called for OrderId: {OrderId}", orderId);

            if (orderUpdate == null)
            {
                _logger.LogWarning("UpdateOrderAsync received null orderUpdate for OrderId: {OrderId}", orderId);
                throw new ArgumentNullException(nameof(orderUpdate));
            }

            var order = await _context.Orders
                .Include(o => o.OrderItems)
                .FirstOrDefaultAsync(o => o.OrderId == orderId);

            if (order == null)
            {
                _logger.LogWarning("Order not found for OrderId: {OrderId}", orderId);
                return null;
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                order.PartnerId = orderUpdate.PartnerId != 0 ? orderUpdate.PartnerId : order.PartnerId;
                order.CurrencyId = orderUpdate.CurrencyId != 0 ? orderUpdate.CurrencyId : order.CurrencyId;
                order.SiteId = orderUpdate.SiteId != 0 ? orderUpdate.SiteId : order.SiteId;
                order.QuoteId = orderUpdate.QuoteId != 0 ? orderUpdate.QuoteId : order.QuoteId;
                order.OrderNumber = orderUpdate.OrderNumber ?? order.OrderNumber;
                order.OrderDate = orderUpdate.OrderDate ?? order.OrderDate;
                order.Deadline = orderUpdate.Deadline ?? order.Deadline;
                order.DeliveryDate = orderUpdate.DeliveryDate ?? order.DeliveryDate;
                order.ReferenceNumber = orderUpdate.ReferenceNumber ?? order.ReferenceNumber;
                order.OrderType = orderUpdate.OrderType ?? order.OrderType;
                order.CompanyName = orderUpdate.CompanyName ?? order.CompanyName;
                order.TotalAmount = orderUpdate.TotalAmount != 0 ? orderUpdate.TotalAmount : order.TotalAmount;
                order.DiscountAmount = orderUpdate.DiscountAmount ?? order.DiscountAmount;
                order.PaymentTerms = orderUpdate.PaymentTerms ?? order.PaymentTerms;
                order.ShippingMethod = orderUpdate.ShippingMethod ?? order.ShippingMethod;
                order.SalesPerson = orderUpdate.SalesPerson ?? order.SalesPerson;
                order.Subject = orderUpdate.Subject ?? order.Subject;
                order.Description = orderUpdate.Description ?? order.Description;
                order.DetailedDescription = orderUpdate.DetailedDescription ?? order.DetailedDescription;
                order.Status = orderUpdate.Status ?? order.Status;
                order.ModifiedBy = orderUpdate.ModifiedBy ?? order.ModifiedBy;
                order.ModifiedDate = orderUpdate.ModifiedDate ?? DateTime.UtcNow;

                if (orderUpdate.OrderItems != null)
                {
                    foreach (var itemUpdate in orderUpdate.OrderItems)
                    {
                        if (itemUpdate.OrderItemId == 0)
                        {
                            itemUpdate.OrderId = orderId;
                            await CreateOrderItemAsync(orderId, itemUpdate);
                        }
                        else
                        {
                            await UpdateOrderItemAsync(orderId, itemUpdate.OrderItemId, itemUpdate);
                        }
                    }
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return order;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error updating order for OrderId: {OrderId}", orderId);
                throw;
            }
        }

public async Task<bool> DeleteOrderAsync(int orderId)
        {
            var order = await _context.Orders.FirstOrDefaultAsync(o => o.OrderId == orderId);
            if (order == null)
            {
                _logger.LogWarning("Order {orderId} not found for deletion", orderId);
                return false;
            }
            _context.Orders.Remove(order);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Order {orderId} deleted successfully", orderId);
            return true;
        }

public async Task<OrderItem?> CreateOrderItemAsync(int OrderId, OrderItem item)
        {
            _logger.LogInformation("CreateOrderItemAsync called for OrderId: {OrderId}", OrderId);

            if (item == null)
            {
                _logger.LogWarning("CreateOrderItemAsync received null item for OrderId: {OrderId}", OrderId);
                throw new ArgumentNullException(nameof(item));
            }

            if (_context == null)
            {
                _logger.LogError("Database context is null for CreateOrderItemAsync OrderId: {OrderId}", OrderId);
                throw new InvalidOperationException("Adatbázis kapcsolat nem érhető el");
            }

            var order = await _context.Orders.FirstOrDefaultAsync(q => q.OrderId == OrderId);
            if (order == null)
            {
                _logger.LogWarning("Order not found for OrderId: {OrderId}", OrderId);
                return null;
            }

            var product = await _context.Products.FirstOrDefaultAsync(p => p.ProductId == item.ProductId);
            if (product == null)
            {
                _logger.LogWarning("Product not found for ProductId: {ProductId}", item.ProductId);
                throw new ArgumentException($"Érvénytelen ProductId: {item.ProductId}");
            }

            if (item.VatTypeId <= 0)
            {
                _logger.LogWarning("Invalid VatTypeId for OrderItem: {VatTypeId}", item.VatTypeId);
                throw new ArgumentException($"Érvénytelen ÁFA típus azonosító: {item.VatTypeId}");
            }

            var vatType = await _context.VatTypes.FirstOrDefaultAsync(v => v.VatTypeId == item.VatTypeId);
            if (vatType == null)
            {
                _logger.LogWarning("VAT type not found for VatTypeId: {VatTypeId}", item.VatTypeId);
                throw new ArgumentException($"Érvénytelen ÁFA típus azonosító: {item.VatTypeId}");
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                item.OrderId = OrderId;
                item.Description ??= "";
                item.CreatedDate = DateTime.UtcNow;
                item.ModifiedDate = DateTime.UtcNow;
                item.CreatedBy ??= "System";
                item.ModifiedBy ??= "System";

                _context.OrderItems.Add(item);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                _logger.LogInformation("Created OrderItem ID: {OrderItemId} for OrderId: {OrderId} with VatTypeId: {VatTypeId}", item.OrderItemId, OrderId, item.VatTypeId);

                return item;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error creating OrderItem for OrderId: {OrderId}", OrderId);
                throw;
            }
        }

public async Task<OrderItem?> UpdateOrderItemAsync(int OrderId, int OrderItemId, OrderItem itemUpdate)
        {
            _logger.LogInformation("UpdateOrderItemAsync called for OrderId: {OrderId}, OrderItemId: {OrderItemId}", OrderId, OrderItemId);

            if (itemUpdate == null)
            {
                _logger.LogWarning("UpdateOrderItemAsync received null itemUpdate for OrderId: {OrderId}, OrderItemId: {OrderItemId}", OrderId, OrderItemId);
                throw new ArgumentNullException(nameof(itemUpdate));
            }

            var orderItem = await _context.OrderItems
                .Include(oi => oi.Product)
                .FirstOrDefaultAsync(oi => oi.OrderId == OrderId && oi.OrderItemId == OrderItemId);

            if (orderItem == null)
            {
                _logger.LogWarning("OrderItem not found for OrderId: {OrderId}, OrderItemId: {OrderItemId}", OrderId, OrderItemId);
                return null;
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                orderItem.ProductId = itemUpdate.ProductId;
                orderItem.Quantity = itemUpdate.Quantity;
                orderItem.UnitPrice = itemUpdate.UnitPrice;
                orderItem.Description = itemUpdate.Description ?? orderItem.Description;
                orderItem.DiscountAmount = itemUpdate.DiscountAmount ?? orderItem.DiscountAmount;
                orderItem.DiscountType = itemUpdate.DiscountType ?? orderItem.DiscountType;
                orderItem.ModifiedBy = itemUpdate.ModifiedBy ?? orderItem.ModifiedBy;
                orderItem.ModifiedDate = itemUpdate.ModifiedDate ?? DateTime.UtcNow;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation("Updated OrderItem ID: {OrderItemId} for OrderId: {OrderId}", OrderItemId, OrderId);

                return orderItem;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error updating OrderItem for OrderId: {OrderId}, OrderItemId: {OrderItemId}", OrderId, OrderItemId);
                throw;
            }
        }

public async Task<bool> DeleteOrderItemAsync(int orderId, int orderItemId)
        {
            try
            {
                var orderItem = await _context.OrderItems
                    .FirstOrDefaultAsync(oi => oi.OrderId == orderId && oi.OrderItemId == orderItemId);

                if (orderItem == null)
                {
                    _logger.LogWarning("Order item not found for OrderId: {OrderId}, OrderItemId: {OrderItemId}", orderId, orderItemId);
                    var orderItems = await _context.OrderItems
                        .Where(oi => oi.OrderId == orderId)
                        .Select(oi => new { oi.OrderItemId, oi.ProductId })
                        .ToListAsync();
                    _logger.LogInformation("Available order items for OrderId: {OrderId}: {OrderItems}", orderId, orderItems);
                    return false;
                }

                _context.OrderItems.Remove(orderItem);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Order item {OrderItemId} deleted successfully for OrderId: {OrderId}", orderItemId, orderId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting order item {OrderItemId} for OrderId: {OrderId}", orderItemId, orderId);
                throw;
            }
        }
public async Task<Order> CopyOrderAsync(int orderId)
{
    var sourceOrder = await _context.Orders
        .Include(o => o.OrderItems)
        .FirstOrDefaultAsync(o => o.OrderId == orderId);
    if (sourceOrder == null)
    {
        _logger.LogWarning("Order not found for OrderId: {OrderId}", orderId);
        throw new ArgumentException($"Érvénytelen OrderId: {orderId}");
    }

    _logger.LogInformation("Copying OrderId {OrderId}: PartnerId={PartnerId}, CurrencyId={CurrencyId}, ItemCount={ItemCount}", 
        orderId, sourceOrder.PartnerId, sourceOrder.CurrencyId, sourceOrder.OrderItems?.Count ?? 0);

    // Validate required fields
    if (!await _context.Partners.AnyAsync(p => p.PartnerId == sourceOrder.PartnerId))
    {
        _logger.LogWarning("Invalid PartnerId: {PartnerId} for OrderId: {OrderId}", sourceOrder.PartnerId, orderId);
        throw new ArgumentException($"Érvénytelen PartnerId: {sourceOrder.PartnerId}");
    }

    if (!await _context.Currencies.AnyAsync(c => c.CurrencyId == sourceOrder.CurrencyId))
    {
        _logger.LogWarning("Invalid CurrencyId: {CurrencyId} for OrderId: {OrderId}", sourceOrder.CurrencyId, orderId);
        throw new ArgumentException($"Érvénytelen CurrencyId: {sourceOrder.CurrencyId}");
    }

    if (sourceOrder.OrderItems == null || !sourceOrder.OrderItems.Any(item => item != null))
    {
        _logger.LogWarning("No valid OrderItems for OrderId: {OrderId}", orderId);
        throw new ArgumentException("Legalább egy érvényes tétel szükséges");
    }

    var validOrderItems = sourceOrder.OrderItems
        .Where(item => item != null)
        .ToList();

    foreach (var item in validOrderItems)
    {
        if (!await _context.Products.AnyAsync(p => p.ProductId == item.ProductId))
        {
            _logger.LogWarning("Invalid ProductId: {ProductId} for OrderItemId: {OrderItemId} in OrderId: {OrderId}", 
                item.ProductId, item.OrderItemId, orderId);
            throw new ArgumentException($"Érvénytelen ProductId: {item.ProductId}");
        }
    }

    var newOrder = new Order
    {
        PartnerId = sourceOrder.PartnerId,
        CurrencyId = sourceOrder.CurrencyId,
        OrderNumber = await GetNextOrderNumberAsync(),
        OrderDate = sourceOrder.OrderDate,
        Deadline = sourceOrder.Deadline,
        Description = sourceOrder.Description,
        TotalAmount = sourceOrder.TotalAmount,
        SalesPerson = sourceOrder.SalesPerson,
        DeliveryDate = sourceOrder.DeliveryDate,
        DiscountPercentage = sourceOrder.DiscountPercentage,
        DiscountAmount = sourceOrder.DiscountAmount,
        CompanyName = sourceOrder.CompanyName,
        Subject = sourceOrder.Subject,
        DetailedDescription = sourceOrder.DetailedDescription,
        CreatedBy = "System",
        CreatedDate = DateTime.UtcNow,
        ModifiedBy = "System",
        ModifiedDate = DateTime.UtcNow,
        Status = sourceOrder.Status ?? "Draft",
        SiteId = sourceOrder.SiteId,
        PaymentTerms = sourceOrder.PaymentTerms,
        ShippingMethod = sourceOrder.ShippingMethod,
        OrderType = sourceOrder.OrderType,
        ReferenceNumber = sourceOrder.ReferenceNumber != null 
            ? $"{sourceOrder.ReferenceNumber}-COPY-{DateTime.UtcNow.Ticks}" 
            : null,
        QuoteId = sourceOrder.QuoteId,
        OrderItems = validOrderItems.Select(i => new OrderItem
        {
            ProductId = i.ProductId,
            Quantity = i.Quantity,
            UnitPrice = i.UnitPrice,
            DiscountAmount = i.DiscountAmount,
            DiscountType = i.DiscountType,
            Description = i.Description,
            VatTypeId = i.VatTypeId,
            CreatedBy = "System",
            CreatedDate = DateTime.UtcNow,
            ModifiedBy = "System",
            ModifiedDate = DateTime.UtcNow
        }).ToList()
    };

    using var transaction = await _context.Database.BeginTransactionAsync();
    try
    {
        _context.Orders.Add(newOrder);
        await _context.SaveChangesAsync(); // Saves both Order and OrderItems
        await transaction.CommitAsync();

        var createdOrder = await _context.Orders
            .Include(o => o.OrderItems)
            .FirstOrDefaultAsync(o => o.OrderId == newOrder.OrderId);
        return createdOrder ?? throw new Exception("Failed to retrieve created order");
    }
    catch (DbUpdateException ex)
    {
        await transaction.RollbackAsync();
        _logger.LogError(ex, "Database error creating new order for copied OrderId: {OrderId}", orderId);
        throw new InvalidOperationException($"Adatbázis hiba: {ex.InnerException?.Message ?? ex.Message}", ex);
    }
    catch (Exception ex)
    {
        await transaction.RollbackAsync();
        _logger.LogError(ex, "Unexpected error creating new order for copied OrderId: {OrderId}", orderId);
        throw new InvalidOperationException($"Hiba a másolás során: {ex.Message}", ex);
    }
}


        public async Task<List<Order>> GetOrdersAsync(string searchTerm, string statusFilter, string sortBy, int skip, int take)
        {
            var query = _context.Orders
                .Include(o => o.Partner)
                .Include(o => o.Site)
                .Include(o => o.Currency)
                .Include(o => o.Quote)
                .Include(o => o.OrderItems)!
                .ThenInclude(oi => oi.Product)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query = query.Where(o => o.OrderNumber != null && o.OrderNumber.Contains(searchTerm) || o.CompanyName != null && o.CompanyName.Contains(searchTerm));
            }

            if (!string.IsNullOrWhiteSpace(statusFilter))
            {
                query = query.Where(o => o.Status == statusFilter);
            }

            string sortByLower = sortBy?.ToLower() ?? "orderdate";
            query = sortByLower switch
            {
                "orderdate" => query.OrderBy(o => o.OrderDate),
                "deadline" => query.OrderBy(o => o.Deadline),
                _ => query.OrderBy(o => o.OrderId)
            };

            return await query
                .Skip(skip)
                .Take(take)
                .ToListAsync();
        }
    }
}