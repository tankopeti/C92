using Cloud9_2.Data;
using Cloud9_2.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;

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

                // GET: api/vat/types?search=abc
        [HttpGet("GetVatTypesForSelect")]
        public async Task<IActionResult> GetVatTypesForSelect([FromQuery] string search = "")
        {
            try
            {

                if (_context.VatTypes == null)
                {
                    return StatusCode(500, new { title = "Internal server error", errors = new { General = new[] { "VAT types configuration is missing in the database context" } } });
                }

                var vatTypes = await _context.VatTypes
                    .AsNoTracking()
                    .Where(v => string.IsNullOrEmpty(search) || v.TypeName.Contains(search))
                    .OrderBy(v => v.TypeName)
                    .Select(v => new
                    {
                        id = v.VatTypeId,
                        text = v.TypeName
                    })
                    .Take(50)
                    .ToListAsync();

                if (!vatTypes.Any())
                {
                    return NotFound(new { message = "No VAT types found." });
                }

                return Ok(vatTypes);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { title = "Internal server error", errors = new { General = new[] { $"Failed to retrieve VAT types: {ex.Message}" } } });
            }
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

