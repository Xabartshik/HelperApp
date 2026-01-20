using HelperApp.Models.Auth;
using HelperApp.Models.User;
using System.IdentityModel.Tokens.Jwt;
using static HelperApp.Services.ApiClient;

namespace HelperApp.Services;

public class AuthService : IAuthService
{
    private readonly IApiClient _apiClient;
    private readonly ILogger<AuthService> _logger;
    private CurrentUser? _currentUser;

    private const string TokenKey = "access_token";
    private const string EmployeeIdKey = "last_employee_id";
    private const string RoleKey = "last_role";

    public AuthService(IApiClient apiClient, ILogger<AuthService> logger)
    {
        _apiClient = apiClient;
        _logger = logger;
    }

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

            // Декодируем токен чтобы получить ExpiresAt
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(response.AccessToken);
            var expiresAt = jwtToken.ValidTo;

            _currentUser = new CurrentUser
            {
                Id = response.User.Id,
                EmployeeId = response.User.EmployeeId,
                FirstName = response.User.FirstName,    
                LastName = response.User.LastName,        
                Role = response.User.Role,
                AccessToken = response.AccessToken,
                TokenExpiresAt = expiresAt
            };

            // Сохраняем токен в SecureStorage
            await SecureStorage.SetAsync(TokenKey, response.AccessToken);

            // Сохраняем метаданные пользователя в Preferences
            Preferences.Set(EmployeeIdKey, response.User.EmployeeId);
            Preferences.Set(RoleKey, response.User.Role);
            Preferences.Set("FirstName", response.User.FirstName); 
            Preferences.Set("LastName", response.User.LastName);   

            // Устанавливаем токен в ApiClient
            _apiClient.SetAuthorizationToken(response.AccessToken);

            _logger.LogInformation("Успешный логин: EmployeeId={EmployeeId}, Role={Role}", 
                response.User.EmployeeId, response.User.Role);

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
            // Пытаемся получить токен из SecureStorage
            var token = await SecureStorage.GetAsync(TokenKey);

            if (string.IsNullOrEmpty(token))
            {
                _logger.LogInformation("Токен не найден в SecureStorage");
                return null;
            }

            // Проверяем, не истёк ли токен
            try
            {
                var handler = new JwtSecurityTokenHandler();
                var jwtToken = handler.ReadJwtToken(token);

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

            // Получаем сохранённые метаданные
            var employeeId = Preferences.Get(EmployeeIdKey, 0);
            var role = Preferences.Get(RoleKey, "");

            if (employeeId == 0 || string.IsNullOrEmpty(role))
            {
                _logger.LogWarning("Метаданные пользователя не найдены в Preferences");
                await LogoutAsync();
                return null;
            }

            _currentUser = new CurrentUser
            {
                EmployeeId = employeeId,
                Role = role,
                AccessToken = token,
                TokenExpiresAt = new JwtSecurityTokenHandler().ReadJwtToken(token).ValidTo
            };

            // Устанавливаем токен в ApiClient
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
            // Очищаем токен
            SecureStorage.Remove(TokenKey);

            // Очищаем метаданные
            Preferences.Remove(EmployeeIdKey);
            Preferences.Remove(RoleKey);

            // Удаляем токен из ApiClient
            _apiClient.SetAuthorizationToken(null);

            _currentUser = null;

            _logger.LogInformation("Успешный выход из приложения");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при выходе из приложения");
        }
    }

    public CurrentUser? GetCurrentUser()
    {
        return _currentUser;
    }
}
