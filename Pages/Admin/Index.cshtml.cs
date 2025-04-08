using Microsoft.AspNetCore.Authorization; // Added for [Authorize]
using Microsoft.AspNetCore.Mvc.RazorPages;


namespace Cloud9_2.Pages.Admin;

    [Authorize(Roles = "SuperAdmin, Admin")]

public class IndexModel : PageModel
{
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(ILogger<IndexModel> logger)
    {
        _logger = logger;
    }

    public void OnGet()
    {

    }
}
