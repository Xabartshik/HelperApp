using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using HelperApp.Models.Inventory;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Devices;

namespace HelperApp.Services;

public class ApiClient : IApiClient
{

    private const string HasNewTasksForWorkerRouteTemplate = "Inventory/worker/{0}/check-new";
    private const string GetNewAssignmentsForWorkerRouteTemplate = "Inventory/worker/{0}/new-tasks";

    private readonly HttpClient _httpClient;
    private readonly ILogger<ApiClient> _logger;

    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private bool _hasNetwork = true;
    public bool HasNetwork => _hasNetwork;

    public ApiClient(ILogger<ApiClient> logger)
    {
        _logger = logger;
        _httpClient = new HttpClient();

        // Базовый адрес для эмулятора Android
        var baseAddress = DeviceInfo.Current.Platform == DevicePlatform.Android
            ? "http://10.0.2.2:5000/api/v1/"
            : "http://localhost:5000/api/v1/";

        _httpClient.BaseAddress = new Uri(baseAddress);
        _httpClient.Timeout = TimeSpan.FromSeconds(30);
    }

    public void SetAuthorizationToken(string? token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            _httpClient.DefaultRequestHeaders.Authorization = null;
            return;
        }

        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    public async Task<T?> GetAsync<T>(string endpoint, CancellationToken cancellationToken = default)
    {
        try
        {
            _hasNetwork = true;

            using var response = await _httpClient.GetAsync(endpoint, cancellationToken);

            if (response.StatusCode == HttpStatusCode.Unauthorized)
                throw new UnauthorizedException("Токен истёк или невалиден");

            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync(cancellationToken);

            if (string.IsNullOrWhiteSpace(content))
                return default;

            return JsonSerializer.Deserialize<T>(content, _jsonOptions);
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

    public async Task<T?> PostAsync<T>(string endpoint, object? data = null, CancellationToken cancellationToken = default)
    {
        try
        {
            _hasNetwork = true;

            HttpContent? content = null;
            if (data != null)
            {
                var json = JsonSerializer.Serialize(data, _jsonOptions);
                content = new StringContent(json, Encoding.UTF8, "application/json");
            }

            using var response = await _httpClient.PostAsync(endpoint, content, cancellationToken);

            if (response.StatusCode == HttpStatusCode.Unauthorized)
                throw new UnauthorizedException("Токен истёк или невалиден");

            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

            if (string.IsNullOrWhiteSpace(responseContent))
                return default;

            return JsonSerializer.Deserialize<T>(responseContent, _jsonOptions);
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

    public async Task<T?> PostAsync<T>(string endpoint, HttpContent content, CancellationToken cancellationToken = default)
    {
        try
        {
            _hasNetwork = true;

            using var response = await _httpClient.PostAsync(endpoint, content, cancellationToken);

            if (response.StatusCode == HttpStatusCode.Unauthorized)
                throw new UnauthorizedException("Токен истёк или невалиден");

            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

            if (string.IsNullOrWhiteSpace(responseContent))
                return default;

            return JsonSerializer.Deserialize<T>(responseContent, _jsonOptions);
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

    // ===== Специфичные методы под задачи =====

    public async Task<bool> HasNewTasksForWorkerAsync(int workerId, CancellationToken cancellationToken = default)
    {
        var endpoint = string.Format(HasNewTasksForWorkerRouteTemplate, workerId);
        var result = await GetAsync<bool?>(endpoint, cancellationToken);
        return result ?? false;
    }

    public async Task<IReadOnlyList<InventoryAssignmentDetailedDto>?> GetNewAssignmentsForWorkerAsync(
        int workerId,
        CancellationToken cancellationToken = default)
    {
        var endpoint = string.Format(GetNewAssignmentsForWorkerRouteTemplate, workerId);
        return await GetAsync<List<InventoryAssignmentDetailedDto>>(endpoint, cancellationToken);
    }

    // ===== Исключения =====

    public class NoNetworkException : Exception
    {
        public NoNetworkException(string message, Exception? innerException = null)
            : base(message, innerException) { }
    }

    public class UnauthorizedException : Exception
    {
        public UnauthorizedException(string message) : base(message) { }
    }
}
