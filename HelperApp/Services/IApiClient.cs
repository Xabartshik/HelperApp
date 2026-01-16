using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace HelperApp.Services;

public class ApiClient : IApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ApiClient> _logger;
    private bool _hasNetwork = true;

    public bool HasNetwork => _hasNetwork;

    public ApiClient(ILogger<ApiClient> logger)
    {
        _logger = logger;
        _httpClient = new HttpClient();
        
        // Базовый адрес для эмулятора Android
        var baseAddress = DeviceInfo.Current.Platform == DevicePlatform.Android
            ? "http://10.0.2.2:5000/api/v1/"  // Измени порт на свой
            : "http://localhost:5000/api/v1/";  // Для остальных платформ

        _httpClient.BaseAddress = new Uri(baseAddress);
        _httpClient.Timeout = TimeSpan.FromSeconds(30);
    }

    public void SetAuthorizationToken(string? token)
    {
        if (string.IsNullOrEmpty(token))
        {
            _httpClient.DefaultRequestHeaders.Authorization = null;
        }
        else
        {
            _httpClient.DefaultRequestHeaders.Authorization = 
                new AuthenticationHeaderValue("Bearer", token);
        }
    }

    public async Task<T?> GetAsync<T>(string endpoint)
    {
        try
        {
            _hasNetwork = true;
            var response = await _httpClient.GetAsync(endpoint);
            
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                throw new UnauthorizedException("Токен истёк или невалиден");
            }

            response.EnsureSuccessStatusCode();
            
            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<T>(content, 
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            
            return result;
        }
        catch (HttpRequestException ex)
        {
            _hasNetwork = false;
            _logger.LogError(ex, "Ошибка сети при GET {endpoint}", endpoint);
            throw new NoNetworkException("Нет подключения к сети", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при GET {endpoint}", endpoint);
            throw;
        }
    }

    public async Task<T?> PostAsync<T>(string endpoint, object? data = null)
    {
        try
        {
            _hasNetwork = true;
            HttpContent? content = null;

            if (data != null)
            {
                var json = JsonSerializer.Serialize(data);
                content = new StringContent(json, Encoding.UTF8, "application/json");
            }

            var response = await _httpClient.PostAsync(endpoint, content);
            
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                throw new UnauthorizedException("Токен истёк или невалиден");
            }

            response.EnsureSuccessStatusCode();
            
            var responseContent = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<T>(responseContent, 
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            
            return result;
        }
        catch (HttpRequestException ex)
        {
            _hasNetwork = false;
            _logger.LogError(ex, "Ошибка сети при POST {endpoint}", endpoint);
            throw new NoNetworkException("Нет подключения к сети", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при POST {endpoint}", endpoint);
            throw;
        }
    }

    public async Task<T?> PostAsync<T>(string endpoint, HttpContent content)
    {
        try
        {
            _hasNetwork = true;
            var response = await _httpClient.PostAsync(endpoint, content);
            
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                throw new UnauthorizedException("Токен истёк или невалиден");
            }

            response.EnsureSuccessStatusCode();
            
            var responseContent = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<T>(responseContent, 
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            
            return result;
        }
        catch (HttpRequestException ex)
        {
            _hasNetwork = false;
            _logger.LogError(ex, "Ошибка сети при POST {endpoint}", endpoint);
            throw new NoNetworkException("Нет подключения к сети", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при POST {endpoint}", endpoint);
            throw;
        }
    }
}

public class NoNetworkException : Exception
{
    public NoNetworkException(string message, Exception? innerException = null) 
        : base(message, innerException) { }
}

public class UnauthorizedException : Exception
{
    public UnauthorizedException(string message) : base(message) { }
}
