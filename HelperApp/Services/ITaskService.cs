using HelperApp.Models.Tasks;

namespace HelperApp.Services;

public interface ITaskService
{
    Task<IEnumerable<TaskItem>> GetTasksForCurrentUserAsync(int employeeId);
    void StartPeriodicSync(Func<IEnumerable<TaskItem>, Task> onTasksUpdated, int intervalSeconds = 30);
    void StopPeriodicSync();
}
