using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using HelperApp.Models;
using HelperApp.Models.Inventory;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Devices;

namespace HelperApp.Services;

public class ApiClient : IApiClient
{
    private const string HasNewTasksForWorkerRouteTemplate = "Inventory/worker/{0}/check-new";
    private const string GetNewAssignmentsForWorkerRouteTemplate = "Inventory/worker/{0}/new-tasks";
    private const string GetInventoryTaskDetailsRouteTemplate = "Inventory/worker/{0}/tasks/{1}/details";

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
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _httpClient = new HttpClient();

        var baseAddress = GetBaseAddress();

        _httpClient.BaseAddress = new Uri(baseAddress);
        _httpClient.Timeout = TimeSpan.FromSeconds(30);
    }

    /// <summary>
    /// Определяет базовый адрес API в зависимости от платформы и типа устройства
    /// </summary>
    private string GetBaseAddress()
    {
#if ANDROID
        // Проверяем, работаем ли мы на эмуляторе
        bool isEmulator = IsAndroidEmulator();

        if (isEmulator)
        {
            _logger.LogInformation("Обнаружен Android эмулятор, используем 10.0.2.2");
            return "http://10.0.2.2:5000/api/v1/";
        }
        else
        {
            // Для физического Android устройства
            // ВАЖНО: Замените на IP адрес вашего компьютера!
            // Узнать можно командой: ipconfig (Windows) или ifconfig (Linux/Mac)
            var physicalDeviceAddress = "http://192.168.0.100:5000/api/v1/";

            _logger.LogInformation("Обнаружено физическое Android устройство, используем {Address}", physicalDeviceAddress);
            return physicalDeviceAddress;
        }
#elif IOS
        // Для iOS симулятора localhost работает напрямую
        _logger.LogInformation("iOS платформа, используем localhost");
        return "http://localhost:5000/api/v1/";
#else
        // Для других платформ (Windows, MacCatalyst и т.д.)
        _logger.LogInformation("Другая платформа, используем localhost");
        return "http://localhost:5000/api/v1/";
#endif
    }

#if ANDROID
    /// <summary>
    /// Проверяет, является ли текущее Android устройство эмулятором
    /// </summary>
    private bool IsAndroidEmulator()
    {
        try
        {
            var fingerprint = Android.OS.Build.Fingerprint?.ToLower() ?? string.Empty;
            var model = Android.OS.Build.Model?.ToLower() ?? string.Empty;
            var manufacturer = Android.OS.Build.Manufacturer?.ToLower() ?? string.Empty;
            var product = Android.OS.Build.Product?.ToLower() ?? string.Empty;
            var hardware = Android.OS.Build.Hardware?.ToLower() ?? string.Empty;

            // Проверяем различные признаки эмулятора
            bool isEmulator =
                fingerprint.Contains("generic") ||
                fingerprint.Contains("unknown") ||
                model.Contains("google_sdk") ||
                model.Contains("emulator") ||
                model.Contains("android sdk") ||
                manufacturer.Contains("genymotion") ||
                product.Contains("sdk") ||
                product.Contains("google_sdk") ||
                product.Contains("sdk_gphone") ||
                product.Contains("vbox86p") ||
                hardware.Contains("goldfish") ||
                hardware.Contains("ranchu");

            _logger.LogDebug(
                "Device info - Fingerprint: {Fingerprint}, Model: {Model}, Manufacturer: {Manufacturer}, Product: {Product}, Hardware: {Hardware}, IsEmulator: {IsEmulator}",
                fingerprint, model, manufacturer, product, hardware, isEmulator);

            return isEmulator;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Не удалось определить тип устройства, предполагаем эмулятор");
            return true; // На всякий случай возвращаем true
        }
    }
#endif

    public void SetAuthorizationToken(string? token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            _httpClient.DefaultRequestHeaders.Authorization = null;
            return;
        }

        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    public async Task<T?> GetAsync<T>(string endpoint, CancellationToken ct = default)
    {
        try
        {
            _hasNetwork = true;

            using var response = await _httpClient.GetAsync(endpoint, ct);
            if (response.StatusCode == HttpStatusCode.Unauthorized)
                throw new UnauthorizedException("Токен истёк или невалиден");

            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync(ct);
            if (string.IsNullOrWhiteSpace(content))
                return default;

            return JsonSerializer.Deserialize<T>(content, _jsonOptions);
        }
        catch (HttpRequestException ex)
        {
            _hasNetwork = false;
            _logger.LogError(ex, "Ошибка сети при GET {Endpoint}", endpoint);
            throw new NoNetworkException("Нет подключения к сети", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при GET {Endpoint}", endpoint);
            throw;
        }
    }

    public async Task<T?> PostAsync<T>(string endpoint, object? data = null, CancellationToken ct = default)
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

            using var response = await _httpClient.PostAsync(endpoint, content, ct);
            if (response.StatusCode == HttpStatusCode.Unauthorized)
                throw new UnauthorizedException("Токен истёк или невалиден");

            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync(ct);
            if (string.IsNullOrWhiteSpace(responseContent))
                return default;

            return JsonSerializer.Deserialize<T>(responseContent, _jsonOptions);
        }
        catch (HttpRequestException ex)
        {
            _hasNetwork = false;
            _logger.LogError(ex, "Ошибка сети при POST {Endpoint}", endpoint);
            throw new NoNetworkException("Нет подключения к сети", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при POST {Endpoint}", endpoint);
            throw;
        }
    }

    public async Task<T?> PostAsync<T>(string endpoint, HttpContent content, CancellationToken ct = default)
    {
        try
        {
            _hasNetwork = true;

            using var response = await _httpClient.PostAsync(endpoint, content, ct);
            if (response.StatusCode == HttpStatusCode.Unauthorized)
                throw new UnauthorizedException("Токен истёк или невалиден");

            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync(ct);
            if (string.IsNullOrWhiteSpace(responseContent))
                return default;

            return JsonSerializer.Deserialize<T>(responseContent, _jsonOptions);
        }
        catch (HttpRequestException ex)
        {
            _hasNetwork = false;
            _logger.LogError(ex, "Ошибка сети при POST {Endpoint}", endpoint);
            throw new NoNetworkException("Нет подключения к сети", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при POST {Endpoint}", endpoint);
            throw;
        }
    }

    // ===== Специфичные методы под задачи =====

    public async Task<bool> HasNewTasksForWorkerAsync(int workerId, CancellationToken cancellationToken = default)
    {
        var endpoint = string.Format(HasNewTasksForWorkerRouteTemplate, workerId);
        var result = await GetAsync<TaskCheckResponse>(endpoint, cancellationToken);
        return result?.HasNewTasks ?? false;
    }

    public Task<List<InventoryAssignmentDetailedWithItemDto>?> GetNewAssignmentsForWorkerAsync(int workerId, CancellationToken cancellationToken = default)
    {
        var endpoint = string.Format(GetNewAssignmentsForWorkerRouteTemplate, workerId);
        return GetAsync<List<InventoryAssignmentDetailedWithItemDto>>(endpoint, cancellationToken);
    }

    public Task<InventoryTaskDetailsDto?> GetInventoryTaskDetailsAsync(
        int userId,
        int inventoryTaskId,
        CancellationToken cancellationToken = default)
    {
        var endpoint = string.Format(GetInventoryTaskDetailsRouteTemplate, userId, inventoryTaskId);
        return GetAsync<InventoryTaskDetailsDto>(endpoint, cancellationToken);
    }

    // ===== Исключения =====
    public class NoNetworkException : Exception
    {
        public NoNetworkException(string message, Exception? innerException = null) : base(message, innerException) { }
    }

    public class UnauthorizedException : Exception
    {
        public UnauthorizedException(string message) : base(message) { }
    }
}
