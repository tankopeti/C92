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
    public class VatController : Controller
    {
        private readonly ApplicationDbContext _context;

        public VatController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Vat/Index
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var vatTypes = await _context.VatTypes.ToListAsync();
            return View(vatTypes);
        }

        // GET: Vat/Create
        [HttpGet]
        public IActionResult Create()
        {
            var model = new VatType(); // Defaults to TypeName = "27%", Rate = 27.00m
            return View(model);
        }

        // POST: Vat/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(VatType vatType)
        {
            // Normalize input
            if (!string.IsNullOrEmpty(vatType.TypeName))
            {
                vatType.TypeName = vatType.TypeName.Trim();
            }

            if (ModelState.IsValid)
            {
                _context.VatTypes.Add(vatType);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            return View(vatType);
        }
    }
}

