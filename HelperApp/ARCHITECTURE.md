# Architecture: Mobile App - Server Integration for Inventory Tasks

## System Overview

```
┌─────────────────────────────────────────────────────────────────┐
│                        MOBILE APP (MAUI)                        │
│                      HelperApp - C# / XAML                      │
├─────────────────────────────────────────────────────────────────┤
│                                                                   │
│  ┌──────────────────────────────────────────────────────────┐   │
│  │                    USER INTERFACE (XAML)                 │   │
│  │  ┌─────────────────────────────────────────────────┐    │   │
│  │  │  InventoryTasksPage                              │    │   │
│  │  │  - Task List Display                             │    │   │
│  │  │  - Task Details View                             │    │   │
│  │  │  - Refresh Controls                              │    │   │
│  │  └─────────────────────────────────────────────────┘    │   │
│  └────────────────────────────┬─────────────────────────────┘   │
│                               │                                   │
│  ┌────────────────────────────▼─────────────────────────────┐   │
│  │             VIEWMODEL (Presentation Logic)               │   │
│  │  ┌─────────────────────────────────────────────────┐    │   │
│  │  │  InventoryTasksViewModel                         │    │   │
│  │  │  - Polling Management                            │    │   │
│  │  │  - Task Refresh Logic                            │    │   │
│  │  │  - Data Binding (ObservableProperty)             │    │   │
│  │  │  - Commands (RefreshTasks, SelectTask, etc.)     │    │   │
│  │  └─────────────────────────────────────────────────┘    │   │
│  └────────────────────────────┬─────────────────────────────┘   │
│                               │                                   │
│  ┌────────────────────────────▼─────────────────────────────┐   │
│  │           SERVICE LAYER (HTTP Communication)             │   │
│  │  ┌─────────────────────────────────────────────────┐    │   │
│  │  │  IInventoryApiService (Interface)               │    │   │
│  │  │  InventoryApiService (Implementation)           │    │   │
│  │  │                                                  │    │   │
│  │  │  Methods:                                        │    │   │
│  │  │  - GetTaskDetailsAsync(taskId)                  │    │   │
│  │  │  - GetNewTasksSinceAsync(since)                 │    │   │
│  │  │  - CheckForNewTasksAsync(since)                 │    │   │
│  │  │  - GetUserPendingTasksAsync(userId)             │    │   │
│  │  │  - GetAllTasksAsync()                           │    │   │
│  │  └─────────────────────────────────────────────────┘    │   │
│  └────────────────────────────┬─────────────────────────────┘   │
│                               │                                   │
│  ┌────────────────────────────▼─────────────────────────────┐   │
│  │                 DATA MODELS (DTOs)                       │   │
│  │  - InventoryTaskDetailsDto                              │   │
│  │  - InventoryItemDto                                      │   │
│  │  - TaskCheckResponse                                     │   │
│  │  - InventoryTaskSummary                                  │   │
│  └────────────────────────────┬─────────────────────────────┘   │
│                               │                                   │
└───────────────────────────────┼──────────────────────────────────┘
                                │
                    ┌───────────▼───────────┐
                    │   HttpClient / JSON   │
                    │  HTTPS Communication  │
                    └───────────┬───────────┘
                                │
                                │
┌───────────────────────────────▼───────────────────────────────┐
│                    TASKCONTROL SERVER                          │
│                  ASP.NET Core / C#                             │
├───────────────────────────────────────────────────────────────┤
│                                                                 │
│  ┌──────────────────────────────────────────────────────┐    │
│  │               API LAYER (Controllers)                 │    │
│  │  ┌───────────────────────────────────────────────┐   │    │
│  │  │  InventoryController                          │   │    │
│  │  │                                                │   │    │
│  │  │  Endpoints:                                    │   │    │
│  │  │  GET  /api/inventory                           │   │    │
│  │  │  GET  /api/inventory/{id}                      │   │    │
│  │  │  GET  /api/inventory/user/{userId}             │   │    │
│  │  │  GET  /api/inventory/new?since={time}          │   │    │
│  │  │  GET  /api/inventory/check-new?since={time}    │   │    │
│  │  └───────────────────────────────────────────────┘   │    │
│  └──────────────────────────┬───────────────────────────┘    │
│                             │                                 │
│  ┌──────────────────────────▼───────────────────────────┐    │
│  │         APPLICATION LAYER (Services)                 │    │
│  │  - Business Logic                                    │    │
│  │  - Data Transformation                               │    │
│  │  - DTO Mapping                                       │    │
│  └──────────────────────────┬───────────────────────────┘    │
│                             │                                 │
│  ┌──────────────────────────▼───────────────────────────┐    │
│  │    DATA ACCESS LAYER (Repositories)                  │    │
│  │  - Database Queries                                  │    │
│  │  - Entity Mapping                                    │    │
│  └──────────────────────────┬───────────────────────────┘    │
│                             │                                 │
│  ┌──────────────────────────▼───────────────────────────┐    │
│  │               DATABASE (PostgreSQL)                  │    │
│  │  Tables:                                             │    │
│  │  - inventory_assignments                             │    │
│  │  - inventory_assignment_lines                        │    │
│  │  - inventory_assignment_items                        │    │
│  │  - products / items / positions                      │    │
│  └──────────────────────────────────────────────────────┘    │
│                                                                 │
└──────────────────────────────────────────────────────────────┘
```

## Data Flow Diagram

### Polling Mechanism Flow

```
┌─────────────────────────────────────────────────────────────┐
│ InventoryTasksViewModel.StartPolling()                      │
└────────────────────────┬────────────────────────────────────┘
                         │
                         ▼
    ┌──────────────────────────────────────────┐
    │ Wait 30 seconds (PollingIntervalSeconds)│
    └────────────────────┬─────────────────────┘
                         │
                         ▼
    ┌──────────────────────────────────────────┐
    │ CheckForNewTasksAsync(lastTaskCheck)    │
    └────────────────────┬─────────────────────┘
                         │
                         ▼
    ┌──────────────────────────────────────────────────────────┐
    │ InventoryApiService.CheckForNewTasksAsync()             │
    │ HTTP GET /api/inventory/check-new?since={timestamp}     │
    └────────────────────┬─────────────────────────────────────┘
                         │
          ┌──────────────┴──────────────┐
          │                             │
          ▼                             ▼
    ┌─────────────────┐           ┌──────────────────┐
    │ HasNewTasks=YES │           │ HasNewTasks=NO   │
    └────────┬────────┘           └──────┬───────────┘
             │                           │
             ▼                           ▼
    ┌──────────────────────┐   ┌──────────────────────┐
    │ RefreshTasksAsync()  │   │ Update UI Status     │
    │ (Full task details)  │   │ "No new tasks"       │
    └────────┬─────────────┘   └──────────────────────┘
             │
             ▼
    ┌──────────────────────────────────────────┐
    │ Fetch GetNewTasksSinceAsync(lastCheck)   │
    │ HTTP GET /api/inventory/new?since=...    │
    └────────┬─────────────────────────────────┘
             │
             ▼
    ┌──────────────────────────────────────────┐
    │ Update Tasks Collection (UI)             │
    │ Update lastTaskCheck = DateTime.UtcNow   │
    └──────────────────────────────────────────┘
             │
             └─────────────────────┐
                                   │
              Loop back to wait ◄──┘
```

### Task Selection Flow

```
User Selects Task from List
         │
         ▼
SelectTaskAsync(taskSummary)
         │
         ▼
InventoryApiService.GetTaskDetailsAsync(taskId)
         │
         ▼
HTTP GET /api/inventory/{id}
         │
         ▼
Deserialized InventoryTaskDetailsDto
         │
         ▼
BindingContext Updated: SelectedTaskDetails
         │
         ▼
UI Displays:
- Zone Code
- Total Expected Count
- Item Details (Name, Quantity, Dimensions, Weight, etc.)
```

## Class Responsibility Distribution

### InventoryTasksViewModel
**Responsibilities:**
- Manage polling lifecycle
- Orchestrate API calls
- Update observable collections
- Maintain UI state (IsLoading, StatusMessage)
- Handle commands (Refresh, SelectTask, StopPolling)
- Convert DTOs to UI models

**Key Properties:**
```
- Tasks: ObservableCollection<InventoryTaskSummary>
- SelectedTaskDetails: InventoryTaskDetailsDto
- IsLoading: bool
- StatusMessage: string
- HasNewTasks: bool
```

### InventoryApiService
**Responsibilities:**
- HTTP communication with server
- JSON serialization/deserialization
- Error handling and logging
- Base address and token management
- Request timeout configuration

**Key Methods:**
```
GetTaskDetailsAsync(taskId)
GetAllTasksAsync()
GetUserPendingTasksAsync(userId)
GetNewTasksSinceAsync(since)
CheckForNewTasksAsync(since)
```

### InventoryTasksPage (View)
**Responsibilities:**
- Display task list and details
- Handle user interactions
- Initialize ViewModel on appearing
- Manage polling lifecycle (start/stop)
- Bind UI to ViewModel

### Models/DTOs
**Responsibilities:**
- Data transfer between client and server
- UI binding via observable properties
- JSON deserialization targets

## Polling Strategy Comparison

### Lightweight Check (check-new endpoint)
```
Bandwidth: ~500 bytes
Latency: ~100-200ms
Frequency: Every 30 seconds (configurable)
Response:
{
  "hasNewTasks": bool,
  "newTaskCount": int,
  "latestTaskTime": datetime,
  "lastChecked": datetime
}
```

### Full Refresh (new endpoint)
```
Bandwidth: ~5-50KB (depends on task count)
Latency: ~200-500ms
Frequency: Only when hasNewTasks = true
Response: Full task details array
```

## Error Handling Strategy

```
API Call
    │
    ├─ HttpRequestException
    │  └─ Log warning + return null
    │
    ├─ TaskCanceledException
    │  └─ Log error (timeout) + return null
    │
    ├─ JsonException
    │  └─ Log error (bad response) + return null
    │
    └─ Other Exception
       └─ Log error + return null

ViewModel catches null:
    └─ Updates StatusMessage with error info
```

## Dependency Injection Flow

```
MauiProgram.cs
    │
    ├─ services.AddHttpClient<IInventoryApiService, InventoryApiService>
    │
    ├─ services.AddSingleton<InventoryTasksViewModel>
    │  └─ Injected IInventoryApiService
    │
    └─ services.AddSingleton<InventoryTasksPage>
       └─ Injected InventoryTasksViewModel

At Runtime:
    │
    ├─ HttpClient auto-created for InventoryApiService
    │
    ├─ InventoryTasksViewModel receives IInventoryApiService
    │
    └─ InventoryTasksPage receives InventoryTasksViewModel
```

## Threading Model

```
Main Thread (UI Thread)
    │
    ├─ PollingTaskAsync (Background Task)
    │  └─ Awaits CheckForNewTasksAsync
    │     └─ HTTP call (thread pool)
    │
    └─ UI Updates (MainThread.BeginInvokeOnMainThread)
       ├─ Update Tasks Collection
       ├─ Update SelectedTaskDetails
       └─ Update Status Message
```

## Configuration Points

1. **Server Address**
   ```csharp
   _viewModel.Initialize("https://api.example.com", token);
   ```

2. **Polling Interval**
   ```csharp
   private const int PollingIntervalSeconds = 30; // In ViewModel
   ```

3. **HTTP Timeout**
   ```csharp
   private const int DefaultTimeoutSeconds = 30; // In ApiService
   ```

4. **Logging Level**
   ```csharp
   services.AddLogging(builder => builder.AddDebug());
   ```

## Scalability Considerations

### For Large Task Volumes:
1. **Implement pagination** on /api/inventory endpoint
2. **Add filtering** by zone, status, date range
3. **Cache** recently viewed tasks
4. **Virtual scrolling** in CollectionView

### For Frequent Updates:
1. **Consider SignalR** instead of polling
2. **Implement incremental updates**
3. **Add heartbeat** to detect connection loss

### For Offline Support:
1. **Local SQLite database**
2. **Sync queue** for pending operations
3. **Conflict resolution** strategy

## Security Architecture

```
Authentication Flow:
    │
    ├─ User logs in
    │  └─ Receive JWT token
    │
    ├─ Store in SecureStorage (device-specific encryption)
    │
    └─ Add to every request:
       Authorization: Bearer {token}

Server validates token on each request
    ├─ Valid → Return data
    ├─ Expired → Return 401
    └─ Invalid → Return 403
```

## Future Extensions

1. **Task Completion**
   - Add PUT /api/inventory/{id}/complete
   - Update task status

2. **Real-time Sync**
   - SignalR hub for instant updates
   - Reduce polling overhead

3. **Offline Mode**
   - Local data persistence
   - Queue system for offline actions

4. **Analytics**
   - Track polling success rate
   - Monitor API response times
   - Log error patterns

5. **Advanced Filtering**
   - Query by zone, date, status
   - Full-text search on items
