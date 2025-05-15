using AutoMapper;
using Cloud9_2.Data;
using Cloud9_2.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Cloud9_2.Services
{
    public class OrderService : IOrderService
    {
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;

        public OrderService(ApplicationDbContext context, IMapper mapper)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        }

        public async Task<IEnumerable<OrderDto>> GetOrdersAsync(int page, int pageSize, string statusFilter, string searchTerm)
        {
            var query = _context.Orders
                .Include(o => o.Partner)
                .Include(o => o.Currency)
                .Include(o => o.OrderItems).ThenInclude(oi => oi.Product)
                .AsQueryable();

            if (!string.IsNullOrEmpty(statusFilter))
            {
                query = query.Where(o => o.Status == statusFilter);
            }

            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(o => o.OrderNumber.Contains(searchTerm) || o.Partner.Name.Contains(searchTerm));
            }

            query = query.OrderBy(o => o.OrderDate);
            var orders = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return _mapper.Map<IEnumerable<OrderDto>>(orders);
        }

        public async Task<string> GetNextOrderNumberAsync()
        {
            var lastOrder = await _context.Orders
                .OrderByDescending(o => o.OrderNumber)
                .FirstOrDefaultAsync();

            if (lastOrder == null || string.IsNullOrEmpty(lastOrder.OrderNumber))
            {
                return "ORD-000001";
            }

            var numberPart = lastOrder.OrderNumber.Replace("ORD-", "");
            if (int.TryParse(numberPart, out var number))
            {
                return $"ORD-{++number:D6}";
            }

            return "ORD-000001";
        }

        public async Task<OrderDto> CreateOrderAsync(OrderDto orderDto)
        {
            var order = _mapper.Map<Order>(orderDto);
            order.OrderNumber = await GetNextOrderNumberAsync();
            order.CreatedDate = DateTime.UtcNow;
            order.ModifiedDate = DateTime.UtcNow;
            order.OrderItems?.ForEach(oi => oi.OrderItemId = 0);
            _context.Orders.Add(order);
            await _context.SaveChangesAsync();
            return _mapper.Map<OrderDto>(order);
        }

        public async Task<OrderDto> UpdateOrderAsync(int id, OrderDto orderDto)
        {
            var order = await _context.Orders
                .Include(o => o.OrderItems)
                .FirstOrDefaultAsync(o => o.OrderId == id);
            if (order == null)
            {
                throw new KeyNotFoundException("Order not found.");
            }

            _mapper.Map(orderDto, order);
            order.ModifiedDate = DateTime.UtcNow;
            _context.OrderItems.RemoveRange(order.OrderItems);
            order.OrderItems = _mapper.Map<List<OrderItem>>(orderDto.OrderItems);
            order.OrderItems.ForEach(oi => { oi.OrderId = id; oi.OrderItemId = 0; });

            await _context.SaveChangesAsync();
            return _mapper.Map<OrderDto>(order);
        }

        public async Task<OrderDto> CopyOrderAsync(int id)
        {
            var order = await _context.Orders
                .Include(o => o.OrderItems)
                .FirstOrDefaultAsync(o => o.OrderId == id);
            if (order == null)
            {
                throw new KeyNotFoundException("Order not found.");
            }

            var newOrder = _mapper.Map<Order>(order);
            newOrder.OrderId = 0;
            newOrder.OrderNumber = await GetNextOrderNumberAsync();
            newOrder.CreatedDate = DateTime.UtcNow;
            newOrder.ModifiedDate = DateTime.UtcNow;
            newOrder.OrderItems.ForEach(oi => oi.OrderItemId = 0);
            _context.Orders.Add(newOrder);
            await _context.SaveChangesAsync();
            return _mapper.Map<OrderDto>(newOrder);
        }

        public async Task<bool> DeleteOrderAsync(int id)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null)
            {
                return false;
            }

            _context.Orders.Remove(order);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<IEnumerable<PartnerDto>> GetPartnersAsync(string searchTerm)
        {
            var query = _context.Partners.AsQueryable();
            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(p => p.Name.Contains(searchTerm));
            }

            var partners = await query
                .Take(100)
                .ToListAsync();
            return _mapper.Map<IEnumerable<PartnerDto>>(partners);
        }

        public async Task<IEnumerable<ProductDto>> GetProductsAsync(string searchTerm)
        {
            var query = _context.Products.AsQueryable();
            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(p => p.Name.Contains(searchTerm));
            }

            var products = await query
                .Take(100)
                .ToListAsync();
            return _mapper.Map<IEnumerable<ProductDto>>(products);
        }

        public async Task<IEnumerable<OrderItemDto>> GetOrderItemsAsync(int orderId)
        {
            var order = await _context.Orders
                .Include(o => o.OrderItems).ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(o => o.OrderId == orderId);
            if (order == null)
            {
                throw new KeyNotFoundException("Order not found.");
            }

            return _mapper.Map<IEnumerable<OrderItemDto>>(order.OrderItems);
        }

        public async Task<OrderDto> GetOrderByIdAsync(int orderId)
        {
            var order = await _context.Orders
                .Include(o => o.Partner)
                .Include(o => o.Currency)
                .Include(o => o.OrderItems).ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(o => o.OrderId == orderId);
            if (order == null)
            {
                throw new KeyNotFoundException("Order not found.");
            }

            return _mapper.Map<OrderDto>(order);
        }

        public async Task<OrderItemDto> CreateOrderItemAsync(int orderId, OrderItemDto itemDto)
        {
            var order = await _context.Orders.FindAsync(orderId);
            if (order == null)
            {
                throw new KeyNotFoundException("Order not found.");
            }

            if (itemDto.ProductId.HasValue && !await _context.Products.AnyAsync(p => p.ProductId == itemDto.ProductId))
            {
                throw new ArgumentException("Invalid ProductId.");
            }

            var orderItem = _mapper.Map<OrderItem>(itemDto);
            orderItem.OrderId = orderId;
            orderItem.OrderItemId = 0;
            _context.OrderItems.Add(orderItem);
            await _context.SaveChangesAsync();
            return _mapper.Map<OrderItemDto>(orderItem);
        }

        public async Task<OrderItemDto> UpdateOrderItemAsync(int orderId, int orderItemId, OrderItemDto itemDto)
        {
            var orderItem = await _context.OrderItems
                .FirstOrDefaultAsync(oi => oi.OrderId == orderId && oi.OrderItemId == orderItemId);
            if (orderItem == null)
            {
                throw new KeyNotFoundException("Order item not found.");
            }

            if (itemDto.ProductId.HasValue && !await _context.Products.AnyAsync(p => p.ProductId == itemDto.ProductId))
            {
                throw new ArgumentException("Invalid ProductId.");
            }

            _mapper.Map(itemDto, orderItem);
            await _context.SaveChangesAsync();
            return _mapper.Map<OrderItemDto>(orderItem);
        }

        public async Task<bool> DeleteOrderItemAsync(int orderId, int orderItemId)
        {
            var orderItem = await _context.OrderItems
                .FirstOrDefaultAsync(oi => oi.OrderId == orderId && oi.OrderItemId == orderItemId);
            if (orderItem == null)
            {
                return false;
            }

            _context.OrderItems.Remove(orderItem);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}