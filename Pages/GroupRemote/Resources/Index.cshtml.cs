using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Cloud9_2.Data;
using Cloud9_2.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Cloud9_2.Pages.GroupRemote
{
    public class ResourcesModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public ResourcesModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public IList<Resource> Resources { get; set; }
        [BindProperty]
        public Resource Resource { get; set; }
        public SelectList ResourceTypeOptions { get; set; }
        public SelectList ResourceStatusOptions { get; set; }
        public SelectList UserOptions { get; set; }
        public SelectList PartnerOptions { get; set; }
        public SelectList SiteOptions { get; set; }
        public SelectList ContactOptions { get; set; }
        public SelectList EmployeeOptions { get; set; }

        public async Task OnGetAsync()
        {
            try
            {
                Resources = await _context.Resources
                    .Include(r => r.ResourceType)
                    .Include(r => r.ResourceStatus)
                    .Include(r => r.WhoBuy)
                    .Include(r => r.WhoLastServiced)
                    .Include(r => r.Partner)
                    .Include(r => r.Site)
                    .Include(r => r.Contact)
                    .Include(r => r.Employee)
                    .Where(r => r.IsActive == true)
                    .OrderBy(r => r.Name)
                    .ToListAsync();

                await LoadDropdowns();
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error loading resources: {ex.Message}";
                Resources = new List<Resource>();
            }
        }

        public async Task<IActionResult> OnPostCreateAsync()
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                TempData["ErrorMessage"] = "Invalid input data: " + string.Join("; ", errors);
                await LoadDropdowns();
                return Page();
            }

            Resource.CreatedDate = DateTime.UtcNow;
            Resource.IsActive = true;
            _context.Resources.Add(Resource);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Resource created successfully.";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostUpdateAsync()
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                TempData["ErrorMessage"] = "Invalid input data: " + string.Join("; ", errors);
                await LoadDropdowns();
                return Page();
            }

            var existingResource = await _context.Resources.FindAsync(Resource.ResourceId);
            if (existingResource == null)
            {
                TempData["ErrorMessage"] = "Resource not found.";
                return RedirectToPage();
            }

            existingResource.Name = Resource.Name;
            existingResource.ResourceTypeId = Resource.ResourceTypeId;
            existingResource.ResourceStatusId = Resource.ResourceStatusId;
            existingResource.Serial = Resource.Serial;
            existingResource.NextService = Resource.NextService;
            existingResource.DateOfPurchase = Resource.DateOfPurchase;
            existingResource.WarrantyPeriod = Resource.WarrantyPeriod;
            existingResource.WarrantyExpireDate = Resource.WarrantyExpireDate;
            existingResource.ServiceDate = Resource.ServiceDate;
            existingResource.WhoBuyId = Resource.WhoBuyId;
            existingResource.WhoLastServicedId = Resource.WhoLastServicedId;
            existingResource.PartnerId = Resource.PartnerId;
            existingResource.SiteId = Resource.SiteId;
            existingResource.ContactId = Resource.ContactId;
            existingResource.EmployeeId = Resource.EmployeeId;
            existingResource.Price = Resource.Price;
            existingResource.CreatedDate = existingResource.CreatedDate;
            existingResource.IsActive = Resource.IsActive ?? true;
            existingResource.Comment1 = Resource.Comment1;
            existingResource.Comment2 = Resource.Comment2;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Resource updated successfully.";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            var resource = await _context.Resources.FindAsync(id);
            if (resource == null)
            {
                TempData["ErrorMessage"] = "Resource not found.";
                return RedirectToPage();
            }

            resource.IsActive = false;
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Resource deleted successfully.";
            return RedirectToPage();
        }

        private async Task LoadDropdowns()
        {
            ResourceTypeOptions = new SelectList(
                await _context.ResourceTypes.Where(t => t.IsActive == true && !string.IsNullOrEmpty(t.Name)).ToListAsync(),
                "ResourceTypeId",
                "Name"
            );
            ResourceStatusOptions = new SelectList(
                await _context.ResourceStatuses.Where(s => s.IsActive == true && !string.IsNullOrEmpty(s.Name)).ToListAsync(),
                "ResourceStatusId",
                "Name"
            );
            UserOptions = new SelectList(
                await _context.Users.Where(u => !string.IsNullOrEmpty(u.UserName)).ToListAsync(),
                "Id",
                "UserName"
            );
            PartnerOptions = new SelectList(
                await _context.Partners.Where(p => !string.IsNullOrEmpty(p.Name)).ToListAsync(),
                "PartnerId",
                "Name"
            );
            SiteOptions = new SelectList(
                await _context.Sites.Where(s => s.SiteId > 0 && !string.IsNullOrEmpty(s.SiteName)).ToListAsync(),
                "SiteId",
                "SiteName"
            );
            ContactOptions = new SelectList(
                await _context.Contacts.Where(c => !string.IsNullOrEmpty(c.FirstName)).ToListAsync(),
                "ContactId",
                "FirstName"
            );
            EmployeeOptions = new SelectList(
                await _context.Employees.Where(e => !string.IsNullOrEmpty(e.FirstName)).ToListAsync(),
                "EmployeeId",
                "FirstName"
            );
        }
    }
}