using Microsoft.AspNetCore.Mvc;
using Cloud9_2.Models;
using Cloud9_2.Data;
using Microsoft.EntityFrameworkCore;

namespace Cloud9_2.Controllers
{
    [Route("api/vat")]
    public class VatController : Controller
    {
        private readonly ApplicationDbContext _context;

        public VatController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet("types")]
        public async Task<IActionResult> GetVatTypes()
        {
            var vatTypes = await _context.VatTypes
                .Select(v => new VatTypeDto
                {
                    VatTypeId = v.VatTypeId,
                    TypeName = v.TypeName,
                    Rate = v.Rate,
                    FormattedRate = $"{v.Rate}%"
                })
                .ToListAsync();

            if (!vatTypes.Any())
            {
                return NotFound("No VAT types found.");
            }

            return Json(vatTypes);
        }
    }
}

