using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Cloud9_2.Models;
using Cloud9_2.Data;

namespace Cloud9_2.Services
{

    public interface ITaskService
    {
        Task<IEnumerable<TaskPMDto>> GetAllTasksAsync(bool includeInactive = false);
        Task<TaskPMDto> GetTaskByIdAsync(int id);
        Task<TaskPMDto> CreateTaskAsync(TaskPMCreateDto createDto, string userId);
        Task<TaskPMDto> UpdateTaskAsync(int id, TaskPMUpdateDto updateDto);
        Task<bool> DeleteTaskAsync(int id);
        Task<IEnumerable<TaskStatusPMDto>> GetTaskStatusesAsync();
        Task<IEnumerable<TaskPriorityPMDto>> GetTaskPrioritiesAsync();
        Task<IEnumerable<TaskTypePMDto>> GetTaskTypesAsync();
    }

    public class TaskService : ITaskService
    {
        private readonly ApplicationDbContext _context;
        private readonly IUserService _userService;
        private readonly ILogger<TaskService> _logger;

        public TaskService(ApplicationDbContext context, IUserService userService, ILogger<TaskService> logger)
        {
            _context = context;
            _userService = userService;
            _logger = logger;
        }

        public async Task<IEnumerable<TaskPMDto>> GetAllTasksAsync(bool includeInactive = false)
        {
            var query = _context.TaskPMs
                .Include(t => t.TaskTypePM)
                .Include(t => t.ProjectPM)
                .Include(t => t.TaskStatusPM)
                .Include(t => t.TaskPriorityPM)
                .AsQueryable();

            if (!includeInactive)
            {
                query = query.Where(t => t.IsActive);
            }

            var tasks = await query.OrderByDescending(t => t.CreatedDate).ToListAsync();
            return tasks.Select(t => MapToDto(t));
        }

        public async Task<TaskPMDto> GetTaskByIdAsync(int id)
        {
            var task = await _context.TaskPMs
                .Include(t => t.TaskTypePM)
                .Include(t => t.ProjectPM)
                .Include(t => t.TaskStatusPM)
                .Include(t => t.TaskPriorityPM)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (task == null) return null;

            return MapToDto(task);
        }

        public async Task<TaskPMDto> CreateTaskAsync(TaskPMCreateDto createDto, string userId)
        {
            var task = new TaskPM
            {
                Title = createDto.Title,
                Description = createDto.Description,
                TaskTypePMId = createDto.TaskTypePMId,
                ProjectPMId = createDto.ProjectPMId,
                TaskStatusPMId = createDto.TaskStatusPMId,
                TaskPriorityPMId = createDto.TaskPriorityPMId,
                DueDate = createDto.DueDate,
                EstimatedHours = createDto.EstimatedHours,
                CreatedById = userId,
                CreatedDate = DateTime.UtcNow,
                AssignedToId = createDto.AssignedToId,
                IsActive = true
            };

            _context.TaskPMs.Add(task);
            await _context.SaveChangesAsync();

            return await GetTaskByIdAsync(task.Id);
        }

        public async Task<TaskPMDto> UpdateTaskAsync(int id, TaskPMUpdateDto updateDto)
        {
            var task = await _context.TaskPMs
                .Include(t => t.TaskTypePM)
                .Include(t => t.ProjectPM)
                .Include(t => t.TaskStatusPM)
                .Include(t => t.TaskPriorityPM)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (task == null)
            {
                return null;
            }

            // Update properties
            task.Title = updateDto.Title;
            task.Description = updateDto.Description;
            task.TaskTypePMId = updateDto.TaskTypePMId;
            task.ProjectPMId = updateDto.ProjectPMId;
            task.TaskStatusPMId = updateDto.TaskStatusPMId;
            task.TaskPriorityPMId = updateDto.TaskPriorityPMId;
            task.DueDate = updateDto.DueDate;
            task.EstimatedHours = updateDto.EstimatedHours;
            task.ActualHours = updateDto.ActualHours;
            task.AssignedToId = updateDto.AssignedToId;
            task.IsActive = updateDto.IsActive;

            // Set completed date if status changed to "Completed"
            if (updateDto.TaskStatusPMId == 3 && task.CompletedDate == null) // Assuming 3 is Completed
            {
                task.CompletedDate = DateTime.UtcNow;
            }

            try
            {
                await _context.SaveChangesAsync();
                return MapToDto(task); // Use your existing mapping method
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Error updating task ID: {TaskId}", id);
                throw; // Or return null if you prefer
            }
        }



        public async Task<bool> DeleteTaskAsync(int id)
        {
            var task = await _context.TaskPMs.FindAsync(id);
            if (task == null)
            {
                return false;
            }

            // Soft delete (recommended approach)
            task.IsActive = false;
            
            try
            {
                await _context.SaveChangesAsync();
                return true;
            }
            catch (DbUpdateException)
            {
                // Log the error here if needed
                return false;
            }
        }

        public async Task<IEnumerable<TaskStatusPMDto>> GetTaskStatusesAsync()
        {
            return await _context.TaskStatusPMs
                .Where(s => s.IsActive)
                .OrderBy(s => s.DisplayOrder)
                .Select(s => new TaskStatusPMDto
                {
                    Id = s.Id,
                    Name = s.Name,
                    Description = s.Description,
                    ColorCode = s.ColorCode
                })
                .ToListAsync();
        }

        public async Task<IEnumerable<TaskPriorityPMDto>> GetTaskPrioritiesAsync()
        {
            return await _context.TaskPriorityPMs
                .Where(p => p.IsActive)
                .OrderBy(p => p.DisplayOrder)
                .Select(p => new TaskPriorityPMDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    Description = p.Description,
                    ColorCode = p.ColorCode,
                    Icon = p.Icon
                })
                .ToListAsync();
        }

        public async Task<IEnumerable<TaskTypePMDto>> GetTaskTypesAsync()
        {
            return await _context.TaskTypePMs
                .Where(t => t.IsActive)
                .OrderBy(t => t.Name)
                .Select(t => new TaskTypePMDto
                {
                    Id = t.Id,
                    Name = t.Name,
                    Description = t.Description,
                    Icon = t.Icon
                })
                .ToListAsync();
        }

        private TaskPMDto MapToDto(TaskPM task)
        {
            return new TaskPMDto
            {
                Id = task.Id,
                Title = task.Title,
                Description = task.Description,
                TaskType = task.TaskTypePM != null ? new TaskTypePMDto
                {
                    Id = task.TaskTypePM.Id,
                    Name = task.TaskTypePM.Name,
                    Description = task.TaskTypePM.Description,
                    Icon = task.TaskTypePM.Icon
                } : null,
                Project = task.ProjectPM != null ? new ProjectPMDto
                {
                    Id = task.ProjectPM.Id,
                    Name = task.ProjectPM.Name,
                    Status = task.ProjectPM.ProjectStatusPM != null ? new ProjectStatusPMDto
                    {
                        Id = task.ProjectPM.ProjectStatusPM.Id,
                        Name = task.ProjectPM.ProjectStatusPM.Name
                        // Other status properties
                    } : null
                } : null,
                Status = task.TaskStatusPM != null ? new TaskStatusPMDto
                {
                    Id = task.TaskStatusPM.Id,
                    Name = task.TaskStatusPM.Name,
                    Description = task.TaskStatusPM.Description,
                    ColorCode = task.TaskStatusPM.ColorCode
                } : null,
                Priority = task.TaskPriorityPM != null ? new TaskPriorityPMDto
                {
                    Id = task.TaskPriorityPM.Id,
                    Name = task.TaskPriorityPM.Name,
                    Description = task.TaskPriorityPM.Description,
                    ColorCode = task.TaskPriorityPM.ColorCode,
                    Icon = task.TaskPriorityPM.Icon
                } : null,
                DueDate = task.DueDate,
                EstimatedHours = task.EstimatedHours,
                ActualHours = task.ActualHours,
                CreatedBy = task.CreatedById,
                CreatedDate = task.CreatedDate,
                AssignedTo = task.AssignedToId,
                CompletedDate = task.CompletedDate,
                IsActive = task.IsActive
            };
        }
    }

}