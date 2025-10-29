using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Cloud9_2.Data;
using Cloud9_2.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Cloud9_2.Pages.Tasks
{
    public class TaskTypesModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public TaskTypesModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public IList<TaskTypePM> TaskTypes { get; set; }

        [BindProperty]
        public TaskTypePM TaskType { get; set; }

        // GET: Load all TaskTypes
        public async Task OnGetAsync()
        {
            TaskTypes = await _context.TaskTypePMs
                .Where(t => t.IsActive)
                .ToListAsync();
        }

        // POST: Create a new TaskType
        public async Task<IActionResult> OnPostCreateAsync()
        {
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Invalid input data.";
                return RedirectToPage();
            }

            TaskType.CreatedAt = DateTime.UtcNow;
            TaskType.IsActive = true;
            _context.TaskTypePMs.Add(TaskType);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Task Type created successfully.";
            return RedirectToPage();
        }

        // POST: Update an existing TaskType
        public async Task<IActionResult> OnPostUpdateAsync()
        {
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Invalid input data.";
                return RedirectToPage();
            }

            var existingTaskType = await _context.TaskTypePMs.FindAsync(TaskType.TaskTypePMId);
            if (existingTaskType == null)
            {
                TempData["ErrorMessage"] = "Task Type not found.";
                return RedirectToPage();
            }

            existingTaskType.TaskTypePMName = TaskType.TaskTypePMName;
            existingTaskType.Description = TaskType.Description;
            existingTaskType.Icon = TaskType.Icon;
            existingTaskType.IsActive = TaskType.IsActive;
            existingTaskType.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Task Type updated successfully.";
            return RedirectToPage();
        }

        // POST: Delete a TaskType
        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            var taskType = await _context.TaskTypePMs.FindAsync(id);
            if (taskType == null)
            {
                TempData["ErrorMessage"] = "Task Type not found.";
                return RedirectToPage();
            }

            taskType.IsActive = false; // Soft delete
            taskType.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Task Type deleted successfully.";
            return RedirectToPage();
        }
    }
}