using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Cloud9_2.Models;
using Cloud9_2.Data;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Cloud9_2.Pages.DocManagement.Search
{
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty(SupportsGet = true)]
        public string SearchTerm { get; set; }

        public List<DocumentResult> Results { get; set; } = new();

        public async Task OnGetAsync()
        {
            if (string.IsNullOrWhiteSpace(SearchTerm))
            {
                Results = new List<DocumentResult>();
                return;
            }

            var terms = SearchTerm
                .Split(' ', System.StringSplitOptions.RemoveEmptyEntries)
                .Select(t => t.Trim())
                .ToList();

            var query = await _context.Documents
                .Include(d => d.DocumentMetadata)
                .Where(d =>
                    terms.Any(term =>
                        d.FileName.Contains(term) ||
                        d.DocumentMetadata.Any(m =>
                            m.Key.Contains(term) ||
                            m.Value.Contains(term))))
                .ToListAsync();

            Results = query.Select(d => new DocumentResult
            {
                Id = d.DocumentId,
                FileName = d.FileName,
                Metadata = d.DocumentMetadata
                    .GroupBy(m => m.Key)
                    .ToDictionary(
                        g => g.Key,
                        g => string.Join(", ", g.Select(m => m.Value))
                    )
            }).ToList();
        }

        public class DocumentResult
        {
            public int Id { get; set; }
            public string FileName { get; set; }
            public Dictionary<string, string> Metadata { get; set; }
        }
    }
}
