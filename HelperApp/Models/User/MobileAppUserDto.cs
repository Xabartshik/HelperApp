namespace HelperApp.Models.User;

public class MobileAppUserDto
{
    public int Id { get; set; }
    public int EmployeeId { get; set; }
    public string Role { get; set; } = null!;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
