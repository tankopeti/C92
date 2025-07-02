using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Cloud9_2.Data;
using Cloud9_2.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Cloud9_2.Pages.Logistics.Products
{
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public IList<Product> Products { get; set; }
        public int TotalRecords { get; set; }
        public int CurrentPage { get; set; }
        public int PageSize { get; set; } = 10;
        public int TotalPages { get; set; }
        public string SearchTerm { get; set; }

        public async Task OnGetAsync(int? pageNumber, int? pageSize, string searchTerm)
        {
            SearchTerm = searchTerm;
            CurrentPage = pageNumber ?? 1;
            PageSize = pageSize ?? PageSize;

            try
            {
                var query = _context.Products
                    .Include(p => p.Category)
                    .AsQueryable();

                if (!string.IsNullOrEmpty(SearchTerm))
                {
                    query = query.Where(p => p.SKU.Contains(SearchTerm) || p.Name.Contains(SearchTerm));
                }

                TotalRecords = await query.CountAsync();
                Console.WriteLine($"TotalRecords: {TotalRecords}");

                TotalPages = (int)Math.Ceiling(TotalRecords / (double)PageSize);

                if (CurrentPage < 1) CurrentPage = 1;
                if (CurrentPage > TotalPages && TotalPages > 0) CurrentPage = TotalPages;

                Products = await query
                    .OrderBy(p => p.Name)
                    .Skip((CurrentPage - 1) * PageSize)
                    .Take(PageSize)
                    .ToListAsync();

                Console.WriteLine($"Fetched {Products.Count} products");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching products: {ex.Message}");
            }
        }
    }
}