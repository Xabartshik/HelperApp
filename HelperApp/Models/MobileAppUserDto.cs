namespace HelperApp.Models;


public class MobileAppUserDto
{
    public int Id { get; set; }
    public int EmployeeId { get; set; }
    public string Role { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}