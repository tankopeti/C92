using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Cloud9_2.Models;
using Cloud9_2.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Cloud9_2.Pages.DocManagement.Search
{
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<IndexModel> _logger;

        public IndexModel(ApplicationDbContext context, ILogger<IndexModel> logger)
        {
            _context = context;
            _logger = logger;
        }

        [BindProperty(SupportsGet = true)]
        public string? SearchTerm { get; set; }

        public List<DocumentResult> Results { get; set; } = new();

        public async Task OnGetAsync()
        {
            Results = new List<DocumentResult>();
            if (string.IsNullOrWhiteSpace(SearchTerm))
                return;

            Results = await SearchAsync(SearchTerm);
        }

        // ✅ /DocManagement/Search?handler=LiveResults&searchTerm=...
public async Task<IActionResult> OnGetLiveResultsAsync(string searchTerm)
{
    // DEBUG: response headerekbe írjuk ki
    Response.Headers["X-Debug-SearchTerm"] = searchTerm ?? "";
    Response.Headers["X-Debug-DbProvider"] = _context.Database.ProviderName ?? "";
    Response.Headers["X-Debug-CanConnect"] = (await _context.Database.CanConnectAsync()).ToString();

    var count = await _context.Documents.AsNoTracking().CountAsync();
    Response.Headers["X-Debug-DocumentsCount"] = count.ToString();

    // Ha akarod, connection stringet ne tedd ki headerbe (biztonság),
    // inkább csak a DB nevét próbáld kinyerni. (Providerfüggő.)
    // Response.Headers["X-Debug-Conn"] = _context.Database.GetDbConnection().ConnectionString;

    try
    {
        Results = new List<DocumentResult>();

        if (string.IsNullOrWhiteSpace(searchTerm))
            return BuildPartial("_LiveResultsPartial", Results);

        Results = await SearchAsync(searchTerm);

        Response.Headers["X-Debug-ResultsCount"] = Results.Count.ToString();
        return BuildPartial("_LiveResultsPartial", Results);
    }
    catch (Exception ex)
    {
        Response.Headers["X-Debug-Exception"] = ex.GetType().Name;
        Results = new List<DocumentResult>();
        return BuildPartial("_LiveResultsPartial", Results);
    }
}


        private IActionResult BuildPartial(string viewName, List<DocumentResult> model)
        {
            // Razor Pages-ben ez a legstabilabb módja a partial visszaadásnak
            var vd = new ViewDataDictionary<List<DocumentResult>>(ViewData, model);

            return new PartialViewResult
            {
                ViewName = viewName,
                ViewData = vd
            };
        }

private async Task<List<DocumentResult>> SearchAsync(string searchTerm)
{
    var q = searchTerm.Trim();

    if (q.Length == 0)
        return new List<DocumentResult>();

    var docs = await _context.Documents
        .AsNoTracking()
        .Include(d => d.DocumentMetadata)
        .Where(d =>
            (d.FileName != null && d.FileName.Contains(q)) ||
            (d.FilePath != null && d.FilePath.Contains(q)) ||
            d.DocumentMetadata.Any(m =>
                (m.Key != null && m.Key.Contains(q)) ||
                (m.Value != null && m.Value.Contains(q))
            )
        )
        .OrderByDescending(d => d.UploadDate)
        .Take(50)
        .ToListAsync();

    return docs.Select(d => new DocumentResult
    {
        Id = d.DocumentId,
        FileName = d.FileName ?? "",
        Metadata = d.DocumentMetadata
            .GroupBy(m => m.Key)
            .ToDictionary(
                g => g.Key,
                g => string.Join(", ", g.Select(m => m.Value))
            )
    }).ToList();
}

        private static Expression<Func<T, bool>> OrElse<T>(
            Expression<Func<T, bool>> expr1,
            Expression<Func<T, bool>> expr2)
        {
            var parameter = Expression.Parameter(typeof(T));

            var left = ReplaceParameter(expr1.Body, expr1.Parameters[0], parameter);
            var right = ReplaceParameter(expr2.Body, expr2.Parameters[0], parameter);

            return Expression.Lambda<Func<T, bool>>(
                Expression.OrElse(left, right),
                parameter);
        }

        private static Expression ReplaceParameter(Expression body, ParameterExpression from, ParameterExpression to)
            => new ReplaceVisitor(from, to).Visit(body);

        private sealed class ReplaceVisitor : ExpressionVisitor
        {
            private readonly ParameterExpression _from;
            private readonly ParameterExpression _to;

            public ReplaceVisitor(ParameterExpression from, ParameterExpression to)
            {
                _from = from;
                _to = to;
            }

            protected override Expression VisitParameter(ParameterExpression node)
                => node == _from ? _to : base.VisitParameter(node);
        }

        public class DocumentResult
        {
            public int Id { get; set; }
            public string FileName { get; set; } = "";
            public Dictionary<string, string> Metadata { get; set; } = new();
        }
    }
}
