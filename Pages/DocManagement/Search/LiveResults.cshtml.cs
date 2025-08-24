using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Cloud9_2.Data;
using Cloud9_2.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Cloud9_2.Pages.DocManagement.Search
{
    public class LiveResultsModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public LiveResultsModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public List<Document> Documents { get; set; } = new();

        public async Task OnGetAsync(string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                Documents = new List<Document>();
                return;
            }

            var terms = searchTerm.Split(' ', System.StringSplitOptions.RemoveEmptyEntries);

            Documents = await _context.Documents
                .Include(d => d.DocumentMetadata)
                .Where(d =>
                    terms.Any(term =>
                        d.FileName.Contains(term) ||
                        d.DocumentMetadata.Any(m =>
                            m.Key.Contains(term) ||
                            m.Value.Contains(term))))
                .ToListAsync();
        }
    }
}
