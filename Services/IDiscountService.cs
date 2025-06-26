using Cloud9_2.Models;
using System.Threading.Tasks;

namespace Cloud9_2.Services
{
    public interface IDiscountService
    {
        Task<DiscountDto> GetDiscountAsync(string entityType, int itemId);
        Task<DiscountDto> CreateDiscountAsync(string entityType, DiscountDto discountDto);
        Task<DiscountDto> UpdateDiscountAsync(string entityType, int itemId, DiscountDto discountDto);
        Task<bool> DeleteDiscountAsync(string entityType, int itemId);
    }
}