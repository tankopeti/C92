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
    public class ResourceStatusesModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public ResourceStatusesModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public IList<ResourceStatus> ResourceStatuses { get; set; }

        [BindProperty]
        public ResourceStatus ResourceStatus { get; set; }

        public async Task OnGetAsync()
        {
            ResourceStatuses = await _context.ResourceStatuses
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

            ResourceStatus.CreatedDate = DateTime.UtcNow;
            ResourceStatus.IsActive = true; // Default for new records
            _context.ResourceStatuses.Add(ResourceStatus);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Resource Status created successfully.";
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

            var existingResourceStatus = await _context.ResourceStatuses.FindAsync(ResourceStatus.ResourceStatusId);
            if (existingResourceStatus == null)
            {
                TempData["ErrorMessage"] = "Resource Status not found.";
                return RedirectToPage();
            }

            existingResourceStatus.Name = ResourceStatus.Name;
            existingResourceStatus.Description = ResourceStatus.Description;
            existingResourceStatus.CreatedDate = existingResourceStatus.CreatedDate; // Preserve original
            existingResourceStatus.IsActive = ResourceStatus.IsActive ?? true;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Resource Status updated successfully.";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            var resourceStatus = await _context.ResourceStatuses.FindAsync(id);
            if (resourceStatus == null)
            {
                TempData["ErrorMessage"] = "Resource Status not found.";
                return RedirectToPage();
            }

            resourceStatus.IsActive = false; // Soft delete
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Resource Status deleted successfully.";
            return RedirectToPage();
        }
    }
}