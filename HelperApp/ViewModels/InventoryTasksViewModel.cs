using System.Collections.ObjectModel;
using HelperApp.Models;
using System.Windows.Input;

namespace HelperApp.ViewModels
{
    /// <summary>
    /// ViewModel for inventory tasks management
    /// Handles task polling, retrieval, and UI updates
    /// </summary>
    public partial class InventoryTasksViewModel : BaseViewModel
    {
        private readonly IInventoryApiService _apiService;
        private CancellationTokenSource? _pollingCts;
        private DateTime _lastTaskCheck;
        private const int PollingIntervalSeconds = 30; // Adjust based on your needs

        [ObservableProperty]
        ObservableCollection<InventoryTaskSummary> tasks = new();

        [ObservableProperty]
        bool isLoading;

        [ObservableProperty]
        string statusMessage = "Ready";

        [ObservableProperty]
        InventoryTaskDetailsDto? selectedTaskDetails;

        [ObservableProperty]
        bool hasNewTasks;

        public ICommand RefreshTasksCommand { get; }
        public ICommand SelectTaskCommand { get; }
        public ICommand StopPollingCommand { get; }

        public InventoryTasksViewModel(IInventoryApiService apiService)
        {
            _apiService = apiService ?? throw new ArgumentNullException(nameof(apiService));
            _lastTaskCheck = DateTime.UtcNow;

            RefreshTasksCommand = new AsyncRelayCommand(RefreshTasksAsync);
            SelectTaskCommand = new AsyncRelayCommand<InventoryTaskSummary>(SelectTaskAsync);
            StopPollingCommand = new RelayCommand(StopPolling);
        }

        /// <summary>
        /// Initialize the view model with server configuration
        /// </summary>
        /// <param name="serverAddress">Base address of the TaskControl server</param>
        /// <param name="authToken">Bearer token for authentication</param>
        public void Initialize(string serverAddress, string authToken)
        {
            _apiService.SetBaseAddress(serverAddress);
            if (!string.IsNullOrEmpty(authToken))
            {
                _apiService.SetAuthToken(authToken);
            }
        }

        /// <summary>
        /// Start polling for new tasks at regular intervals
        /// </summary>
        public void StartPolling()
        {
            if (_pollingCts?.IsCancellationRequested == false)
            {
                StatusMessage = "Polling already active";
                return;
            }

            _pollingCts = new CancellationTokenSource();
            _lastTaskCheck = DateTime.UtcNow;
            StatusMessage = "Started polling for tasks...";

            _ = PollTasksAsync(_pollingCts.Token);
        }

        /// <summary>
        /// Stop active polling
        /// </summary>
        private void StopPolling()
        {
            _pollingCts?.Cancel();
            _pollingCts?.Dispose();
            _pollingCts = null;
            StatusMessage = "Polling stopped";
        }

        /// <summary>
        /// Polling loop that periodically checks for new tasks
        /// </summary>
        private async Task PollTasksAsync(CancellationToken cancellationToken)
        {
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    await Task.Delay(TimeSpan.FromSeconds(PollingIntervalSeconds), cancellationToken);

                    await CheckForNewTasksAsync(cancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
                StatusMessage = "Polling cancelled";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Polling error: {ex.Message}";
            }
        }

        /// <summary>
        /// Check if there are new tasks without fetching full details
        /// Lightweight operation for frequent polling
        /// </summary>
        public async Task CheckForNewTasksAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var checkResponse = await _apiService.CheckForNewTasksAsync(_lastTaskCheck, cancellationToken);

                if (checkResponse == null)
                {
                    StatusMessage = "Failed to check tasks";
                    return;
                }

                HasNewTasks = checkResponse.HasNewTasks;

                if (checkResponse.HasNewTasks && checkResponse.NewTaskCount > 0)
                {
                    StatusMessage = $"Found {checkResponse.NewTaskCount} new task(s)!";
                    await RefreshTasksAsync(cancellationToken);
                    _lastTaskCheck = DateTime.UtcNow;
                }
                else
                {
                    StatusMessage = "No new tasks";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error checking tasks: {ex.Message}";
            }
        }

        /// <summary>
        /// Refresh the list of tasks from server
        /// </summary>
        [RelayCommand]
        public async Task RefreshTasksAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                IsLoading = true;
                StatusMessage = "Loading tasks...";

                var newTasks = await _apiService.GetNewTasksSinceAsync(_lastTaskCheck, cancellationToken);

                if (newTasks == null)
                {
                    StatusMessage = "Failed to load tasks";
                    return;
                }

                // Convert DTOs to summary models
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    Tasks.Clear();
                    foreach (var task in newTasks)
                    {
                        Tasks.Add(new InventoryTaskSummary
                        {
                            TaskId = task.TaskId,
                            ZoneCode = task.ZoneCode,
                            TotalItems = task.Items.Count,
                            InitiatedAt = task.InitiatedAt,
                            HasBeenViewed = false
                        });
                    }

                    _lastTaskCheck = DateTime.UtcNow;
                    StatusMessage = $"Loaded {Tasks.Count} task(s)";
                });
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Load detailed information for selected task
        /// </summary>
        public async Task SelectTaskAsync(InventoryTaskSummary? task, CancellationToken cancellationToken = default)
        {
            if (task == null)
            {
                SelectedTaskDetails = null;
                return;
            }

            try
            {
                IsLoading = true;
                StatusMessage = $"Loading details for task {task.TaskId}...";

                var details = await _apiService.GetTaskDetailsAsync(task.TaskId, cancellationToken);

                MainThread.BeginInvokeOnMainThread(() =>
                {
                    SelectedTaskDetails = details;
                    if (details != null)
                    {
                        task.HasBeenViewed = true;
                        StatusMessage = $"Task {task.TaskId}: {details.Items.Count} items in zone {details.ZoneCode}";
                    }
                    else
                    {
                        StatusMessage = "Failed to load task details";
                    }
                });
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error loading task: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Load all tasks from server (non-polling mode)
        /// </summary>
        [RelayCommand]
        public async Task LoadAllTasksAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                IsLoading = true;
                StatusMessage = "Loading all tasks...";

                var allTasks = await _apiService.GetAllTasksAsync(cancellationToken);

                if (allTasks == null)
                {
                    StatusMessage = "Failed to load tasks";
                    return;
                }

                MainThread.BeginInvokeOnMainThread(() =>
                {
                    Tasks.Clear();
                    foreach (var task in allTasks)
                    {
                        Tasks.Add(new InventoryTaskSummary
                        {
                            TaskId = task.TaskId,
                            ZoneCode = task.ZoneCode,
                            TotalItems = task.Items.Count,
                            InitiatedAt = task.InitiatedAt,
                            HasBeenViewed = false
                        });
                    }

                    StatusMessage = $"Loaded {Tasks.Count} task(s)";
                });
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        public override void OnNavigatedTo(object? parameter)
        {
            base.OnNavigatedTo(parameter);
            // Initialize polling when navigating to tasks page
            if (parameter is string serverAddress)
            {
                Initialize(serverAddress, "");
            }
        }

        public override void Dispose()
        {
            StopPolling();
            base.Dispose();
        }
    }

    /// <summary>
    /// Base ViewModel class (if you don't have one, you can simplify this)
    /// </summary>
    public abstract class BaseViewModel : IDisposable
    {
        public virtual void OnNavigatedTo(object? parameter) { }
        public virtual void OnNavigatedFrom() { }
        public virtual void Dispose() { }
    }
}
