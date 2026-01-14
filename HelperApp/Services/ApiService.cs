using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using HelperApp.Models;

namespace HelperApp.Services;

public class ApiService
{
    private readonly HttpClient _http;

    public ApiService()
    {
        // Создаём HttpClient с SocketsHttpHandler для корректной работы на Android
        var handler = new SocketsHttpHandler
        {
            AllowAutoRedirect = true,
            PooledConnectionLifetime = TimeSpan.Zero // избегаем проблем с кэшированием соединений
        };

        _http = new HttpClient(handler)
        {
            BaseAddress = new Uri("http://10.0.2.2:5000/")
        };
    }

    public async Task<MobileAppUserDto?> LoginAsync(string username, string password)
    {
        try
        {
            var jsonContent = JsonContent.Create(new
            {
                username,
                password
            });

            var response = await _http.PostAsync("api/v1/mobileappuser/validate", jsonContent);

            if (!response.IsSuccessStatusCode)
                return null;

            var user = await response.Content.ReadFromJsonAsync<MobileAppUserDto>(new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return user;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка запроса: {ex.Message}");
            return null;
        }
    }
}
