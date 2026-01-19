# Inventory API Integration Guide

This document describes the integration between HelperApp (mobile client) and TaskControl server for inventory task management.

## Overview

The integration allows the mobile app to:
1. Poll the server for new inventory tasks
2. Retrieve detailed task information when needed
3. Display task details to the user

## Architecture

```
TaskControl Server (ASP.NET)
         ↓
   API Endpoints
         ↓
HelperApp (MAUI)
   ├─ Services (InventoryApiService)
   ├─ ViewModels (InventoryTasksViewModel)
   ├─ Models (InventoryTaskDetailsDto, etc.)
   └─ Views (UI components)
```

## API Endpoints

All endpoints are under `/api/inventory`

### 1. Get All Tasks
**GET** `/api/inventory`

Returns all inventory tasks.

**Response (200 OK):**
```json
[
  {
    "taskId": 1,
    "zoneCode": "ZONE_A",
    "items": [...],
    "totalExpectedCount": 50,
    "initiatedAt": "2026-01-19T09:00:00Z"
  }
]
```

### 2. Get Task Details by ID
**GET** `/api/inventory/{id}`

Returns detailed information for a specific task.

**Parameters:**
- `id` (int, path): Task ID

**Response (200 OK):**
```json
{
  "taskId": 1,
  "zoneCode": "ZONE_A",
  "items": [
    {
      "itemId": 101,
      "itemName": "Item 1",
      "positionCode": "POS_001",
      "positionId": 1,
      "expectedQuantity": 10,
      "weight": 2.5,
      "length": 30.0,
      "width": 20.0,
      "height": 15.0,
      "status": "Available"
    }
  ],
  "totalExpectedCount": 50,
  "initiatedAt": "2026-01-19T09:00:00Z"
}
```

**Error Response (404 Not Found):**
```json
{
  "message": "Inventory task 999 not found"
}
```

### 3. Get User's Pending Tasks
**GET** `/api/inventory/user/{userId}`

Returns tasks assigned to a specific user.

**Parameters:**
- `userId` (int, path): User ID

**Response (200 OK):**
Same as endpoint 1

### 4. Get New Tasks Since Timestamp
**GET** `/api/inventory/new?since={timestamp}`

Returns tasks created after the specified timestamp.

**Query Parameters:**
- `since` (datetime, optional): ISO 8601 format timestamp (UTC)
  - Example: `2026-01-19T09:00:00Z`
  - If not provided, returns all tasks

**Response (200 OK):**
Same as endpoint 1

### 5. Check for New Tasks (Lightweight)
**GET** `/api/inventory/check-new?since={timestamp}`

Lightweight endpoint for polling. Returns only task metadata without full details.

**Query Parameters:**
- `since` (datetime, optional): ISO 8601 format timestamp

**Response (200 OK):**
```json
{
  "hasNewTasks": true,
  "newTaskCount": 3,
  "latestTaskTime": "2026-01-19T10:30:00Z",
  "lastChecked": "2026-01-19T10:35:00Z"
}
```

## Setup in HelperApp

### 1. Register Services in MauiProgram.cs

```csharp
public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            // ... other configurations ...
            .ConfigureServices(services =>
            {
                // Register HTTP client for inventory API
                services.AddHttpClient<IInventoryApiService, InventoryApiService>();
                
                // Register ViewModel
                services.AddSingleton<InventoryTasksViewModel>();
                
                // Register your pages/views as needed
                services.AddSingleton<InventoryTasksPage>();
            });

        return builder.Build();
    }
}
```

### 2. Initialize in Your View or ViewModel

```csharp
public partial class InventoryTasksPage : ContentPage
{
    private readonly InventoryTasksViewModel _viewModel;

    public InventoryTasksPage(InventoryTasksViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        
        // Configure API connection
        _viewModel.Initialize(
            serverAddress: "https://taskcontrol-api.example.com",
            authToken: SecureStorage.GetToken() // Get from your auth service
        );
        
        // Start polling for tasks
        _viewModel.StartPolling();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        
        // Stop polling when leaving page
        _viewModel.StopPollingCommand?.Execute(null);
    }
}
```

## Usage Examples

### Example 1: Initialize and Start Polling

```csharp
// In your ViewModel or Page
private readonly InventoryTasksViewModel _viewModel;

public async Task Initialize()
{
    // Set server configuration
    _viewModel.Initialize(
        serverAddress: "https://localhost:5001",
        authToken: "your-jwt-token"
    );
    
    // Start automatic polling
    _viewModel.StartPolling();
}
```

### Example 2: Manual Task Refresh

```csharp
// Refresh task list manually
await _viewModel.RefreshTasksAsync();

// Check for new tasks without full refresh
await _viewModel.CheckForNewTasksAsync();
```

### Example 3: Select and View Task Details

```csharp
// When user selects a task from list
var selectedTask = Tasks[0]; // Get from UI selection
await _viewModel.SelectTaskAsync(selectedTask);

// SelectedTaskDetails now contains full task information
var details = _viewModel.SelectedTaskDetails;
if (details != null)
{
    foreach (var item in details.Items)
    {
        // Process item data for display
        var itemInfo = $"{item.ItemName} - Qty: {item.ExpectedQuantity}";
    }
}
```

### Example 4: Direct API Service Usage

```csharp
public class MyService
{
    private readonly IInventoryApiService _apiService;
    
    public MyService(IInventoryApiService apiService)
    {
        _apiService = apiService;
    }
    
    public async Task GetTaskDetails(int taskId)
    {
        // Configure
        _apiService.SetBaseAddress("https://api.example.com");
        _apiService.SetAuthToken("token");
        
        // Get task
        var task = await _apiService.GetTaskDetailsAsync(taskId);
        if (task != null)
        {
            // Use task data
        }
    }
}
```

## Polling Strategy

The `InventoryTasksViewModel` implements a polling mechanism:

1. **Check-only polling** (default every 30 seconds):
   - Calls `/api/inventory/check-new?since={lastCheck}`
   - Only returns metadata (count, timestamp)
   - Very lightweight - minimal bandwidth/battery impact
   - If `HasNewTasks = true`, triggers full refresh

2. **Full refresh** (on demand or when new tasks detected):
   - Calls `/api/inventory/new?since={lastCheck}`
   - Retrieves complete task details
   - Updates local UI collection

### Adjusting Polling Interval

Modify in `InventoryTasksViewModel.cs`:

```csharp
private const int PollingIntervalSeconds = 30; // Change this value
```

## Error Handling

The service implements comprehensive error handling:

```csharp
// All methods return null on error and log the exception
var task = await _apiService.GetTaskDetailsAsync(taskId);

if (task == null)
{
    // Handle error - check logs for details
    await DisplayAlert("Error", "Failed to load task", "OK");
}
```

## Configuration

### Server Base Address

Set your server address (with or without trailing slash):

```csharp
_apiService.SetBaseAddress("https://taskcontrol-api.example.com");
// or
_apiService.SetBaseAddress("https://taskcontrol-api.example.com/");
```

### Authentication Token

Set Bearer token from your authentication:

```csharp
var token = await SecureStorage.GetAsync("auth_token");
if (!string.IsNullOrEmpty(token))
{
    _apiService.SetAuthToken(token);
}
```

### Timeout

Default timeout is 30 seconds (modifiable in `InventoryApiService.cs`):

```csharp
private const int DefaultTimeoutSeconds = 30;
```

## Logging

All operations are logged. Enable debug logging:

```csharp
// In MauiProgram.cs
services.AddLogging(builder =>
{
    builder.AddDebug();
});
```

Log categories:
- `HelperApp.Services.InventoryApiService` - API service logs
- `HelperApp.ViewModels.InventoryTasksViewModel` - ViewModel logs

## Database Tables

The server populates these tables when creating inventory tasks:

- `inventory_assigneent_lines` - Main inventory assignments
- `inventory_assigneent_items` - Individual items in assignments

These are queried by the API endpoints to build the response DTOs.

## Best Practices

1. **Always dispose of resources:**
   ```csharp
   protected override void OnDisappearing()
   {
       _viewModel?.Dispose();
   }
   ```

2. **Use async/await properly:**
   ```csharp
   await _viewModel.RefreshTasksAsync(cancellationToken);
   ```

3. **Check for network connectivity:**
   ```csharp
   if (!Connectivity.Current.NetworkAccess == NetworkAccess.Internet)
   {
       // Handle offline scenario
   }
   ```

4. **Cache task data locally when possible:**
   - Store recently viewed tasks
   - Allow offline viewing
   - Reduce API calls

5. **Implement proper error recovery:**
   ```csharp
   int retries = 3;
   while (retries > 0)
   {
       var result = await _apiService.GetTaskDetailsAsync(taskId);
       if (result != null) break;
       retries--;
       await Task.Delay(1000); // Wait before retry
   }
   ```

## Troubleshooting

### Tasks not loading?
- Verify server address is correct and accessible
- Check authentication token validity
- Review logs for specific error messages
- Ensure API endpoints are implemented on server

### Polling not working?
- Check polling interval configuration
- Verify CancellationToken is not cancelled
- Check network connectivity
- Monitor thread count if using MainThread.BeginInvokeOnMainThread

### Slow performance?
- Reduce polling interval if appropriate
- Use check-new endpoint instead of full refresh
- Implement local caching
- Consider pagination for large task lists

## Security Considerations

1. **Always use HTTPS** in production
2. **Validate tokens** before API calls
3. **Don't store sensitive data** in local properties
4. **Implement token refresh** when expired
5. **Use SecureStorage** for sensitive information

## Future Enhancements

- Add support for task filtering (by zone, status, etc.)
- Implement pagination for large task lists
- Add offline caching and sync
- Support for task updates/completion
- Real-time notifications via SignalR

## Support

For issues or questions, refer to:
- GitHub Issues in respective repositories
- Server API documentation (Swagger)
- Client code comments and XML documentation
