using HelperApp.Models.User;

namespace HelperApp.Services;

public interface IAuthService
{
    Task<CurrentUser?> LoginAsync(string employeeId, string password);
    Task<CurrentUser?> TryAutoLoginAsync();
    Task LogoutAsync();

    CurrentUser? GetCurrentUser();
}
