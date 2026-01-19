namespace HelperApp.Models.User;

public class CurrentUser
{
    public int Id { get; set; }
    public int EmployeeId { get; set; }
    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;   
    public string FullName => $"{LastName} {FirstName}";  
    public string Role { get; set; } = null!;
    public string AccessToken { get; set; } = null!;
    public DateTime TokenExpiresAt { get; set; }
}
