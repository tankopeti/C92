using Cloud9_2.Models;
using Cloud9_2.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Cloud9_2.Pages.CRM.Orders
{
    public class IndexModel : PageModel
    {
        private readonly IOrderService _orderService;

        public IndexModel(IOrderService orderService)
        {
            _orderService = orderService ?? throw new ArgumentNullException(nameof(orderService));
        }

        public IList<OrderDto> Orders { get; set; } = new List<OrderDto>();
        public string SearchTerm { get; set; }
        public string StatusFilter { get; set; }
        public string SortBy { get; set; }
        public int CurrentPage { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public int TotalPages { get; set; }
        public string NextOrderNumber { get; set; }

        public async Task OnGetAsync(int pageNumber = 1, int pageSize = 10, string searchTerm = null, string statusFilter = null, string sortBy = null)
        {
            try
            {
                CurrentPage = pageNumber < 1 ? 1 : pageNumber;
                PageSize = pageSize < 1 ? 10 : pageSize;
                SearchTerm = searchTerm;
                StatusFilter = statusFilter;
                SortBy = sortBy ?? "OrderDate";

                // Fetch orders
                var orders = await _orderService.GetOrdersAsync(CurrentPage, PageSize, StatusFilter, null);

                // Apply search filter (client-side for simplicity; ideally, move to service with search support)
                Orders = orders.ToList();
                if (!string.IsNullOrEmpty(SearchTerm))
                {
                    var lowerSearch = SearchTerm.ToLower();
                    Orders = Orders.Where(o =>
                        o.OrderNumber?.ToLower().Contains(lowerSearch) == true ||
                        o.Partner?.Name?.ToLower().Contains(lowerSearch) == true
                    ).ToList();
                }

                // Apply sorting
                Orders = SortBy switch
                {
                    "OrderNumber" => Orders.OrderBy(o => o.OrderNumber).ToList(),
                    "PartnerName" => Orders.OrderBy(o => o.Partner?.Name).ToList(),
                    "OrderDate" => Orders.OrderBy(o => o.OrderDate).ToList(),
                    "TotalAmount" => Orders.OrderBy(o => o.TotalAmount).ToList(),
                    _ => Orders.OrderBy(o => o.OrderDate).ToList()
                };

                // Calculate total pages
                TotalPages = (int)Math.Ceiling(Orders.Count / (double)PageSize);
                Orders = Orders.Skip((CurrentPage - 1) * PageSize).Take(PageSize).ToList();

                // Get next order number
                NextOrderNumber = await _orderService.GetNextOrderNumberAsync();
            }
            catch (Exception ex)
            {
                // Log error (in production, use ILogger)
                TempData["ErrorMessage"] = "Hiba történt az adatok betöltése közben.";
            }
        }
    }
}