// File: Pages/GroupRemote/Dashboard.cshtml.cs

using Cloud9_2.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Globalization;

namespace Cloud9_2.Pages.GroupRemote
{
    public class DashboardModel : PageModel
    {
        private readonly TaskPMService _taskService;

        public DashboardModel(TaskPMService taskService)
        {
            _taskService = taskService;
        }

        // Stats
        public int TotalTasksToday { get; set; }
        public int RunningTasks { get; set; }
        public int PendingTasks { get; set; }
        public int FailedTasksToday { get; set; }
        public string LastUpdate { get; set; } = string.Empty;

        // Chart data
        public List<string> ChartLabels { get; } = new();
        public List<int> ChartData { get; set; } = new();

        public async Task OnGetAsync()
        {
            var allTasks = await _taskService.GetAllTasksAsync();

            var today = DateTime.Today;
            var thirtyDaysAgo = today.AddDays(-29);

            // === Chart: tasks created per day (last 30 days) ===
            var tasksByDay = allTasks
                .Where(t => t.CreatedDate >= thirtyDaysAgo)
                .GroupBy(t => t.CreatedDate.Value.Date)
                .Select(g => new { Date = g.Key, Count = g.Count() })
                .ToDictionary(x => x.Date, x => x.Count);

            ChartLabels.Clear();
            ChartData.Clear();

            for (var date = thirtyDaysAgo; date <= today; date = date.AddDays(1))
            {
                var label = date.ToString("MMM d", new CultureInfo("hu-HU")); // nov 15, dec 1, stb.
                ChartLabels.Add(label);

                var count = tasksByDay.TryGetValue(date.Date, out var c) ? c : 0;
                ChartData.Add(count);
            }

            // === Stats counters ===
            TotalTasksToday = allTasks.Count(t => t.CreatedDate.Value.Date == today);

            // FONTOS: Ellenőrizd a saját adatbázisodban a TaskStatusPMId értékeket!
            const int StatusPending = 1;   // Függőben
            const int StatusRunning = 2;   // Futó
            const int StatusFailed  = 4;   // Sikertelen / Hiba

            PendingTasks      = allTasks.Count(t => t.TaskStatusPMId == StatusPending);
            RunningTasks      = allTasks.Count(t => t.TaskStatusPMId == StatusRunning);
            FailedTasksToday  = allTasks.Count(t => t.TaskStatusPMId == StatusFailed && t.CreatedDate.Value.Date == today);

            LastUpdate = DateTime.Now.ToString("yyyy. MM. dd. HH:mm", new CultureInfo("hu-HU"));
        }
    }
}