using Cloud9_2.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Cloud9_2.Services
{
    public class TaskActivationService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<TaskActivationService> _logger;

        public TaskActivationService(ApplicationDbContext context, ILogger<TaskActivationService> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Soft deactivate: IsActive = false for a single TaskPM by Id.
        /// </summary>
        public async Task SetInactiveAsync(int id, CancellationToken ct = default)
        {
            // Biztosítsuk: csak 1 rekordot módosítunk, és csak a megadott ID-t.
            var task = await _context.TaskPMs
                .FirstOrDefaultAsync(t => t.Id == id, ct);

            if (task == null)
                throw new KeyNotFoundException($"Task {id} not found.");

            if (task.IsActive == false)
            {
                _logger.LogInformation("Task {Id} already inactive.", id);
                return;
            }

            task.IsActive = false;
            task.UpdatedDate = DateTime.UtcNow;

            await _context.SaveChangesAsync(ct);

            _logger.LogInformation("Task {Id} set inactive successfully.", id);
        }
    }
}
