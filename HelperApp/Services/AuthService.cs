using HelperApp.Models.Auth;
using HelperApp.Models.User;
using Microsoft.Extensions.Logging;
using System.IdentityModel.Tokens.Jwt;
using static HelperApp.Services.ApiClient;

namespace HelperApp.Services;

public sealed class AuthService : IAuthService
{
    private readonly IApiClient _apiClient;
    private readonly ILogger<AuthService> _logger;

    private CurrentUser? _currentUser;

    private const string TokenKey = "access_token";

    private const string EmployeeIdKey = "last_employee_id";
    private const string RoleKey = "last_role";
    private const string FirstNameKey = "first_name";
    private const string LastNameKey = "last_name";
    private const string UserIdKey = "user_id";

    public AuthService(IApiClient apiClient, ILogger<AuthService> logger)
    {
        _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public CurrentUser? GetCurrentUser() => _currentUser;

    public async Task<CurrentUser?> LoginAsync(string employeeId, string password)
    {
        try
        {
            var loginRequest = new LoginRequest
            {
                Username = employeeId,
                Password = password
            };

            var response = await _apiClient.PostAsync<LoginResponse>("mobileappuser/login", loginRequest);
            if (response?.AccessToken == null)
            {
                _logger.LogWarning("Пустой токен от сервера");
                return null;
            }

            var jwtToken = new JwtSecurityTokenHandler().ReadJwtToken(response.AccessToken);

            _currentUser = new CurrentUser
            {
                Id = response.User.Id,
                EmployeeId = response.User.EmployeeId,
                FirstName = response.User.FirstName,
                LastName = response.User.LastName,
                Role = response.User.Role,
                AccessToken = response.AccessToken,
                TokenExpiresAt = jwtToken.ValidTo
            };

            await SecureStorage.SetAsync(TokenKey, response.AccessToken);

            Preferences.Set(UserIdKey, response.User.Id);
            Preferences.Set(EmployeeIdKey, response.User.EmployeeId);
            Preferences.Set(RoleKey, response.User.Role);
            Preferences.Set(FirstNameKey, response.User.FirstName ?? string.Empty);
            Preferences.Set(LastNameKey, response.User.LastName ?? string.Empty);

            _apiClient.SetAuthorizationToken(response.AccessToken);

            _logger.LogInformation(
                "Успешный логин: EmployeeId={EmployeeId}, Role={Role}",
                response.User.EmployeeId,
                response.User.Role);

            return _currentUser;
        }
        catch (UnauthorizedException)
        {
            _logger.LogWarning("Неверные учетные данные для пользователя: {EmployeeId}", employeeId);
            throw;
        }
        catch (NoNetworkException)
        {
            _logger.LogError("Нет подключения к сети при логине");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при логине пользователя {EmployeeId}", employeeId);
            throw;
        }
    }

    public async Task<CurrentUser?> TryAutoLoginAsync()
    {
        try
        {
            var token = await SecureStorage.GetAsync(TokenKey);
            if (string.IsNullOrEmpty(token))
            {
                _logger.LogInformation("Токен не найден в SecureStorage");
                return null;
            }

            JwtSecurityToken jwtToken;
            try
            {
                jwtToken = new JwtSecurityTokenHandler().ReadJwtToken(token);
                if (jwtToken.ValidTo < DateTime.UtcNow)
                {
                    _logger.LogInformation("Токен истёк");
                    await LogoutAsync();
                    return null;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при проверке валидности токена");
                await LogoutAsync();
                return null;
            }

            var employeeId = Preferences.Get(EmployeeIdKey, 0);
            var role = Preferences.Get(RoleKey, string.Empty);

            // ВАЖНО: восстанавливаем ФИО
            var firstName = Preferences.Get(FirstNameKey, string.Empty);
            var lastName = Preferences.Get(LastNameKey, string.Empty);
            var userId = Preferences.Get(UserIdKey, 0);

            if (employeeId == 0 || string.IsNullOrEmpty(role))
            {
                _logger.LogWarning("Метаданные пользователя не найдены в Preferences");
                await LogoutAsync();
                return null;
            }

            _currentUser = new CurrentUser
            {
                Id = userId,
                EmployeeId = employeeId,
                FirstName = firstName,
                LastName = lastName,
                Role = role,
                AccessToken = token,
                TokenExpiresAt = jwtToken.ValidTo
            };

            _apiClient.SetAuthorizationToken(token);

            _logger.LogInformation("Успешный автологин: EmployeeId={EmployeeId}", employeeId);
            return _currentUser;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при попытке автологина");
            return null;
        }
    }

    public async Task LogoutAsync()
    {
        try
        {
            SecureStorage.Remove(TokenKey);

            Preferences.Remove(UserIdKey);
            Preferences.Remove(EmployeeIdKey);
            Preferences.Remove(RoleKey);
            Preferences.Remove(FirstNameKey);
            Preferences.Remove(LastNameKey);

            _apiClient.SetAuthorizationToken(null);
            _currentUser = null;

            _logger.LogInformation("Успешный выход из приложения");
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при выходе из приложения");
        }
    }
}
