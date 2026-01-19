using System.Net.Http.Json;
using HelperApp.Models;

namespace HelperApp.Services
{
    /// <summary>
    /// Service for communicating with TaskControl server
    /// Handles all inventory task-related API calls from mobile app
    /// </summary>
    public interface IInventoryApiService
    {
        Task<InventoryTaskDetailsDto?> GetTaskDetailsAsync(int taskId, CancellationToken cancellationToken = default);
        Task<List<InventoryTaskDetailsDto>?> GetAllTasksAsync(CancellationToken cancellationToken = default);
        Task<List<InventoryTaskDetailsDto>?> GetUserPendingTasksAsync(int userId, CancellationToken cancellationToken = default);
        Task<List<InventoryTaskDetailsDto>?> GetNewTasksSinceAsync(DateTime? since = null, CancellationToken cancellationToken = default);
        Task<TaskCheckResponse?> CheckForNewTasksAsync(DateTime? since = null, CancellationToken cancellationToken = default);
        void SetBaseAddress(string baseAddress);
        void SetAuthToken(string token);
    }

    /// <summary>
    /// Implementation of inventory API service
    /// Uses HttpClient for REST API communication
    /// </summary>
    public class InventoryApiService : IInventoryApiService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<InventoryApiService> _logger;
        private const string InventoryEndpoint = "api/inventory";
        private const int DefaultTimeoutSeconds = 30;

        public InventoryApiService(HttpClient httpClient, ILogger<InventoryApiService> logger)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _httpClient.Timeout = TimeSpan.FromSeconds(DefaultTimeoutSeconds);
        }

        /// <summary>
        /// Set the base address of the API server
        /// </summary>
        /// <param name="baseAddress">Server base URL (e.g., "https://taskcontrol-api.example.com")</param>
        public void SetBaseAddress(string baseAddress)
        {
            if (string.IsNullOrWhiteSpace(baseAddress))
            {
                _logger.LogWarning("Attempted to set empty base address");
                return;
            }

            try
            {
                _httpClient.BaseAddress = new Uri(baseAddress);
                _logger.LogInformation("API base address set to: {BaseAddress}", baseAddress);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to set base address: {BaseAddress}", baseAddress);
            }
        }

        /// <summary>
        /// Set authentication token for API requests
        /// </summary>
        /// <param name="token">Bearer token from authentication</param>
        public void SetAuthToken(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                _httpClient.DefaultRequestHeaders.Authorization = null;
                _logger.LogInformation("Auth token cleared");
                return;
            }

            _httpClient.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            _logger.LogInformation("Auth token set successfully");
        }

        /// <summary>
        /// Get detailed information about a specific inventory task
        /// Called when user selects a task from the list
        /// </summary>
        /// <param name="taskId">ID of the task to retrieve</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Task details or null if not found</returns>
        public async Task<InventoryTaskDetailsDto?> GetTaskDetailsAsync(int taskId, CancellationToken cancellationToken = default)
        {
            if (taskId <= 0)
            {
                _logger.LogWarning("Invalid task ID: {TaskId}", taskId);
                return null;
            }

            try
            {
                _logger.LogInformation("Fetching inventory task details for task {TaskId}", taskId);

                var response = await _httpClient.GetAsync(
                    $"{InventoryEndpoint}/{taskId}",
                    cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Failed to fetch task details. Status: {StatusCode}", response.StatusCode);
                    return null;
                }

                var task = await response.Content.ReadAsAsync<InventoryTaskDetailsDto?>(cancellationToken);
                _logger.LogInformation("Successfully retrieved task {TaskId} with {ItemCount} items", 
                    taskId, task?.Items.Count ?? 0);

                return task;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP error retrieving task {TaskId}", taskId);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving task details for {TaskId}", taskId);
                return null;
            }
        }

        /// <summary>
        /// Get all inventory tasks from server
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of all inventory tasks or empty list on error</returns>
        public async Task<List<InventoryTaskDetailsDto>?> GetAllTasksAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Fetching all inventory tasks");

                var response = await _httpClient.GetAsync(
                    InventoryEndpoint,
                    cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Failed to fetch all tasks. Status: {StatusCode}", response.StatusCode);
                    return null;
                }

                var tasks = await response.Content.ReadAsAsync<List<InventoryTaskDetailsDto>?>(cancellationToken);
                _logger.LogInformation("Successfully retrieved {TaskCount} inventory tasks", tasks?.Count ?? 0);

                return tasks;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all inventory tasks");
                return null;
            }
        }

        /// <summary>
        /// Get pending (unfinished) tasks assigned to specific user
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of user's pending tasks or null on error</returns>
        public async Task<List<InventoryTaskDetailsDto>?> GetUserPendingTasksAsync(int userId, CancellationToken cancellationToken = default)
        {
            if (userId <= 0)
            {
                _logger.LogWarning("Invalid user ID: {UserId}", userId);
                return null;
            }

            try
            {
                _logger.LogInformation("Fetching pending tasks for user {UserId}", userId);

                var response = await _httpClient.GetAsync(
                    $"{InventoryEndpoint}/user/{userId}",
                    cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Failed to fetch user tasks. Status: {StatusCode}", response.StatusCode);
                    return null;
                }

                var tasks = await response.Content.ReadAsAsync<List<InventoryTaskDetailsDto>?>(cancellationToken);
                _logger.LogInformation("Successfully retrieved {TaskCount} pending tasks for user {UserId}", 
                    tasks?.Count ?? 0, userId);

                return tasks;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving pending tasks for user {UserId}", userId);
                return null;
            }
        }

        /// <summary>
        /// Get new tasks created after specified timestamp
        /// Used for polling mechanism to check for new assignments
        /// </summary>
        /// <param name="since">Timestamp in UTC. If null, returns all new tasks.</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of new inventory tasks or null on error</returns>
        public async Task<List<InventoryTaskDetailsDto>?> GetNewTasksSinceAsync(DateTime? since = null, CancellationToken cancellationToken = default)
        {
            try
            {
                var query = since.HasValue ? $"?since={since.Value:O}" : "";
                _logger.LogInformation("Fetching new inventory tasks since {Since}", since?.ToString("O") ?? "start");

                var response = await _httpClient.GetAsync(
                    $"{InventoryEndpoint}/new{query}",
                    cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Failed to fetch new tasks. Status: {StatusCode}", response.StatusCode);
                    return null;
                }

                var tasks = await response.Content.ReadAsAsync<List<InventoryTaskDetailsDto>?>(cancellationToken);
                _logger.LogInformation("Successfully retrieved {TaskCount} new inventory tasks", tasks?.Count ?? 0);

                return tasks;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving new inventory tasks");
                return null;
            }
        }

        /// <summary>
        /// Lightweight check for new tasks without fetching full data
        /// Optimal for frequent polling - returns only metadata
        /// </summary>
        /// <param name="since">Timestamp in UTC to check tasks after this time</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Task check response or null on error</returns>
        public async Task<TaskCheckResponse?> CheckForNewTasksAsync(DateTime? since = null, CancellationToken cancellationToken = default)
        {
            try
            {
                var query = since.HasValue ? $"?since={since.Value:O}" : "";
                _logger.LogInformation("Checking for new inventory tasks since {Since}", since?.ToString("O") ?? "start");

                var response = await _httpClient.GetAsync(
                    $"{InventoryEndpoint}/check-new{query}",
                    cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Failed to check for new tasks. Status: {StatusCode}", response.StatusCode);
                    return null;
                }

                var checkResponse = await response.Content.ReadAsAsync<TaskCheckResponse?>(cancellationToken);
                _logger.LogInformation(
                    "Task check result: HasNewTasks={HasNewTasks}, Count={Count}",
                    checkResponse?.HasNewTasks ?? false,
                    checkResponse?.NewTaskCount ?? 0);

                return checkResponse;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking for new inventory tasks");
                return null;
            }
        }
    }

    /// <summary>
    /// Extension methods for HttpContent to deserialize JSON
    /// </summary>
    internal static class HttpContentExtensions
    {
        public static async Task<T?> ReadAsAsync<T>(this HttpContent content, CancellationToken cancellationToken = default)
        {
            using (var stream = await content.ReadAsStreamAsync(cancellationToken))
            {
                return await System.Text.Json.JsonSerializer.DeserializeAsync<T>(stream, cancellationToken: cancellationToken);
            }
        }
    }
}
