using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Cloud9_2.Data;
using Cloud9_2.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Cloud9_2.Pages.HR.WorkGroup
{
    public class JobTitlesModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public JobTitlesModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public IList<JobTitle> JobTitles { get; set; } = new List<JobTitle>();
        [BindProperty]
        public JobTitleCreateDto JobTitleCreate { get; set; } = new JobTitleCreateDto();
        [BindProperty]
        public JobTitleUpdateDto JobTitleUpdate { get; set; } = new JobTitleUpdateDto();

        public async Task OnGetAsync()
        {
            try
            {
                JobTitles = await _context.JobTitles
                    .OrderBy(jt => jt.TitleName)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error loading job titles: {ex.Message}";
                if (ex.InnerException != null)
                {
                    TempData["ErrorMessage"] += $" Inner: {ex.InnerException.Message}";
                }
                JobTitles = new List<JobTitle>();
            }
        }

        public async Task<IActionResult> OnPostCreateAsync()
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                TempData["ErrorMessage"] = "Invalid input: " + string.Join("; ", errors);
                await OnGetAsync();
                return Page();
            }

            try
            {
                var jobTitle = new JobTitle
                {
                    TitleName = JobTitleCreate.TitleName,
                    CreatedAt = DateTime.UtcNow
                };

                _context.JobTitles.Add(jobTitle);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Job title created successfully.";
                return RedirectToPage();
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error creating job title: {ex.Message}";
                if (ex.InnerException != null)
                {
                    TempData["ErrorMessage"] += $" Inner: {ex.InnerException.Message}";
                }
                await OnGetAsync();
                return Page();
            }
        }

        public async Task<IActionResult> OnPostUpdateAsync(int id)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                TempData["ErrorMessage"] = "Invalid input: " + string.Join("; ", errors);
                await OnGetAsync();
                return Page();
            }

            try
            {
                var jobTitle = await _context.JobTitles.FindAsync(id);
                if (jobTitle == null)
                {
                    TempData["ErrorMessage"] = "Job title not found.";
                    return RedirectToPage();
                }

                jobTitle.TitleName = JobTitleUpdate.TitleName;
                jobTitle.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Job title updated successfully.";
                return RedirectToPage();
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error updating job title: {ex.Message}";
                if (ex.InnerException != null)
                {
                    TempData["ErrorMessage"] += $" Inner: {ex.InnerException.Message}";
                }
                await OnGetAsync();
                return Page();
            }
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            try
            {
                var jobTitle = await _context.JobTitles
                    .FirstOrDefaultAsync(jt => jt.JobTitleId == id);

                if (jobTitle == null)
                {
                    TempData["ErrorMessage"] = "Job title not found.";
                    return RedirectToPage();
                }

                _context.JobTitles.Remove(jobTitle);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Job title deleted successfully.";
                return RedirectToPage();
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error deleting job title: {ex.Message}";
                if (ex.InnerException != null)
                {
                    TempData["ErrorMessage"] += $" Inner: {ex.InnerException.Message}";
                }
                await OnGetAsync();
                return Page();
            }
        }
    }
}