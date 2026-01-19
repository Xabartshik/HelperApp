namespace HelperApp.Models
{
    /// <summary>
    /// Detailed inventory task information received from server
    /// </summary>
    public class InventoryTaskDetailsDto
    {
        public int TaskId { get; set; }
        public string ZoneCode { get; set; } = null!;
        public List<InventoryItemDto> Items { get; set; } = new();
        public int TotalExpectedCount { get; set; }
        public DateTime InitiatedAt { get; set; }
    }

    /// <summary>
    /// Individual item within inventory task
    /// </summary>
    public class InventoryItemDto
    {
        public int ItemId { get; set; }
        public string ItemName { get; set; } = null!;
        public string PositionCode { get; set; } = null!;
        public int PositionId { get; set; }
        public int ExpectedQuantity { get; set; }
        public double Weight { get; set; }
        public double Length { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }
        public string Status { get; set; } = "Available";
    }

    /// <summary>
    /// Response from server when checking for new tasks
    /// </summary>
    public class TaskCheckResponse
    {
        public bool HasNewTasks { get; set; }
        public int NewTaskCount { get; set; }
        public DateTime? LatestTaskTime { get; set; }
        public DateTime LastChecked { get; set; }
    }

    /// <summary>
    /// Summary of pending inventory tasks for user
    /// </summary>
    public class InventoryTaskSummary
    {
        public int TaskId { get; set; }
        public string ZoneCode { get; set; } = null!;
        public int TotalItems { get; set; }
        public DateTime InitiatedAt { get; set; }
        public bool HasBeenViewed { get; set; }
    }
}
