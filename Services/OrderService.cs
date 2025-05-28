using Cloud9_2.Data;
using Cloud9_2.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using Cloud9_2.Pages.CRM.Orders;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Cloud9_2.Services
{
    public class OrderService : IOrderService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<OrderService> _logger;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public OrderService(ApplicationDbContext context, ILogger<OrderService> logger, IHttpContextAccessor httpContextAccessor)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<string> GetNextOrderNumberAsync()
        {
            try
            {
                if (_context == null)
                {
                    _logger.LogError("Database context is null");
                    throw new InvalidOperationException("Database context is not initialized");
                }

                var lastOrderNumber = await _context.Orders
                    .Where(q => q.OrderNumber != null && q.OrderNumber.StartsWith("TestOrder-"))
                    .OrderByDescending(q => q.OrderNumber)
                    .Select(q => q.OrderNumber)
                    .FirstOrDefaultAsync();

                string baseNumber = "TestOrder-0001";
                if (!string.IsNullOrEmpty(lastOrderNumber))
                {
                    var parts = lastOrderNumber.Split('-');
                    if (parts.Length >= 2 && int.TryParse(parts[1], out int lastNumber))
                    {
                        baseNumber = $"TestOrder-{lastNumber + 1:D4}";
                    }
                    else
                    {
                        _logger.LogWarning("Failed to parse lastOrderNumber: {LastOrderNumber}", lastOrderNumber);
                    }
                }

                string nextNumber;
                var random = new Random();
                int attempts = 0;
                const int maxAttempts = 5;

                do
                {
                    if (attempts++ >= maxAttempts)
                    {
                        _logger.LogError("Failed to generate unique order number after {MaxAttempts} attempts", maxAttempts);
                        throw new InvalidOperationException("Unable to generate unique order number");
                    }

                    var randomSuffix = random.Next(0, 10000).ToString("D4");
                    nextNumber = $"{baseNumber}-{randomSuffix}";
                }
                while (await _context.Orders.AnyAsync(o => o.OrderNumber == nextNumber));

                _logger.LogInformation("Generated next order number: {NextNumber}", nextNumber);
                return nextNumber;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating next order number: {Message}", ex.Message);
                throw;
            }
        }

        public async Task<bool> OrderExistsAsync(int OrderId)
        {
            return await _context.Orders.AnyAsync(q => q.OrderId == OrderId);
        }

        public async Task<OrderDto> CreateOrderAsync(CreateOrderDto orderDto)
        {
            try
            {
                if (orderDto.PartnerId <= 0)
                    throw new ArgumentException("PartnerId is required.");
                if (orderDto.OrderItems == null || !orderDto.OrderItems.Any())
                    throw new ArgumentException("At least one order item is required.");

                var order = new Order
                {
                    OrderNumber = await GetNextOrderNumberAsync(), // Always generate
                    PartnerId = orderDto.PartnerId,
                    CurrencyId = orderDto.CurrencyId,
                    SiteId = orderDto.SiteId,
                    QuoteId = orderDto.QuoteId,
                    OrderDate = orderDto.OrderDate ?? DateTime.UtcNow,
                    Deadline = orderDto.Deadline,
                    DeliveryDate = orderDto.DeliveryDate,
                    ReferenceNumber = orderDto.ReferenceNumber,
                    OrderType = orderDto.OrderType,
                    CompanyName = orderDto.CompanyName,
                    TotalAmount = orderDto.TotalAmount ?? 0,
                    DiscountPercentage = orderDto.DiscountPercentage,
                    DiscountAmount = orderDto.DiscountAmount,
                    PaymentTerms = orderDto.PaymentTerms,
                    ShippingMethod = orderDto.ShippingMethod,
                    SalesPerson = orderDto.SalesPerson,
                    Subject = orderDto.Subject,
                    Description = orderDto.Description,
                    DetailedDescription = orderDto.DetailedDescription,
                    Status = orderDto.Status ?? "Draft",
                    CreatedBy = orderDto.CreatedBy,
                    CreatedDate = orderDto.CreatedDate ?? DateTime.UtcNow,
                    ModifiedBy = orderDto.ModifiedBy,
                    ModifiedDate = orderDto.ModifiedDate,
                    OrderItems = new List<OrderItem>()
                };

                foreach (var itemDto in orderDto.OrderItems)
                {
                    if (itemDto.ProductId <= 0)
                        throw new ArgumentException($"Invalid ProductId for order item: {itemDto.ProductId}");
                    var orderItem = new OrderItem
                    {
                        ProductId = itemDto.ProductId,
                        Quantity = itemDto.Quantity,
                        UnitPrice = itemDto.UnitPrice,
                        DiscountPercentage = itemDto.DiscountPercentage,
                        DiscountAmount = itemDto.DiscountAmount,
                        Description = itemDto.Description
                    };
                    order.OrderItems.Add(orderItem);
                }

                _context.Orders.Add(order);
                await _context.SaveChangesAsync();

                return new OrderDto
                {
                    OrderId = order.OrderId,
                    OrderNumber = order.OrderNumber
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating order: {Message}", ex.Message);
                throw;
            }
        }

        public async Task<List<PartnerDto>> GetPartnersAsync()
        {
            try
            {
                var partners = await _context.Partners
                    .Select(p => new PartnerDto
                    {
                        PartnerId = p.PartnerId,
                        Name = p.Name
                    })
                    .ToListAsync();
                _logger.LogInformation($"Fetched {partners.Count} partners from database.");
                return partners;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching partners from database.");
                throw;
            }
        }

        public async Task<List<OrderItemDto>> GetOrderItemsAsync(int OrderId)
        {
            return await _context.OrderItems
                .Where(qi => qi.OrderId == OrderId)
                .Include(qi => qi.Product)
                .Select(qi => new OrderItemDto
                {
                    OrderItemId = qi.OrderItemId,
                    OrderId = qi.OrderId,
                    ProductId = qi.ProductId,
                    Product = qi.Product != null ? new ProductDto
                    {
                        ProductId = qi.Product.ProductId,
                        Name = qi.Product.Name
                    } : null,
                    Quantity = qi.Quantity,
                    UnitPrice = qi.UnitPrice,
                    Description = qi.Description,
                    DiscountPercentage = qi.DiscountPercentage,
                    DiscountAmount = qi.DiscountAmount
                    // TotalPrice is computed in OrderItemDto
                })
                .ToListAsync();
        }

public async Task<OrderDto> UpdateOrderAsync(int orderId, UpdateOrderDto orderDto, string modifiedBy)
{
    _logger.LogInformation("Updating order for OrderId: {OrderId}", orderId);
    using var transaction = await _context.Database.BeginTransactionAsync();
    try
    {
        if (orderDto == null)
        {
            _logger.LogWarning("Received null UpdateOrderDto for OrderId: {OrderId}", orderId);
            throw new ArgumentNullException(nameof(orderDto));
        }

        var order = await _context.Orders
            .Include(o => o.OrderItems)
            .FirstOrDefaultAsync(o => o.OrderId == orderId);
        if (order == null)
        {
            _logger.LogWarning("Order not found for OrderId: {OrderId}", orderId);
            throw new KeyNotFoundException($"Rendelés nem található: {orderId}");
        }

        // Validate required fields
        if (orderDto.OrderDate == null)
            throw new ArgumentException("Rendelés dátuma kötelező.");
        if (orderDto.PartnerId == null)
            throw new ArgumentException("Partner kiválasztása kötelező.");
        if (orderDto.CurrencyId == null)
            throw new ArgumentException("Pénznem kiválasztása kötelező.");
        if (string.IsNullOrEmpty(orderDto.Status))
            throw new ArgumentException("Státusz megadása kötelező.");
        if (orderDto.OrderItems == null || !orderDto.OrderItems.Any())
            throw new ArgumentException("Legalább egy tétel szükséges a rendeléshez.");

        // Validate foreign keys
        if (!await _context.Partners.AnyAsync(p => p.PartnerId == orderDto.PartnerId))
            throw new ArgumentException($"Érvénytelen PartnerId: {orderDto.PartnerId}");
        if (!await _context.Currencies.AnyAsync(c => c.CurrencyId == orderDto.CurrencyId))
            throw new ArgumentException($"Érvénytelen CurrencyId: {orderDto.CurrencyId}");
        if (orderDto.SiteId.HasValue && !await _context.Sites.AnyAsync(s => s.SiteId == orderDto.SiteId.Value))
            throw new ArgumentException($"Érvénytelen SiteId: {orderDto.SiteId}");
        if (orderDto.QuoteId.HasValue && !await _context.Quotes.AnyAsync(q => q.QuoteId == orderDto.QuoteId.Value))
            throw new ArgumentException($"Érvénytelen QuoteId: {orderDto.QuoteId}");

        // Update fields
        order.OrderNumber = orderDto.OrderNumber;
        order.PartnerId = orderDto.PartnerId.Value;
        order.OrderDate = orderDto.OrderDate.Value;
        order.Status = orderDto.Status;
        order.TotalAmount = orderDto.TotalAmount ?? 0;
        order.SalesPerson = orderDto.SalesPerson;
        order.Deadline = orderDto.Deadline;
        order.Subject = orderDto.Subject;
        order.Description = orderDto.Description;
        order.DetailedDescription = orderDto.DetailedDescription;
        order.DiscountAmount = orderDto.DiscountAmount;
        order.DiscountPercentage = orderDto.DiscountPercentage;
        order.DeliveryDate = orderDto.DeliveryDate;
        order.CompanyName = orderDto.CompanyName;
        order.PaymentTerms = orderDto.PaymentTerms;
        order.ShippingMethod = orderDto.ShippingMethod;
        order.OrderType = orderDto.OrderType;
        order.ReferenceNumber = orderDto.ReferenceNumber;
        order.QuoteId = orderDto.QuoteId;
        order.SiteId = orderDto.SiteId;
        order.CurrencyId = orderDto.CurrencyId.Value;

        // Handle OrderItems
        _logger.LogInformation("Processing {ItemCount} OrderItems for OrderId: {OrderId}", orderDto.OrderItems.Count, orderId);

        var updateItemIds = orderDto.OrderItems
            .Where(i => i.OrderItemId != 0)
            .Select(i => i.OrderItemId)
            .ToList();
        var itemsToRemove = order.OrderItems
            .Where(oi => !updateItemIds.Contains(oi.OrderItemId))
            .ToList();
        _context.OrderItems.RemoveRange(itemsToRemove);

        foreach (var itemDto in orderDto.OrderItems)
        {
            if (!itemDto.ProductId.HasValue || itemDto.ProductId <= 0)
                throw new ArgumentException($"Érvénytelen ProductId: {itemDto.ProductId} az {itemDto.OrderItemId} tételhez.");

            if (!await _context.Products.AnyAsync(p => p.ProductId == itemDto.ProductId.Value))
                throw new ArgumentException($"Nem létező ProductId: {itemDto.ProductId} az {itemDto.OrderItemId} tételhez.");

            var item = order.OrderItems.FirstOrDefault(oi => oi.OrderItemId == itemDto.OrderItemId);
            if (item == null && itemDto.OrderItemId == 0)
            {
                item = new OrderItem { OrderId = orderId };
                order.OrderItems.Add(item);
            }
            else if (item == null)
            {
                throw new ArgumentException($"Érvénytelen OrderItemId: {itemDto.OrderItemId}");
            }

            item.ProductId = itemDto.ProductId.Value;
            item.Quantity = itemDto.Quantity ?? 1;
            item.UnitPrice = itemDto.UnitPrice ?? 0;
            item.Description = itemDto.Description;
            item.DiscountPercentage = itemDto.DiscountPercentage;
            item.DiscountAmount = itemDto.DiscountAmount;
            item.TotalPrice = (itemDto.Quantity ?? 1) * (itemDto.UnitPrice ?? 0) - (itemDto.DiscountAmount ?? 0);
        }

        order.ModifiedBy = modifiedBy ?? "System";
        order.ModifiedDate = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        await transaction.CommitAsync();
        _logger.LogInformation("Successfully updated Order ID: {OrderId} with {ItemCount} items", orderId, order.OrderItems.Count);
    }
    catch (DbUpdateException ex)
    {
        await transaction.RollbackAsync();
        _logger.LogError(ex, "Database error saving OrderId: {OrderId}. Inner exception: {InnerException}", orderId, ex.InnerException?.Message);
        throw new InvalidOperationException($"Adatbázis hiba a rendelés mentése során: {ex.InnerException?.Message}", ex);
    }
    catch (Exception ex)
    {
        await transaction.RollbackAsync();
        _logger.LogError(ex, "Unexpected error saving OrderId: {OrderId}: {Message}", orderId, ex.Message);
        throw;
    }

    // Fetch updated order using safe projection to DTO
    try
    {
        var updatedOrderDto = await _context.Orders
            .Where(o => o.OrderId == orderId)
            .Select(order => new OrderDto
            {
                OrderId = order.OrderId,
                OrderNumber = order.OrderNumber,
                OrderDate = order.OrderDate,
                Deadline = order.Deadline,
                Description = order.Description,
                TotalAmount = order.TotalAmount,
                SalesPerson = order.SalesPerson,
                DeliveryDate = order.DeliveryDate,
                DiscountPercentage = order.DiscountPercentage,
                DiscountAmount = order.DiscountAmount,
                CompanyName = order.CompanyName,
                Subject = order.Subject,
                DetailedDescription = order.DetailedDescription,
                CreatedBy = order.CreatedBy,
                CreatedDate = order.CreatedDate,
                ModifiedBy = order.ModifiedBy,
                ModifiedDate = order.ModifiedDate,
                Status = order.Status ?? "Unknown",
                PartnerId = order.PartnerId,
                Partner = order.Partner == null ? null : new PartnerDto
                {
                    PartnerId = order.Partner.PartnerId,
                    Name = order.Partner.Name ?? "Unknown",
                    // Add other PartnerDto fields with ?? "" as needed
                },
                SiteId = order.SiteId,
                Site = order.Site == null ? null : new SiteDto
                {
                    SiteId = order.Site.SiteId,
                    Address = order.Site.City // or whatever property you use
                    // Add other SiteDto fields with ?? "" as needed
                },
                CurrencyId = order.CurrencyId,
                Currency = order.Currency == null ? null : new CurrencyDto
                {
                    CurrencyId = order.Currency.CurrencyId,
                    CurrencyName = order.Currency.CurrencyName ?? "Unknown"
                    // Add other CurrencyDto fields with ?? "" as needed
                },
                PaymentTerms = order.PaymentTerms,
                ShippingMethod = order.ShippingMethod,
                OrderType = order.OrderType,
                OrderItems = order.OrderItems.Select(oi => new OrderItemDto
                {
                    OrderItemId = oi.OrderItemId,
                    OrderId = oi.OrderId,
                    ProductId = oi.ProductId,
                    Product = oi.Product == null ? null : new ProductDto
                    {
                        ProductId = oi.Product.ProductId,
                        Name = oi.Product.Name ?? "Unknown",
                        // Add other ProductDto fields with ?? "" as needed
                    },
                    Quantity = oi.Quantity,
                    UnitPrice = oi.UnitPrice,
                    Description = oi.Description,
                    DiscountPercentage = oi.DiscountPercentage,
                    DiscountAmount = oi.DiscountAmount
                }).ToList(),
                ReferenceNumber = order.ReferenceNumber,
                QuoteId = order.QuoteId,
                Quote = order.Quote == null ? null : new QuoteDto
                {
                    QuoteId = order.Quote.QuoteId
                    // Add other QuoteDto fields with ?? "" as needed
                }
            })
            .FirstOrDefaultAsync();

        if (updatedOrderDto == null)
        {
            throw new KeyNotFoundException($"Frissített rendelés nem található: {orderId}");
        }

        return updatedOrderDto;
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error fetching updated order for OrderId: {OrderId}: {Message}", orderId, ex.Message);
        throw new InvalidOperationException($"Hiba a frissített rendelés lekérdezése során: {ex.Message}", ex);
    }
}

        public async Task<bool> DeleteOrderAsync(int orderId)
        {
            var order = await _context.Orders
                .Include(q => q.OrderItems)
                .FirstOrDefaultAsync(q => q.OrderId == orderId);

            if (order == null)
                return false;

            // Remove other related entities if any
            _context.OrderItems.RemoveRange(order.OrderItems);

            // Add more as needed

            _context.Orders.Remove(order);

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<OrderItemResponseDto> CreateOrderItemAsync(int OrderId, CreateOrderItemDto itemDto)
        {
            _logger.LogInformation("CreateOrderItemAsync called for OrderId: {OrderId}, ItemDto: {ItemDto}",
                OrderId, JsonSerializer.Serialize(itemDto));

            if (itemDto == null)
            {
                _logger.LogWarning("CreateOrderItemAsync received null ItemDto for OrderId: {OrderId}", OrderId);
                throw new ArgumentNullException(nameof(itemDto));
            }

            if (_context == null)
            {
                _logger.LogError("Database context is null for CreateOrderItemAsync OrderId: {OrderId}", OrderId);
                throw new InvalidOperationException("Adatbázis kapcsolat nem érhető el");
            }

            var Order = await _context.Orders.FirstOrDefaultAsync(q => q.OrderId == OrderId);
            if (Order == null)
            {
                _logger.LogWarning("Order not found for OrderId: {OrderId}", OrderId);
                return null;
            }

            var product = await _context.Products.FirstOrDefaultAsync(p => p.ProductId == itemDto.ProductId);
            if (product == null)
            {
                _logger.LogWarning("Product not found for ProductId: {ProductId}", itemDto.ProductId);
                throw new ArgumentException($"Érvénytelen ProductId: {itemDto.ProductId}");
            }

            var OrderItem = new OrderItem
            {
                OrderId = OrderId,
                ProductId = itemDto.ProductId,
                Quantity = itemDto.Quantity,
                UnitPrice = itemDto.UnitPrice,
                Description = itemDto.Description ?? "", // Ensure empty string if null
                DiscountPercentage = itemDto.DiscountPercentage,
                DiscountAmount = itemDto.DiscountAmount
            };

            _context.OrderItems.Add(OrderItem);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Created Order item ID: {OrderItemId} for OrderId: {OrderId}", OrderItem.OrderItemId, OrderId);
            return new OrderItemResponseDto
            {
                OrderItemId = OrderItem.OrderItemId,
                OrderId = OrderItem.OrderId,
                ProductId = OrderItem.ProductId,
                Quantity = OrderItem.Quantity,
                UnitPrice = OrderItem.UnitPrice,
                DiscountPercentage = OrderItem.DiscountPercentage,
                DiscountAmount = OrderItem.DiscountAmount
            };
        }

public async Task<OrderItemResponseDto> UpdateOrderItemAsync(int orderId, int orderItemId, UpdateOrderItemDto itemDto)
{
    _logger.LogInformation("UpdateOrderItemAsync called for OrderId: {OrderId}, OrderItemId: {OrderItemId}, ItemDto: {ItemDto}",
        orderId, orderItemId, JsonSerializer.Serialize(itemDto));

    if (itemDto == null)
    {
        _logger.LogWarning("UpdateOrderItemAsync received null ItemDto for OrderId: {OrderId}", orderId);
        throw new ArgumentNullException(nameof(itemDto));
    }

    if (_context == null)
    {
        _logger.LogError("Database context is null for UpdateOrderItemAsync OrderId: {OrderId}", orderId);
        throw new InvalidOperationException("Adatbázis kapcsolat nem érhető el");
    }

    var orderItem = await _context.OrderItems
        .FirstOrDefaultAsync(q => q.OrderId == orderId && q.OrderItemId == orderItemId);
    if (orderItem == null)
    {
        _logger.LogWarning("Order item not found for OrderId: {OrderId}, OrderItemId: {OrderItemId}", orderId, orderItemId);
        return null;
    }

    // Update fields if provided
    if (itemDto.ProductId.HasValue)
    {
        var product = await _context.Products.FindAsync(itemDto.ProductId.Value);
        if (product == null)
        {
            _logger.LogWarning("Product not found for ProductId: {ProductId}", itemDto.ProductId);
            throw new ArgumentException($"Érvénytelen ProductId: {itemDto.ProductId}");
        }
        orderItem.ProductId = itemDto.ProductId.Value;
    }
    if (itemDto.Quantity.HasValue) orderItem.Quantity = itemDto.Quantity.Value;
    if (itemDto.UnitPrice.HasValue) orderItem.UnitPrice = itemDto.UnitPrice.Value;
    if (itemDto.Description != null) orderItem.Description = itemDto.Description;
    if (itemDto.DiscountPercentage.HasValue) orderItem.DiscountPercentage = itemDto.DiscountPercentage;
    if (itemDto.DiscountAmount.HasValue) orderItem.DiscountAmount = itemDto.DiscountAmount;

    await _context.SaveChangesAsync();

    _logger.LogInformation("Updated Order item ID: {OrderItemId} for OrderId: {OrderId}", orderItemId, orderId);

    return new OrderItemResponseDto
    {
        OrderItemId = orderItem.OrderItemId,
        OrderId = orderItem.OrderId,
        ProductId = orderItem.ProductId,
        Quantity = orderItem.Quantity,
        UnitPrice = orderItem.UnitPrice,
        Description = orderItem.Description,
        DiscountPercentage = orderItem.DiscountPercentage,
        DiscountAmount = orderItem.DiscountAmount
    };
}

        public async Task<bool> DeleteOrderItemAsync(int OrderId, int OrderItemId)
        {
            var item = await _context.OrderItems
                .FirstOrDefaultAsync(qi => qi.OrderId == OrderId && qi.OrderItemId == OrderItemId);

            if (item == null)
            {
                return false;
            }

            _context.OrderItems.Remove(item);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<OrderDto> CopyOrderAsync(int sourceOrderId)
        {
            try
            {
                _logger.LogInformation("Copying order with ID: {OrderId}", sourceOrderId);

                var sourceOrder = await GetOrderByIdAsync(sourceOrderId);
                if (sourceOrder == null)
                {
                    _logger.LogWarning("Source order not found with ID: {OrderId}", sourceOrderId);
                    throw new ArgumentException($"Order with ID {sourceOrderId} not found");
                }

                var createOrderDto = MapToCreateOrderDto(sourceOrder);
                var newOrder = await CreateOrderAsync(createOrderDto);
                _logger.LogInformation("Copied order {SourceOrderId} to new order {NewOrderId}", sourceOrderId, newOrder.OrderId);

                return newOrder;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error copying order with ID: {OrderId}", sourceOrderId);
                throw;
            }
        }

        public async Task<List<OrderDto>> GetOrdersAsync(string searchTerm, string statusFilter, string sortBy, int skip, int take)
        {
            var query = _context.Orders
                .Include(o => o.Partner)
                .Include(o => o.Currency)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .AsQueryable();

            if (!string.IsNullOrEmpty(searchTerm))
            {
                searchTerm = searchTerm.ToLower();
                query = query.Where(o => o.OrderNumber.ToLower().Contains(searchTerm) ||
                                        o.Partner.Name.ToLower().Contains(searchTerm));
            }

            if (!string.IsNullOrEmpty(statusFilter) && statusFilter != "all")
            {
                query = query.Where(o => o.Status == statusFilter);
            }

            query = sortBy switch
            {
                "OrderId" => query.OrderByDescending(o => o.OrderId),
                "ValidityDate" => query.OrderBy(o => o.Deadline),
                _ => query.OrderByDescending(o => o.OrderDate)
            };

            var orders = await query
                .Skip(skip)
                .Take(take)
                .Select(o => new OrderDto
                {
                    OrderId = o.OrderId,
                    OrderNumber = o.OrderNumber,
                    PartnerId = o.PartnerId,
                    Partner = new PartnerDto
                    {
                        PartnerId = o.Partner.PartnerId,
                        Name = o.Partner.Name
                    },
                    CurrencyId = o.CurrencyId,
                    Currency = new CurrencyDto
                    {
                        CurrencyId = o.Currency.CurrencyId,
                        CurrencyName = o.Currency.CurrencyName
                    },
                    OrderDate = o.OrderDate,
                    Status = o.Status,
                    TotalAmount = o.TotalAmount,
                    SalesPerson = o.SalesPerson,
                    Deadline = o.Deadline,
                    Subject = o.Subject,
                    Description = o.Description,
                    DetailedDescription = o.DetailedDescription,
                    DiscountPercentage = o.DiscountPercentage,
                    DiscountAmount = o.DiscountAmount,
                    OrderItems = o.OrderItems.Select(i => new OrderItemDto
                    {
                        OrderItemId = i.OrderItemId,
                        OrderId = i.OrderId,
                        Quantity = i.Quantity,
                        ProductId = i.ProductId,
                        Product = i.Product == null ? new ProductDto { ProductId = 0, Name = "" } : new ProductDto
                        {
                            ProductId = i.Product.ProductId,
                            Name = i.Product.Name ?? ""
                        },
                        UnitPrice = i.UnitPrice,
                        Description = i.Description,
                        DiscountPercentage = i.DiscountPercentage,
                        DiscountAmount = i.DiscountAmount
                    }).ToList()
                })
                .ToListAsync();

            foreach (var order in orders)
            {
                _logger.LogInformation($"Order {order.OrderId}: {order.OrderItems.Count} items");
            }

            return orders;
        }

        public async Task<OrderDto> GetOrderAsync(int orderId)
        {
            try
            {
                _logger.LogInformation("Fetching order with ID: {OrderId}", orderId);

                var order = await _context.Orders
                    .Include(o => o.OrderItems) // Include related OrderItems
                    .Where(o => o.OrderId == orderId)
                    .Select(o => new OrderDto
                    {
                        OrderId = o.OrderId,
                        OrderNumber = o.OrderNumber,
                        PartnerId = o.PartnerId,
                        CurrencyId = o.CurrencyId,
                        SiteId = o.SiteId,
                        QuoteId = o.QuoteId,
                        OrderDate = o.OrderDate,
                        Deadline = o.Deadline,
                        DeliveryDate = o.DeliveryDate,
                        ReferenceNumber = o.ReferenceNumber,
                        OrderType = o.OrderType,
                        CompanyName = o.CompanyName  ?? "",
                        TotalAmount = o.TotalAmount,
                        DiscountPercentage = o.DiscountPercentage,
                        DiscountAmount = o.DiscountAmount,
                        PaymentTerms = o.PaymentTerms,
                        ShippingMethod = o.ShippingMethod,
                        SalesPerson = o.SalesPerson   ?? "",
                        Subject = o.Subject,
                        Description = o.Description,
                        DetailedDescription = o.DetailedDescription,
                        Status = o.Status,
                        CreatedBy = o.CreatedBy,
                        CreatedDate = o.CreatedDate,
                        ModifiedBy = o.ModifiedBy,
                        ModifiedDate = o.ModifiedDate,
                        OrderItems = o.OrderItems.Select(i => new OrderItemDto
                        {
                            OrderItemId = i.OrderItemId,
                            ProductId = i.ProductId,
                            Quantity = i.Quantity,
                            UnitPrice = i.UnitPrice,
                            DiscountPercentage = i.DiscountPercentage,
                            DiscountAmount = i.DiscountAmount,
                            Description = i.Description,
                            OrderId = i.OrderId
                        }).ToList()
                    })
                    .FirstOrDefaultAsync();

                if (order == null)
                {
                    _logger.LogWarning("Order not found with ID: {OrderId}", orderId);
                }
                else
                {
                    _logger.LogInformation("Retrieved order with ID: {OrderId}", orderId);
                }

                return order;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching order with ID: {OrderId}", orderId);
                throw;
            }
        }

        public async Task<OrderDto> GetOrderByIdAsync(int orderId)
        {
            var order = await _context.Orders
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .Include(o => o.Partner)
                .Include(o => o.Currency)
                .Where(o => o.OrderId == orderId)
                .Select(o => new OrderDto
                {
                    OrderId = o.OrderId,
                    OrderNumber = o.OrderNumber,
                    PartnerId = o.PartnerId,
                    Partner = new PartnerDto
                    {
                        PartnerId = o.Partner.PartnerId,
                        Name = o.Partner.Name
                    },
                    CurrencyId = o.CurrencyId,
                    Currency = new CurrencyDto
                    {
                        CurrencyId = o.Currency.CurrencyId,
                        CurrencyName = o.Currency.CurrencyName
                    },
                    OrderDate = o.OrderDate,
                    Status = o.Status,
                    TotalAmount = o.TotalAmount,
                    SalesPerson = o.SalesPerson,
                    Deadline = o.Deadline,
                    Subject = o.Subject,
                    Description = o.Description,
                    DetailedDescription = o.DetailedDescription,
                    DiscountPercentage = o.DiscountPercentage,
                    DiscountAmount = o.DiscountAmount,
                    OrderItems = o.OrderItems.Select(i => new OrderItemDto
                    {
                        OrderItemId = i.OrderItemId,
                        OrderId = i.OrderId,
                        ProductId = i.ProductId,
                        Product = i.Product == null ? null : new ProductDto
                        {
                            ProductId = i.Product.ProductId,
                            Name = i.Product.Name
                        },
                        Quantity = i.Quantity,
                        UnitPrice = i.UnitPrice,
                        Description = i.Description,
                        DiscountPercentage = i.DiscountPercentage,
                        DiscountAmount = i.DiscountAmount
                    }).ToList()
                })
                .FirstOrDefaultAsync();

            if (order != null)
            {
                _logger.LogInformation($"Order {order.OrderId}: {order.OrderItems.Count} items");
            }
            else
            {
                _logger.LogWarning($"Order {orderId} not found");
            }

            return order;
        }

private OrderDto MapToOrderDto(Order order)
    {
        return new OrderDto
        {
            OrderId = order.OrderId,
            OrderNumber = order.OrderNumber,
            OrderDate = order.OrderDate,
            Deadline = order.Deadline,
            Description = order.Description,
            TotalAmount = order.TotalAmount,
            SalesPerson = order.SalesPerson,
            DeliveryDate = order.DeliveryDate,
            DiscountPercentage = order.DiscountPercentage,
            DiscountAmount = order.DiscountAmount,
            CompanyName = order.CompanyName,
            Subject = order.Subject,
            DetailedDescription = order.DetailedDescription,
            CreatedBy = order.CreatedBy,
            CreatedDate = order.CreatedDate,
            ModifiedBy = order.ModifiedBy,
            ModifiedDate = order.ModifiedDate,
            Status = order.Status ?? "Unknown",
            PartnerId = order.PartnerId,
            Partner = order.Partner != null ? new PartnerDto
            {
                PartnerId = order.Partner.PartnerId,
                Name = order.Partner.Name ?? "Unknown",
                // Add other PartnerDto fields
            } : null,
            SiteId = order.SiteId,
            Site = order.Site != null ? new SiteDto
            {
                SiteId = order.Site.SiteId,
                Address = order.Site.City
                // Add other SiteDto fields
            } : null,
            CurrencyId = order.CurrencyId,
            Currency = order.Currency != null ? new CurrencyDto
            {
                CurrencyId = order.Currency.CurrencyId,
                CurrencyName = order.Currency.CurrencyName
                // Add other CurrencyDto fields
            } : null,
            PaymentTerms = order.PaymentTerms,
            ShippingMethod = order.ShippingMethod,
            OrderType = order.OrderType,
            OrderItems = order.OrderItems?.Select(oi => new OrderItemDto
            {
                OrderItemId = oi.OrderItemId,
                OrderId = oi.OrderId,
                ProductId = oi.ProductId,
                Product = oi.Product != null ? new ProductDto
                {
                    ProductId = oi.Product.ProductId,
                    Name = oi.Product.Name ?? "Unknown",
                    // Add other ProductDto fields
                } : null,
                Quantity = oi.Quantity,
                UnitPrice = oi.UnitPrice,
                Description = oi.Description,
                DiscountPercentage = oi.DiscountPercentage,
                DiscountAmount = oi.DiscountAmount
            }).ToList(),
            ReferenceNumber = order.ReferenceNumber,
            QuoteId = order.QuoteId,
            Quote = order.Quote != null ? new QuoteDto
            {
                QuoteId = order.Quote.QuoteId
                // Add other QuoteDto fields
            } : null
        };
    }
        
        private CreateOrderDto MapToCreateOrderDto(OrderDto source)
        {
            return new CreateOrderDto
            {
                PartnerId = source.PartnerId,
                CurrencyId = source.CurrencyId,
                SiteId = source.SiteId,
                QuoteId = source.QuoteId,
                OrderNumber = null, // Server generates TestOrder-XXXX-YYYY
                OrderDate = DateTime.UtcNow, // New date
                Deadline = source.Deadline,
                DeliveryDate = source.DeliveryDate,
                ReferenceNumber = source.ReferenceNumber,
                OrderType = source.OrderType,
                CompanyName = source.CompanyName,
                TotalAmount = source.TotalAmount,
                DiscountPercentage = source.DiscountPercentage,
                DiscountAmount = source.DiscountAmount,
                PaymentTerms = source.PaymentTerms,
                ShippingMethod = source.ShippingMethod,
                SalesPerson = source.SalesPerson,
                Subject = source.Subject,
                Description = source.Description,
                DetailedDescription = source.DetailedDescription,
                Status = "Draft", // New orders start as Draft
                CreatedBy = source.CreatedBy ?? "System",
                CreatedDate = DateTime.UtcNow,
                ModifiedBy = source.ModifiedBy,
                ModifiedDate = DateTime.UtcNow,
                OrderItems = source.OrderItems?.Select(i => new CreateOrderItemDto
                {
                    ProductId = i.ProductId,
                    Quantity = i.Quantity,
                    UnitPrice = i.UnitPrice,
                    DiscountPercentage = i.DiscountPercentage,
                    DiscountAmount = i.DiscountAmount,
                    Description = i.Description
                }).ToList() ?? new List<CreateOrderItemDto>()
            };
        }
    }
}