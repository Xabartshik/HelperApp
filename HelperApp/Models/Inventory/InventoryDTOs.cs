using HelperApp.Models.Tasks;
using System.Text.Json.Serialization;

namespace HelperApp.Models.Inventory;

/// <summary>
/// Статус назначения инвентаризации
/// Соответствует inventoryassignments.status
/// </summary>
public enum InventoryAssignmentStatus
{
    Assigned = 0,
    InProgress = 1,
    Completed = 2,
    Cancelled = 3
}

/// <summary>
/// DTO для строки назначения инвентаризации (товар для учёта)
/// Соответствует серверному InventoryAssignmentLineDto
/// </summary>
public sealed class InventoryAssignmentLineDto
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("inventoryAssignmentId")]
    public int InventoryAssignmentId { get; set; }

    [JsonPropertyName("itemPositionId")]
    public int ItemPositionId { get; set; }

    [JsonPropertyName("expectedQuantity")]
    public int ExpectedQuantity { get; set; }

    [JsonPropertyName("actualQuantity")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? ActualQuantity { get; set; }

    [JsonPropertyName("variance")]
    public int Variance => ActualQuantity.HasValue ? ActualQuantity.Value - ExpectedQuantity : 0;

    [JsonPropertyName("zoneCode")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ZoneCode { get; set; }

    [JsonPropertyName("firstLevelStorageType")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? FirstLevelStorageType { get; set; }

    [JsonPropertyName("flsNumber")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? FlsNumber { get; set; }
}

/// <summary>
/// DTO для детальной информации о назначении инвентаризации
/// Соответствует серверному InventoryAssignmentDetailedDto
/// </summary>
public sealed class InventoryAssignmentDetailedDto
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("taskId")]
    public int TaskId { get; set; }

    [JsonPropertyName("assignedToUserId")]
    public int AssignedToUserId { get; set; }

    [JsonPropertyName("userName")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? UserName { get; set; }

    [JsonPropertyName("branchId")]
    public int BranchId { get; set; }

    [JsonPropertyName("zoneCode")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ZoneCode { get; set; }

    [JsonPropertyName("status")]
    public InventoryAssignmentStatus Status { get; set; }

    [JsonPropertyName("assignedAt")]
    public DateTime AssignedAt { get; set; }

    [JsonPropertyName("completedAt")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public DateTime? CompletedAt { get; set; }

    [JsonPropertyName("statistics")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public InventoryStatisticsDto? Statistics { get; set; }

    [JsonPropertyName("lines")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<InventoryAssignmentLineDto> Lines { get; set; } = new();
}

/// <summary>
/// DTO для статистики инвентаризации
/// </summary>
public sealed class InventoryStatisticsDto
{
    [JsonPropertyName("totalPositions")]
    public int TotalPositions { get; set; }

    [JsonPropertyName("completedPositions")]
    public int CompletedPositions { get; set; }

    [JsonPropertyName("variances")]
    public int Variances { get; set; }
}
