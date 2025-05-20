using Cloud9_2.Data;
using Cloud9_2.Models;
using Cloud9_2.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using AutoMapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Cloud9_2.Pages.CRM.Orders
{
    public class IndexModel : PageModel
    {
        private readonly IOrderService _orderService;
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;
        private readonly ILogger<IndexModel> _logger;

        public IndexModel(IOrderService orderService, ApplicationDbContext context, IMapper mapper, ILogger<IndexModel> logger)
        {
            _orderService = orderService ?? throw new ArgumentNullException(nameof(orderService));
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public IList<OrderDto> Orders { get; set; } = new List<OrderDto>();
        public OrderDto SelectedOrder { get; set; }
        [BindProperty(SupportsGet = true)]
        public string SearchTerm { get; set; }
        [BindProperty(SupportsGet = true)]
        public string StatusFilter { get; set; }
        [BindProperty(SupportsGet = true)]
        public string SortBy { get; set; }
        [BindProperty(SupportsGet = true)]
        public int CurrentPage { get; set; } = 1;
        [BindProperty(SupportsGet = true)]
        public int PageSize { get; set; } = 10;
        public int TotalRecords { get; set; }
        public int TotalPages { get; set; }
        public int DistinctOrderIdCount { get; set; }
        public string NextOrderNumber { get; set; }
        public Dictionary<string, string> StatusDisplayNames { get; set; } = new Dictionary<string, string>
        {
            { "Pending", "Függőben" },
            { "Confirmed", "Megerősítve" },
            { "Shipped", "Kiszállítva" },
            { "Cancelled", "Törölve" }
        };

public async Task OnGetAsync(int? pageNumber, string searchTerm, int? pageSize, string statusFilter, string sortBy, int? orderId)
        {
            CurrentPage = pageNumber ?? 1;
            SearchTerm = searchTerm;
            PageSize = pageSize ?? 10;
            StatusFilter = statusFilter;
            SortBy = sortBy;

            _logger.LogInformation("Fetching Orders: Page={Page}, PageSize={PageSize}, SearchTerm={SearchTerm}, StatusFilter={StatusFilter}, SortBy={SortBy}, OrderId={OrderId}", 
                CurrentPage, PageSize, SearchTerm, StatusFilter, SortBy, orderId);

            if (orderId.HasValue)
            {
                SelectedOrder = await _orderService.GetOrderByIdAsync(orderId.Value);
                if (SelectedOrder == null)
                {
                    _logger.LogWarning("Order {OrderId} not found", orderId);
                }
                else
                {
                    _logger.LogInformation("Retrieved Order {OrderId} with {ItemCount} items", orderId, SelectedOrder.OrderItems?.Count ?? 0);
                }
            }

            IQueryable<Order> OrdersQuery = _context.Orders
                .Include(q => q.Partner)
                .Include(q => q.OrderItems)
                    .ThenInclude(qi => qi.Product);

            if (!string.IsNullOrEmpty(SearchTerm))
            {
                SearchTerm = SearchTerm.ToLower();
                OrdersQuery = OrdersQuery.Where(q => q.OrderNumber.ToLower().Contains(SearchTerm) ||
                                                    q.Subject.ToLower().Contains(SearchTerm) ||
                                                    q.Partner.Name.ToLower().Contains(SearchTerm) ||
                                                    q.Description.ToLower().Contains(SearchTerm));
            }

            if (!string.IsNullOrEmpty(StatusFilter) && StatusFilter != "all")
            {
                OrdersQuery = OrdersQuery.Where(q => q.Status == StatusFilter);
            }

            OrdersQuery = SortBy switch
            {
                "OrderId" => OrdersQuery.OrderByDescending(q => q.OrderId),
                "ValidityDate" => OrdersQuery.OrderBy(q => q.Deadline),
                _ => OrdersQuery.OrderByDescending(q => q.OrderDate)
            };

            TotalRecords = await OrdersQuery.CountAsync();
            TotalPages = (int)Math.Ceiling(TotalRecords / (double)PageSize);
            CurrentPage = Math.Max(1, Math.Min(CurrentPage, TotalPages));

            Orders = await _orderService.GetOrdersAsync(
                SearchTerm,
                StatusFilter,
                SortBy,
                (CurrentPage - 1) * PageSize,
                PageSize);

            _logger.LogInformation("Retrieved {Count} Orders for page {Page}. TotalRecords={TotalRecords}, TotalPages={TotalPages}, StatusFilter={StatusFilter}, SortBy={SortBy}", 
                Orders.Count, CurrentPage, TotalRecords, TotalPages, StatusFilter, SortBy);

            if (!Orders.Any() && TotalRecords > 0)
            {
                _logger.LogWarning("No Orders found for page {Page}, but TotalRecords={TotalRecords}. Possible pagination or filter issue.", CurrentPage, TotalRecords);
            }
        }

        // public async Task<IActionResult> OnPostCreateAsync([FromBody] OrderDto orderDto)
        // {
        //     if (!ModelState.IsValid)
        //     {
        //         _logger.LogWarning("Invalid model state: {Errors}", string.Join("; ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
        //         return BadRequest(new { success = false, message = "Érvénytelen adatok." });
        //     }

        //     try
        //     {
        //         var order = _mapper.Map<Order>(orderDto);
        //         order.CreatedDate = DateTime.UtcNow;
        //         order.ModifiedDate = DateTime.UtcNow;
        //         await _orderService.CreateOrderAsync(order);
        //         return new JsonResult(new { success = true, message = "Rendelés sikeresen létrehozva." });
        //     }
        //     catch (Exception ex)
        //     {
        //         _logger.LogError(ex, "Error creating order: {Message}", ex.Message);
        //         return StatusCode(500, new { success = false, message = "Hiba történt a rendelés létrehozása közben." });
        //     }
        // }

        // public async Task<IActionResult> OnPostEditAsync([FromBody] OrderDto orderDto)
        // {
        //     if (!ModelState.IsValid)
        //     {
        //         _logger.LogWarning("Invalid model state: {Errors}", string.Join("; ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
        //         return BadRequest(new { success = false, message = "Érvénytelen adatok." });
        //     }

        //     try
        //     {
        //         var order = _mapper.Map<Order>(orderDto);
        //         order.ModifiedDate = DateTime.UtcNow;
        //         await _orderService.UpdateOrderAsync(order);
        //         return new JsonResult(new { success = true, message = "Rendelés sikeresen frissítve." });
        //     }
        //     catch (Exception ex)
        //     {
        //         _logger.LogError(ex, "Error updating order: {Message}", ex.Message);
        //         return StatusCode(500, new { success = false, message = "Hiba történt a rendelés frissítése közben." });
        //     }
        // }

        public async Task<IActionResult> OnPostDeleteAsync(int orderId)
        {
            try
            {
                var success = await _orderService.DeleteOrderAsync(orderId);
                if (!success)
                {
                    return BadRequest(new { success = false, message = "A rendelés nem található." });
                }
                return new JsonResult(new { success = true, message = "Rendelés sikeresen törölve." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting order: {Message}", ex.Message);
                return StatusCode(500, new { success = false, message = "Hiba történt a rendelés törlése közben." });
            }
        }

        public async Task<IActionResult> OnGetProductsAsync(string search)
        {
            try
            {
                var products = await _context.Products
                    .Where(p => string.IsNullOrEmpty(search) || p.Name.Contains(search))
                    .Select(p => new { id = p.ProductId, name = p.Name })
                    .Take(10)
                    .ToListAsync();
                return new JsonResult(products);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching products: {Message}", ex.Message);
                return StatusCode(500, new { success = false, message = "Hiba történt a termékek lekérdezése közben." });
            }
        }

        public async Task<IActionResult> OnGetCurrenciesAsync(string search)
        {
            try
            {
                var currencies = await _context.Currencies
                    .Where(c => string.IsNullOrEmpty(search) || c.CurrencyName.Contains(search))
                    .Select(c => new { id = c.CurrencyId, name = c.CurrencyName })
                    .Take(10)
                    .ToListAsync();
                return new JsonResult(currencies);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching currencies: {Message}", ex.Message);
                return StatusCode(500, new { success = false, message = "Hiba történt a pénznemek lekérdezése közben." });
            }
        }

        public async Task<IActionResult> OnGetPartnersAsync(string search)
        {
            try
            {
                var partners = await _context.Partners
                    .Where(p => string.IsNullOrEmpty(search) || p.Name.Contains(search))
                    .Select(p => new { id = p.PartnerId, name = p.Name })
                    .Take(10)
                    .ToListAsync();
                return new JsonResult(partners);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching partners: {Message}", ex.Message);
                return StatusCode(500, new { success = false, message = "Hiba történt a partnerek lekérdezése közben." });
            }
        }
    }
}