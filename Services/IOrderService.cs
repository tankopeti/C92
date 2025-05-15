using Cloud9_2.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Cloud9_2.Services
{
    public interface IOrderService
    {
        Task<IEnumerable<OrderDto>> GetOrdersAsync(int page, int pageSize, string statusFilter, string searchTerm);
        Task<string> GetNextOrderNumberAsync();
        Task<OrderDto> CreateOrderAsync(OrderDto orderDto);
        Task<OrderDto> UpdateOrderAsync(int id, OrderDto orderDto);
        Task<OrderDto> CopyOrderAsync(int id);
        Task<bool> DeleteOrderAsync(int id);
        Task<IEnumerable<PartnerDto>> GetPartnersAsync(string searchTerm);
        Task<IEnumerable<ProductDto>> GetProductsAsync(string searchTerm);
        Task<IEnumerable<OrderItemDto>> GetOrderItemsAsync(int orderId);
        Task<OrderDto> GetOrderByIdAsync(int orderId);
        Task<OrderItemDto> CreateOrderItemAsync(int orderId, OrderItemDto itemDto);
        Task<OrderItemDto> UpdateOrderItemAsync(int orderId, int orderItemId, OrderItemDto itemDto);
        Task<bool> DeleteOrderItemAsync(int orderId, int orderItemId);
    }
}