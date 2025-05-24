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

        public OrderService(ApplicationDbContext context, ILogger<OrderService> logger)
        {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<string> GetNextOrderNumberAsync()
        {
            var lastOrderNumber = await _context.Orders
                .Where(q => q.OrderNumber != null && q.OrderNumber.StartsWith("Order-"))
                .OrderByDescending(q => q.OrderNumber)
                .Select(q => q.OrderNumber)
                .FirstOrDefaultAsync();

            string nextNumber = "Order-0001";
            if (!string.IsNullOrEmpty(lastOrderNumber) && int.TryParse(lastOrderNumber.Replace("Order-", ""), out int lastNumber))
            {
                nextNumber = $"Order-{lastNumber + 1:D4}";
            }

            return nextNumber;
        }

        public async Task<bool> OrderExistsAsync(int OrderId)
        {
            return await _context.Orders.AnyAsync(q => q.OrderId == OrderId);
        }

        // public async Task<List<OrderItemDto>> GetOrderItemsAsync(int OrderId)
        // {
        //     return await _context.OrderItems
        //         .Where(qi => qi.OrderId == OrderId)
        //         .Select(qi => new OrderItemDto
        //         {
        //             OrderItemId = qi.OrderItemId,
        //             OrderId = qi.OrderId,
        //             ProductId = qi.ProductId,
        //             Quantity = qi.Quantity,
        //             UnitPrice = qi.UnitPrice,
        //             Description = qi.Description,
        //             DiscountPercentage = qi.DiscountPercentage,
        //             DiscountAmount = qi.DiscountAmount,
        //             TotalPrice = qi.Quantity * qi.UnitPrice
        //         })
        //         .ToListAsync();
        // }

        public async Task<OrderDto> CreateOrderAsync(CreateOrderDto OrderDto)
        {
            var Order = new Order
            {
                OrderNumber = await GetNextOrderNumberAsync(),
                PartnerId = OrderDto.PartnerId,
                OrderDate = OrderDto.OrderDate ?? DateTime.UtcNow,
                Status = "Draft",
                TotalAmount = OrderDto.TotalAmount
            };

            _context.Orders.Add(Order);
            await _context.SaveChangesAsync();

            return new OrderDto
            {
                OrderId = Order.OrderId,
                OrderNumber = Order.OrderNumber,
                PartnerId = Order.PartnerId,
                OrderDate = Order.OrderDate,
                Status = Order.Status,
                TotalAmount = Order.TotalAmount
            };
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

public async Task<OrderDto> UpdateOrderAsync(int OrderId, UpdateOrderDto OrderDto)
        {
            _logger.LogInformation("UpdateOrderAsync called for OrderId: {OrderId}, OrderDto: {OrderDto}", 
                OrderId, JsonSerializer.Serialize(OrderDto));

            if (OrderDto == null)
            {
                _logger.LogWarning("UpdateOrderAsync received null OrderDto for OrderId: {OrderId}", OrderId);
                throw new ArgumentNullException(nameof(OrderDto));
            }

            if (_context == null)
            {
                _logger.LogError("Database context is null for UpdateOrderAsync OrderId: {OrderId}", OrderId);
                throw new InvalidOperationException("Adatbázis kapcsolat nem érhető el");
            }

            var Order = await _context.Orders.FirstOrDefaultAsync(q => q.OrderId == OrderId);
            if (Order == null)
            {
                _logger.LogWarning("Order not found for OrderId: {OrderId}", OrderId);
                return null;
            }

            Order.OrderNumber = OrderDto.OrderNumber;
            Order.PartnerId = OrderDto.PartnerId;
            Order.OrderDate = OrderDto.OrderDate;
            Order.Status = OrderDto.Status;
            Order.TotalAmount = OrderDto.TotalAmount;
            Order.SalesPerson = OrderDto.SalesPerson;
            Order.Deadline = OrderDto.Deadline;
            Order.Subject = OrderDto.Subject;
            Order.Description = OrderDto.Description;
            Order.DetailedDescription = OrderDto.DetailedDescription;
            Order.DiscountAmount = OrderDto.DiscountAmount;
            Order.DiscountPercentage = OrderDto.DiscountPercentage;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Updated Order ID: {OrderId}", OrderId);
            return new OrderDto
            {
                OrderId = Order.OrderId,
                OrderNumber = Order.OrderNumber,
                PartnerId = Order.PartnerId,
                OrderDate = Order.OrderDate,
                Status = Order.Status,
                TotalAmount = Order.TotalAmount,
                SalesPerson = Order.SalesPerson,
                Deadline = Order.Deadline,
                Subject = Order.Subject,
                Description = Order.Description,
                DetailedDescription = Order.DetailedDescription
            };
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
        Description = itemDto.ItemDescription ?? "", // Ensure empty string if null
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
        ItemDescription = OrderItem.Description,
        DiscountPercentage = OrderItem.DiscountPercentage,
        DiscountAmount = OrderItem.DiscountAmount
    };
}

public async Task<OrderItemResponseDto> UpdateOrderItemAsync(int OrderId, int OrderItemId, UpdateOrderItemDto itemDto)
        {
            _logger.LogInformation("UpdateOrderItemAsync called for OrderId: {OrderId}, OrderItemId: {OrderItemId}, ItemDto: {ItemDto}", 
                OrderId, OrderItemId, JsonSerializer.Serialize(itemDto));

            if (itemDto == null)
            {
                _logger.LogWarning("UpdateOrderItemAsync received null ItemDto for OrderId: {OrderId}", OrderId);
                throw new ArgumentNullException(nameof(itemDto));
            }

            if (_context == null)
            {
                _logger.LogError("Database context is null for UpdateOrderItemAsync OrderId: {OrderId}", OrderId);
                throw new InvalidOperationException("Adatbázis kapcsolat nem érhető el");
            }

            var OrderItem = await _context.OrderItems
                .FirstOrDefaultAsync(q => q.OrderId == OrderId && q.OrderItemId == OrderItemId);
            if (OrderItem == null)
            {
                _logger.LogWarning("Order item not found for OrderId: {OrderId}, OrderItemId: {OrderItemId}", OrderId, OrderItemId);
                return null;
            }

            var product = await _context.Products.FirstOrDefaultAsync(p => p.ProductId == itemDto.ProductId);
            if (product == null)
            {
                _logger.LogWarning("Product not found for ProductId: {ProductId}", itemDto.ProductId);
                throw new ArgumentException($"Érvénytelen ProductId: {itemDto.ProductId}");
            }

            OrderItem.ProductId = itemDto.ProductId;
            OrderItem.Quantity = itemDto.Quantity;
            OrderItem.UnitPrice = itemDto.UnitPrice;
            OrderItem.Description = itemDto.Description;
            OrderItem.DiscountPercentage = itemDto.DiscountPercentage;
            OrderItem.DiscountAmount = itemDto.DiscountAmount;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Updated Order item ID: {OrderItemId} for OrderId: {OrderId}", OrderItemId, OrderId);
            return new OrderItemResponseDto
            {
                OrderItemId = OrderItem.OrderItemId,
                OrderId = OrderItem.OrderId,
                ProductId = OrderItem.ProductId,
                Quantity = OrderItem.Quantity,
                UnitPrice = OrderItem.UnitPrice,
                Description = OrderItem.Description,
                DiscountPercentage = OrderItem.DiscountPercentage,
                DiscountAmount = OrderItem.DiscountAmount
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

        public async Task<OrderDto> CopyOrderAsync(int OrderId)
        {
            var originalOrder = await _context.Orders
                .Include(q => q.OrderItems)
                .FirstOrDefaultAsync(q => q.OrderId == OrderId);

            if (originalOrder == null)
            {
                throw new KeyNotFoundException($"Order with ID {OrderId} not found");
            }

            var newOrder = new Order
            {
                OrderNumber = await GetNextOrderNumberAsync(),
                PartnerId = originalOrder.PartnerId,
                OrderDate = DateTime.UtcNow,
                Status = "Draft",
                TotalAmount = originalOrder.TotalAmount,
                OrderItems = originalOrder.OrderItems.Select(qi => new OrderItem
                {
                    ProductId = qi.ProductId,
                    Quantity = qi.Quantity,
                    UnitPrice = qi.UnitPrice,
                    Description = qi.Description,
                    DiscountPercentage = qi.DiscountPercentage,
                    DiscountAmount = qi.DiscountAmount
                }).ToList()
            };

            _context.Orders.Add(newOrder);
            await _context.SaveChangesAsync();

            return new OrderDto
            {
                OrderId = newOrder.OrderId,
                OrderNumber = newOrder.OrderNumber,
                PartnerId = newOrder.PartnerId,
                OrderDate = newOrder.OrderDate,
                Status = newOrder.Status,
                TotalAmount = newOrder.TotalAmount
            };
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
    }
}