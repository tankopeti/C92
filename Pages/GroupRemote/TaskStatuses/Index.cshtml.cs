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
    public class TaskStatusesModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public TaskStatusesModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public IList<TaskStatusPM> TaskStatuses { get; set; }

        [BindProperty]
        public TaskStatusPM TaskStatus { get; set; }

        // GET: Load all TaskStatuses
        public async Task OnGetAsync()
        {
            TaskStatuses = await _context.TaskStatusesPM
                // .Where(t => t.IsActive == true)
                .OrderBy(t => t.DisplayOrder ?? int.MaxValue)
                .ToListAsync();
        }

        // POST: Create a new TaskStatus
        public async Task<IActionResult> OnPostCreateAsync()
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                TempData["ErrorMessage"] = "Invalid input data: " + string.Join("; ", errors);
                return RedirectToPage();
            }

            TaskStatus.IsActive = TaskStatus.IsActive ?? true; // Default to true if null
            TaskStatus.DisplayOrder = TaskStatus.DisplayOrder ?? 0; // Default to 0 if null
            _context.TaskStatusesPM.Add(TaskStatus);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Task Status created successfully.";
            return RedirectToPage();
        }

        // POST: Update an existing TaskStatus
        public async Task<IActionResult> OnPostUpdateAsync()
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                TempData["ErrorMessage"] = "Invalid input data: " + string.Join("; ", errors);
                return RedirectToPage();
            }

            var existingTaskStatus = await _context.TaskStatusesPM.FindAsync(TaskStatus.TaskStatusPMId);
            if (existingTaskStatus == null)
            {
                TempData["ErrorMessage"] = "Task Status not found.";
                return RedirectToPage();
            }

            existingTaskStatus.Name = TaskStatus.Name;
            existingTaskStatus.Description = TaskStatus.Description;
            existingTaskStatus.DisplayOrder = TaskStatus.DisplayOrder ?? 0;
            existingTaskStatus.IsActive = TaskStatus.IsActive ?? true;
            existingTaskStatus.ColorCode = TaskStatus.ColorCode;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Task Status updated successfully.";
            return RedirectToPage();
        }

        // POST: Delete a TaskStatus
        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            var taskStatus = await _context.TaskStatusesPM.FindAsync(id);
            if (taskStatus == null)
            {
                TempData["ErrorMessage"] = "Task Status not found.";
                return RedirectToPage();
            }

            taskStatus.IsActive = false; // Soft delete
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Task Status deleted successfully.";
            return RedirectToPage();
        }
    }
}