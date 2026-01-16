using HelperApp.Models.User;

namespace HelperApp.Models.Auth;

public class LoginResponse
{
    public string AccessToken { get; set; } = null!;
    public string TokenType { get; set; } = "Bearer";
    public MobileAppUserDto User { get; set; } = null!;
}
