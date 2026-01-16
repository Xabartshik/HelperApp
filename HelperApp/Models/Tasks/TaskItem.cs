namespace HelperApp.Models.Tasks;

public class TaskItem
{
    public int Id { get; set; }
    public string Title { get; set; } = null!;
    public string Description { get; set; } = null!;
    public string Status { get; set; } = "New";
    public DateTime CreatedAt { get; set; }
    public int AssignedToEmployeeId { get; set; }
}
