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
using Microsoft.AspNetCore.Authorization;

namespace Cloud9_2.Pages.CRM.Orders
{
    [Authorize]
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly OrderService _orderService;
        private readonly ILogger<IndexModel> _logger;

        public IndexModel(ApplicationDbContext context, OrderService orderService, ILogger<IndexModel> logger)
        {
            _context = context;
            _orderService = orderService;
            _logger = logger;
        }

        public IList<Order> Orders { get; set; } = new List<Order>();

        [BindProperty]
        public OrderCreateDTO OrderCreateDTO { get; set; }

        [BindProperty]
        public OrderUpdateDTO OrderUpdateDTO { get; set; }

        [BindProperty(SupportsGet = true)]
        public string SearchTerm { get; set; }

        [BindProperty(SupportsGet = true)]
        public int CurrentPage { get; set; } = 1;

        [BindProperty(SupportsGet = true)]
        public int PageSize { get; set; } = 10;

        public int TotalRecords { get; set; }
        public int TotalPages { get; set; }

        public async Task<IActionResult> OnGetAsync(string searchTerm, int? pageNumber, int? pageSize, string sort)
        {
            try
            {
                _logger.LogInformation("Fetching orders for page {Page}, searchTerm: {SearchTerm}, user: {User}",
                    pageNumber ?? 1, searchTerm, User.Identity?.Name);

                SearchTerm = searchTerm;
                CurrentPage = pageNumber ?? 1;
                PageSize = pageSize ?? 10;

                var orders = await _orderService.GetAllOrdersAsync();
                orders = sort switch
                {
                    "OrderDate" => orders.OrderByDescending(o => o.OrderDate).ToList(),
                    "OrderId" => orders.OrderByDescending(o => o.OrderId).ToList(),
                    "Deadline" => orders.OrderBy(o => o.Deadline ?? DateTime.MaxValue).ToList(),
                    _ => orders.OrderBy(o => o.OrderId).ToList()
                };
                _logger.LogInformation("Retrieved {Count} orders", orders.Count);

                if (!string.IsNullOrEmpty(searchTerm))
                {
                    orders = orders.Where(o => o.OrderNumber.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                                              (o.CompanyName != null && o.CompanyName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)) ||
                                              (o.Partner?.Name != null && o.Partner.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)))
                                   .ToList();
                    _logger.LogInformation("After search, {Count} orders remain", orders.Count);
                }

                TotalRecords = orders.Count;
                TotalPages = (int)Math.Ceiling((double)TotalRecords / PageSize);
                _logger.LogInformation("TotalRecords: {TotalRecords}, TotalPages: {TotalPages}", TotalRecords, TotalPages);

                if (CurrentPage < 1) CurrentPage = 1;
                if (CurrentPage > TotalPages && TotalPages > 0)
                {
                    _logger.LogWarning("Requested page {CurrentPage} exceeds total pages {TotalPages}, redirecting", CurrentPage, TotalPages);
                    return RedirectToPage("./Index", new { SearchTerm, PageNumber = TotalPages, PageSize });
                }

                Orders = orders.Skip((CurrentPage - 1) * PageSize).Take(PageSize).ToList();
                _logger.LogInformation("Paginated orders for page {CurrentPage}: {Count}", CurrentPage, Orders.Count);

                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading orders for page {CurrentPage}", CurrentPage);
                TempData["ErrorMessage"] = "Error loading orders. Please try again later.";
                return Page();
            }
        }
    
        public async Task<IActionResult> OnPostCreateAsync()
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Invalid model state for order creation");
                TempData["ErrorMessage"] = "Invalid order data.";
                return Page();
            }

            try
            {
                var userId = User.Identity?.Name ?? "System";
                var order = await _orderService.CreateOrderAsync(OrderCreateDTO, userId);
                _logger.LogInformation("Created order with ID {OrderId}", order.OrderId);
                TempData["SuccessMessage"] = "Order created successfully.";
                return RedirectToPage();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating order");
                TempData["ErrorMessage"] = "Error creating order.";
                return Page();
            }
        }

        public async Task<IActionResult> OnPostUpdateAsync(int id)
        {
            if (!ModelState.IsValid || OrderUpdateDTO.OrderId != id)
            {
                _logger.LogWarning("Invalid model state or ID mismatch for order {OrderId}", id);
                TempData["ErrorMessage"] = "Invalid order data.";
                return Page();
            }

            try
            {
                var userId = User.Identity?.Name ?? "System";
                var existingOrder = await _orderService.GetOrderByIdAsync(id);
                if (existingOrder == null)
                {
                    _logger.LogWarning("Order with ID {OrderId} not found", id);
                    TempData["ErrorMessage"] = $"Order with ID {id} not found.";
                    return Page();
                }

                if (existingOrder.CreatedBy != userId)
                {
                    _logger.LogWarning("User {UserId} attempted to update order {OrderId} they do not own", userId, id);
                    TempData["ErrorMessage"] = "You do not have permission to update this order.";
                    return Page();
                }

                var updatedOrder = await _orderService.UpdateOrderAsync(OrderUpdateDTO, userId);
                if (updatedOrder == null)
                {
                    _logger.LogWarning("Order with ID {OrderId} not found during update", id);
                    TempData["ErrorMessage"] = $"Order with ID {id} not found.";
                    return Page();
                }

                _logger.LogInformation("Updated order with ID {OrderId}", id);
                TempData["SuccessMessage"] = "Order updated successfully.";
                return RedirectToPage();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating order {OrderId}", id);
                TempData["ErrorMessage"] = "Error updating order.";
                return Page();
            }
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            try
            {
                var userId = User.Identity?.Name ?? "System";
                var order = await _orderService.GetOrderByIdAsync(id);
                if (order == null)
                {
                    _logger.LogWarning("Order with ID {OrderId} not found", id);
                    TempData["ErrorMessage"] = $"Order with ID {id} not found.";
                    return Page();
                }

                if (order.CreatedBy != userId)
                {
                    _logger.LogWarning("User {UserId} attempted to delete order {OrderId} they do not own", userId, id);
                    TempData["ErrorMessage"] = "You do not have permission to delete this order.";
                    return Page();
                }

                var result = await _orderService.DeleteOrderAsync(id);
                if (!result)
                {
                    _logger.LogWarning("Order with ID {OrderId} not found during deletion", id);
                    TempData["ErrorMessage"] = $"Order with ID {id} not found.";
                    return Page();
                }

                _logger.LogInformation("Deleted order with ID {OrderId}", id);
                TempData["SuccessMessage"] = "Order deleted successfully.";
                return RedirectToPage();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting order {OrderId}", id);
                TempData["ErrorMessage"] = "Error deleting order.";
                return Page();
            }
        }
    }
}