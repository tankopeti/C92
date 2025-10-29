using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Cloud9_2.Data;
using Cloud9_2.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Cloud9_2.Pages.GroupRemote
{
    public class ResourceTypesModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public ResourceTypesModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public IList<ResourceType> ResourceTypes { get; set; }

        [BindProperty]
        public ResourceType ResourceType { get; set; }

        public async Task OnGetAsync()
        {
            ResourceTypes = await _context.ResourceTypes
                // .Where(r => r.IsActive == true)
                .OrderBy(r => r.Name)
                .ToListAsync();
        }

        public async Task<IActionResult> OnPostCreateAsync()
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                TempData["ErrorMessage"] = "Invalid input data: " + string.Join("; ", errors);
                return RedirectToPage();
            }

            ResourceType.CreatedDate = DateTime.UtcNow;
            ResourceType.IsActive = true; // Default for new records
            _context.ResourceTypes.Add(ResourceType);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Resource Type created successfully.";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostUpdateAsync()
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                TempData["ErrorMessage"] = "Invalid input data: " + string.Join("; ", errors);
                return RedirectToPage();
            }

            var existingResourceType = await _context.ResourceTypes.FindAsync(ResourceType.ResourceTypeId);
            if (existingResourceType == null)
            {
                TempData["ErrorMessage"] = "Resource Type not found.";
                return RedirectToPage();
            }

            existingResourceType.Name = ResourceType.Name;
            existingResourceType.Description = ResourceType.Description;
            existingResourceType.CreatedDate = existingResourceType.CreatedDate; // Preserve original
            existingResourceType.IsActive = ResourceType.IsActive ?? true;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Resource Type updated successfully.";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            var resourceType = await _context.ResourceTypes.FindAsync(id);
            if (resourceType == null)
            {
                TempData["ErrorMessage"] = "Resource Type not found.";
                return RedirectToPage();
            }

            resourceType.IsActive = false; // Soft delete
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Resource Type deleted successfully.";
            return RedirectToPage();
        }
    }
}