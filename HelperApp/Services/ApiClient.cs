namespace HelperApp.Services;

public interface IApiClient
{
    Task<T?> GetAsync<T>(string endpoint);
    Task<T?> PostAsync<T>(string endpoint, object? data = null);
    Task<T?> PostAsync<T>(string endpoint, HttpContent content);
    void SetAuthorizationToken(string? token);
    bool HasNetwork { get; }
}
