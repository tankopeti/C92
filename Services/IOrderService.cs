using Cloud9_2.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Cloud9_2.Services
{
    public interface IOrderService
    {
        Task<string> GetNextOrderNumberAsync();
        Task<bool> OrderExistsAsync(int OrderId);
        Task<List<PartnerDto>> GetPartnersAsync();
        Task<List<OrderItemDto>> GetOrderItemsAsync(int OrderId);
        Task<OrderDto> CreateOrderAsync(CreateOrderDto OrderDto);
        Task<OrderDto> GetOrderByIdAsync(int OrderId);
        Task<OrderDto> UpdateOrderAsync(int OrderId, UpdateOrderDto OrderDto);
        Task<bool> DeleteOrderAsync(int OrderId);
        Task<OrderItemResponseDto> CreateOrderItemAsync(int OrderId, CreateOrderItemDto itemDto);
        Task<OrderItemResponseDto> UpdateOrderItemAsync(int OrderId, int OrderItemId, UpdateOrderItemDto itemDto);
        Task<bool> DeleteOrderItemAsync(int OrderId, int OrderItemId);
        Task<OrderDto> CopyOrderAsync(int OrderId);
        Task<List<OrderDto>> GetOrdersAsync(string searchTerm, string statusFilter, string sortBy, int skip, int take);
    }
}