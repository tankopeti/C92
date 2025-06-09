using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Cloud9_2.Data;
using System.Threading.Tasks;
using System.Linq;

namespace Cloud9_2.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PartnerGroupsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public PartnerGroupsController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetPartnerGroups(string search = "")
        {
            var partnerGroups = await _context.PartnerGroups
                .Where(pg => string.IsNullOrEmpty(search) || pg.PartnerGroupName.Contains(search))
                .Select(pg => new
                {
                    id = pg.PartnerGroupId,
                    text = pg.PartnerGroupName,
                    discountPercentage = pg.DiscountPercentage
                })
                .ToListAsync();

            return Ok(partnerGroups);
        }
    }
}