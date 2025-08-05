using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Cloud9_2.Models;
using Cloud9_2.Data;
using Cloud9_2.Services;

namespace Cloud9_2.Pages.CRM.Orders
{
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<IndexModel> _logger;
        private readonly IOrderService _orderService;

        public IndexModel(ApplicationDbContext context, ILogger<IndexModel> logger, IOrderService orderService)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _orderService = orderService ?? throw new ArgumentNullException(nameof(orderService));
        }

        public int CurrentPage { get; set; }
        public string SearchTerm { get; set; }
        public int PageSize { get; set; }
        public string StatusFilter { get; set; }
        public string SortBy { get; set; }
        public int TotalRecords { get; set; }
        public int TotalPages { get; set; }
        public int DistinctOrderIdCount { get; set; }
        public string NextOrderNumber { get; set; }
        public Order SelectedOrder { get; set; }
        public IList<Order> Orders { get; set; }
        public IDictionary<string, string> StatusDisplayNames { get; set; } = new Dictionary<string, string>
        {
            { "Draft", "Piszkozat" },
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
            SortBy = sortBy ?? "orderdate";

            _logger.LogInformation("Fetching Orders: Page={Page}, PageSize={PageSize}, SearchTerm={SearchTerm}, StatusFilter={StatusFilter}, SortBy={SortBy}, OrderId={OrderId}", 
                CurrentPage, PageSize, SearchTerm, StatusFilter, SortBy, orderId);

            NextOrderNumber = await _orderService.GetNextOrderNumberAsync();

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
                .Include(p => p.Quote)
                .Include(q => q.OrderItems)!
                .ThenInclude(qi => qi.Product);

            if (!string.IsNullOrEmpty(SearchTerm))
            {
                SearchTerm = SearchTerm.ToLower();
                OrdersQuery = OrdersQuery.Where(q => (q.OrderNumber != null && q.OrderNumber.ToLower().Contains(SearchTerm)) ||
                                                    (q.Subject != null && q.Subject.ToLower().Contains(SearchTerm)) ||
                                                    (q.Partner != null && q.Partner.Name != null && q.Partner.Name.ToLower().Contains(SearchTerm)) ||
                                                    (q.Description != null && q.Description.ToLower().Contains(SearchTerm)));
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
            DistinctOrderIdCount = await OrdersQuery.Select(q => q.OrderId).Distinct().CountAsync();
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
    }
}