using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Cloud9_2.Pages.DocManagement.DocIn
{
    public class IndexModel : PageModel
    {
        public int PageSize { get; set; } = 20;

        public void OnGet()
        {
            if (int.TryParse(Request.Query["pageSize"], out var ps) && ps > 0 && ps <= 200)
            {
                PageSize = ps;
            }
        }
    }
}
