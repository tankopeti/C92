using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Cloud9_2.Models;
using Cloud9_2.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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

        public async Task<Order> CreateOrderAsync(OrderCreateDTO orderDto, string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            var userName = user?.UserName ?? "System";

            var order = new Order
            {
                OrderNumber = orderDto.OrderNumber,
                OrderDate = orderDto.OrderDate,
                Deadline = orderDto.Deadline,
                Description = orderDto.Description,
                TotalAmount = orderDto.TotalAmount,
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
                CreatedBy = userName,
                CreatedDate = DateTime.UtcNow,
                ModifiedBy = userName,
                ModifiedDate = DateTime.UtcNow,
                OrderItems = orderDto.OrderItems?.Select(item => new OrderItem
                {
                    Description = item.Description,
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice,
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

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();
            return order;
        }

        public async Task<Order?> GetOrderByIdAsync(int orderId)
        {
            return await _context.Orders
                .Include(o => o.OrderItems)
                .Include(o => o.Partner)
                .Include(o => o.Site)
                .Include(o => o.Currency)
                .Include(o => o.ShippingMethod)
                .Include(o => o.PaymentTerm)
                .Include(o => o.Contact)
                .Include(o => o.Quote)
                .FirstOrDefaultAsync(o => o.OrderId == orderId);
        }

        public async Task<List<Order>> GetAllOrdersAsync()
        {
            try
            {
                var orders = await _context.Orders
                    .Include(o => o.OrderItems)
                    .Include(o => o.Partner)
                    .Include(o => o.Currency)
                    .AsNoTracking() // Optional: Improves performance for read-only queries
                    .ToListAsync();
                _logger.LogInformation("Retrieved {Count} orders from database", orders.Count);
                return orders;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving orders from database");
                throw; // Let the controller handle the error
            }
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

            order.OrderNumber = orderDto.OrderNumber;
            order.OrderDate = orderDto.OrderDate;
            order.Deadline = orderDto.Deadline;
            order.Description = orderDto.Description;
            order.TotalAmount = orderDto.TotalAmount;
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

            if (orderDto.OrderItems != null)
            {
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

                foreach (var itemDto in orderDto.OrderItems)
                {
                    var existingItem = order.OrderItems.FirstOrDefault(i => i.OrderItemId == itemDto.OrderItemId);
                    if (existingItem != null)
                    {
                        existingItem.Description = itemDto.Description;
                        existingItem.Quantity = itemDto.Quantity;
                        existingItem.UnitPrice = itemDto.UnitPrice;
                        existingItem.DiscountAmount = itemDto.DiscountAmount;
                        existingItem.DiscountType = itemDto.DiscountType;
                        existingItem.ProductId = itemDto.ProductId;
                        existingItem.VatTypeId = itemDto.VatTypeId;
                        existingItem.ModifiedBy = userName;
                        existingItem.ModifiedDate = DateTime.UtcNow;
                    }
                    else
                    {
                        order.OrderItems.Add(new OrderItem
                        {
                            OrderId = order.OrderId,
                            Description = itemDto.Description,
                            Quantity = itemDto.Quantity,
                            UnitPrice = itemDto.UnitPrice,
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

            await _context.SaveChangesAsync();
            return order;
        }

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