using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Cloud9_2.Models;
using Cloud9_2.Services;
using Cloud9_2.Data;
using System.Text.Json;
using Cloud9_2.Data;
using Cloud9_2.Models;
using Cloud9_2.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

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

