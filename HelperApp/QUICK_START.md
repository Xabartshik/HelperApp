# Quick Start Guide - Inventory API Integration

## Step 1: Add Services to MauiProgram.cs

In `MauiProgram.cs`, add the following to your `ConfigureServices` or create `AddInventoryServices` extension:

```csharp
public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();

        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            })
            // Add these lines:
            .Services
            .AddHttpClient<IInventoryApiService, InventoryApiService>()
            .ConfigureHttpClient(client =>
            {
                client.Timeout = TimeSpan.FromSeconds(30);
            })
            .End()
            .AddLogging(builder => builder.AddDebug())
            .AddSingleton<InventoryTasksViewModel>()
            .AddSingleton<InventoryTasksPage>();

        return builder.Build();
    }
}
```

## Step 2: Update GlobalUsings.cs

Add these using statements to `GlobalUsings.cs`:

```csharp
using HelperApp.Models;
using HelperApp.Services;
using HelperApp.ViewModels;
using HelperApp.Views;
using System.Windows.Input;
```

## Step 3: Register the Page in AppShell.xaml

Add route to `AppShell.xaml`:

```xml
<ShellContent
    Title="Tasks"
    ContentTemplate="{DataTemplate local:InventoryTasksPage}"
    Route="inventorytasks" />
```

## Step 4: Configure Server Connection

### Option A: Hardcoded (Development Only)

In `InventoryTasksPage.xaml.cs` InitializeViewModel method:

```csharp
private void InitializeViewModel()
{
    _viewModel.Initialize(
        serverAddress: "https://your-api-server.com",
        authToken: "your-jwt-token"
    );
}
```

### Option B: From SecureStorage (Recommended)

```csharp
private async void InitializeViewModel()
{
    var serverAddress = await SecureStorage.GetAsync("server_address") 
        ?? "https://default-server.com";
    var authToken = await SecureStorage.GetAsync("auth_token") ?? "";
    
    _viewModel.Initialize(serverAddress, authToken);
}
```

## Step 5: Basic Usage Example

### In a Page or ViewModel:

```csharp
public class MyPage : ContentPage
{
    private readonly InventoryTasksViewModel _viewModel;

    public MyPage(InventoryTasksViewModel viewModel)
    {
        _viewModel = viewModel;
        BindingContext = viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        
        // Initialize with your server
        _viewModel.Initialize(
            "https://api.example.com",
            "your-auth-token"
        );
        
        // Start automatic polling
        _viewModel.StartPolling();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        
        // Stop polling
        _viewModel.StopPollingCommand?.Execute(null);
    }
}
```

## Step 6: Test the Integration

1. **Build and run the app**
   ```bash
   dotnet build
   dotnet maui run -f net8.0-android
   # or
   dotnet maui run -f net8.0-ios
   ```

2. **Verify server connection**
   - Check Debug output for connection logs
   - Watch for "API base address set to" log message

3. **Create a test task on the server**
   - Use TaskControl API to create inventory task
   - Verify it appears in the mobile app

4. **Test polling**
   - App should check for new tasks every 30 seconds
   - Create new task on server
   - Verify it appears in app within 30 seconds

## Common Issues and Solutions

### Issue: "CORS error" or "No response from server"
**Solution:** 
- Ensure server address is correct and includes protocol (https://)
- Verify server is running and accessible
- Check CORS configuration on server
- Use `http://` for development (localhost)

### Issue: "401 Unauthorized"
**Solution:**
- Verify auth token is valid
- Token format should be: `Authorization: Bearer {token}`
- Check token expiration

### Issue: Tasks not loading
**Solution:**
- Check debug logs for specific error
- Verify endpoints exist on server:
  - GET /api/inventory
  - GET /api/inventory/{id}
  - GET /api/inventory/check-new
- Ensure inventory tasks exist in database

### Issue: App crashes on startup
**Solution:**
- Remove any null references
- Verify HttpClient is properly injected
- Check for missing using statements
- Review exception logs

## Next Steps

1. **Implement task completion**
   - Add PUT/POST endpoint for marking tasks as complete
   - Add UI for task submission

2. **Add offline support**
   - Cache tasks locally using SQLite
   - Sync when connection restored

3. **Implement real-time updates**
   - Consider SignalR for real-time notifications
   - Instead of polling

4. **Add advanced filtering**
   - Filter tasks by zone, status, etc.
   - Add search functionality

## Testing Endpoints Manually

### Using curl or Postman:

```bash
# Get all tasks
curl -H "Authorization: Bearer YOUR_TOKEN" \
  https://api.example.com/api/inventory

# Get specific task
curl -H "Authorization: Bearer YOUR_TOKEN" \
  https://api.example.com/api/inventory/1

# Check for new tasks
curl -H "Authorization: Bearer YOUR_TOKEN" \
  "https://api.example.com/api/inventory/check-new?since=2026-01-19T09:00:00Z"
```

## Files Created

- ✅ `HelperApp/Services/InventoryApiService.cs` - API communication
- ✅ `HelperApp/ViewModels/InventoryTasksViewModel.cs` - Business logic
- ✅ `HelperApp/Models/InventoryModels.cs` - Data models
- ✅ `HelperApp/Views/InventoryTasksPage.xaml` - UI design
- ✅ `HelperApp/Views/InventoryTasksPage.xaml.cs` - Code-behind
- ✅ `TaskControl/Presentation/InventoryController.cs` - Server endpoints
- ✅ `INVENTORY_API_INTEGRATION.md` - Full documentation

## Server Configuration (TaskControl)

Ensure your TaskControl API has:

1. **CORS enabled** for mobile app domain
2. **Authentication** properly configured
3. **InventoryController** registered in routing
4. **Database** populated with inventory tasks

Example Startup configuration:

```csharp
// In Program.cs or Startup.cs
services.AddCors(options =>
{
    options.AddPolicy("MobilePolicy",
        builder => builder
            .WithOrigins("*")
            .AllowAnyMethod()
            .AllowAnyHeader());
});

app.UseCors("MobilePolicy");
```

## Support

For detailed documentation, see: `INVENTORY_API_INTEGRATION.md`

For issues:
1. Check debug logs
2. Review error messages
3. Verify server endpoints
4. Check authentication token validity
