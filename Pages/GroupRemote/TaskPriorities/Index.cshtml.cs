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
    public class TaskPrioritiesModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public TaskPrioritiesModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public IList<TaskPriorityPM> TaskPriorities { get; set; }

        [BindProperty]
        public TaskPriorityPM TaskPriority { get; set; }

        public async Task OnGetAsync()
        {
            TaskPriorities = await _context.TaskPrioritiesPM
                // .Where(t => t.IsActive == true)
                .OrderBy(t => t.DisplayOrder ?? int.MaxValue)
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

            TaskPriority.IsActive = TaskPriority.IsActive ?? true;
            TaskPriority.DisplayOrder = TaskPriority.DisplayOrder ?? 0;
            _context.TaskPrioritiesPM.Add(TaskPriority);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Task Priority created successfully.";
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

            var existingTaskPriority = await _context.TaskPrioritiesPM.FindAsync(TaskPriority.TaskPriorityPMId);
            if (existingTaskPriority == null)
            {
                TempData["ErrorMessage"] = "Task Priority not found.";
                return RedirectToPage();
            }

            existingTaskPriority.Name = TaskPriority.Name;
            existingTaskPriority.Description = TaskPriority.Description;
            existingTaskPriority.DisplayOrder = TaskPriority.DisplayOrder ?? 0;
            existingTaskPriority.IsActive = TaskPriority.IsActive ?? true;
            existingTaskPriority.PriorityColorCode = TaskPriority.PriorityColorCode;
            existingTaskPriority.Icon = TaskPriority.Icon;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Task Priority updated successfully.";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            var taskPriority = await _context.TaskPrioritiesPM.FindAsync(id);
            if (taskPriority == null)
            {
                TempData["ErrorMessage"] = "Task Priority not found.";
                return RedirectToPage();
            }

            taskPriority.IsActive = false;
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Task Priority deleted successfully.";
            return RedirectToPage();
        }
    }
}