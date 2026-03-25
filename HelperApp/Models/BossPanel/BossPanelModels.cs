using System;
using System.Collections.Generic;

namespace HelperApp.Models.BossPanel
{
    public class BossPanelTaskCardDto
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string TaskType { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ExpectedCompletionDate { get; set; }
        public int OverallProgressPercentage { get; set; }
        public double ProgressValue => OverallProgressPercentage / 100.0;
        public List<TaskAssigneeProgressDto> Assignees { get; set; } = new();
    }

    public class TaskAssigneeProgressDto
    {
        public int EmployeeId { get; set; }
        public string FullName { get; set; }
        public int AssignedVolume { get; set; }
        public int CompletedVolume { get; set; }
        public string Status { get; set; }
    }

    public class EmployeeWorkloadDto
    {
        public int EmployeeId { get; set; }
        public string FullName { get; set; }
        public bool IsAtWork { get; set; }
        public int ActiveTasksCount { get; set; }
        public bool HasActiveTasks => ActiveTasksCount > 0;
        public List<ActiveTaskBriefDto> ActiveTasks { get; set; } = new();
    }

    public class ActiveTaskBriefDto
    {
        public int TaskId { get; set; }
        public string Title { get; set; }
        public string TaskType { get; set; }
        public string Status { get; set; }
    }

    public class AvailableEmployeeDto
    {
        public int EmployeeId { get; set; }
        public string FullName { get; set; }
        public bool IsAtWork { get; set; }
        public int ActiveTasksCount { get; set; }
        public bool IsRecommended { get; set; }
    }

    public class CreateInventoryTaskDto
    {
        public int Priority { get; set; }
        public List<int> ItemPositionIds { get; set; } = new();
        public int WorkerCount { get; set; }
        public string Description { get; set; }
        public string DivisionStrategy { get; set; } = "ByQuantity";
        public DateTime? DeadlineDate { get; set; }
    }

    public class CreateInventoryByZoneDto
    {
        public List<string> ZonePrefixes { get; set; } = new();
        public int Priority { get; set; } = 3;
        public int WorkerCount { get; set; } = 1;
        public List<int>? WorkerIds { get; set; }
        public string? Description { get; set; }
        public DateTime? DeadlineDate { get; set; }
    }

    public class CompleteInventoryDto
    {
        public int TaskId { get; set; }
        public int AssignmentsCount { get; set; }
        public string Message { get; set; }
}

    public class PositionCellDto
    {
        public int PositionId { get; set; }
        public int BranchId { get; set; }
        public string Status { get; set; } = "Active";
        public string ZoneCode { get; set; }
        public string FirstLevelStorageType { get; set; }
        public string FLSNumber { get; set; }
        public string? SecondLevelStorage { get; set; }
        public string? ThirdLevelStorage { get; set; }
    }
}
