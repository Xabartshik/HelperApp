namespace HelperApp.Models.User;

public class CurrentUser
{
    public int EmployeeId { get; set; }
    public string Role { get; set; } = null!;
    public string AccessToken { get; set; } = null!;
    public DateTime TokenExpiresAt { get; set; }
}
