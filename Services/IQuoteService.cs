using Cloud9_2.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Cloud9_2.Services
{
    public interface IQuoteService
    {
        Task<string> GetNextQuoteNumberAsync();
        Task<bool> QuoteExistsAsync(int quoteId);
        Task<List<PartnerDto>> GetPartnersAsync();
        Task<List<QuoteItemDto>> GetQuoteItemsAsync(int quoteId);
        Task<QuoteDto> CreateQuoteAsync(CreateQuoteDto quoteDto);
        Task<QuoteDto> GetQuoteByIdAsync(int quoteId);
        Task<QuoteDto> UpdateQuoteAsync(int quoteId, UpdateQuoteDto quoteDto);
        Task<bool> DeleteQuoteAsync(int quoteId);
        Task<QuoteItemResponseDto> CreateQuoteItemAsync(int quoteId, CreateQuoteItemDto itemDto);
        Task<QuoteItemResponseDto> UpdateQuoteItemAsync(int quoteId, int quoteItemId, UpdateQuoteItemDto itemDto);
        Task<bool> DeleteQuoteItemAsync(int quoteId, int quoteItemId);
        Task<QuoteDto> CopyQuoteAsync(int quoteId);
        Task<OrderDto> ConvertQuoteToOrderAsync(int quoteId, ConvertQuoteToOrderDto convertDto, string createdBy);
    }
}