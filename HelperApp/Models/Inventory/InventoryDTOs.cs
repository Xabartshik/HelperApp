using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace HelperApp.Models.Inventory
{
    /// <summary>
    /// Человекочитаемый номер/адрес складской позиции.
    /// </summary>
    public class PositionCodeDto
    {
        /// <summary>
        /// Идентификатор филиала.
        /// </summary>
        [JsonPropertyName("branchId")]
        public int BranchId { get; set; }

        /// <summary>
        /// Код зоны хранения.
        /// </summary>
        [JsonPropertyName("zoneCode")]
        public string ZoneCode { get; set; }

        /// <summary>
        /// Тип хранилища первого уровня (стеллаж, пол, ячейка и т.п.).
        /// </summary>
        [JsonPropertyName("firstLevelStorageType")]
        public string FirstLevelStorageType { get; set; }

        /// <summary>
        /// Номер хранилища первого уровня.
        /// </summary>
        [JsonPropertyName("fLSNumber")]
        public string FLSNumber { get; set; }

        /// <summary>
        /// Номер хранилища второго уровня (опционально).
        /// </summary>
        [JsonPropertyName("secondLevelStorage")]
        public string? SecondLevelStorage { get; set; }

        /// <summary>
        /// Номер хранилища третьего уровня (опционально).
        /// </summary>
        [JsonPropertyName("thirdLevelStorage")]
        public string? ThirdLevelStorage { get; set; }
    }

    /// <summary>
    /// Строка инвентаризации с информацией о товаре
    /// </summary>
    public class InventoryAssignmentLineWithItemDto
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("itemPositionId")]
        public int ItemPositionId { get; set; }

        [JsonPropertyName("positionId")]
        public int PositionId { get; set; }

        [JsonPropertyName("expectedQuantity")]
        public int ExpectedQuantity { get; set; }

        [JsonPropertyName("actualQuantity")]
        public int? ActualQuantity { get; set; }

        [JsonPropertyName("itemId")]
        public int ItemId { get; set; }

        [JsonPropertyName("itemName")]
        public string ItemName { get; set; }

        [JsonPropertyName("displayName")]
        public string DisplayName { get; set; }

        [JsonPropertyName("positionCode")]
        public PositionCodeDto PositionCode { get; set; }

    }

    /// <summary>
    /// Назначение инвентаризации с информацией о товарах
    /// </summary>
    public class InventoryAssignmentDetailedWithItemDto
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("taskNumber")]
        public string TaskNumber { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonPropertyName("createdDate")]
        public string CreatedDate { get; set; }

        [JsonPropertyName("lines")]
        public List<InventoryAssignmentLineWithItemDto> Lines { get; set; } = new();
    }


    [System.Obsolete("Используйте InventoryAssignmentDetailedWithItemDto вместо этого")]
    public class InventoryAssignmentLineDto
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("itemPositionId")]
        public int ItemPositionId { get; set; }

        [JsonPropertyName("expectedQuantity")]
        public int ExpectedQuantity { get; set; }

        [JsonPropertyName("actualQuantity")]
        public int? ActualQuantity { get; set; }

        [JsonPropertyName("zoneCode")]
        public string ZoneCode { get; set; }

        [JsonPropertyName("firstLevelStorageType")]
        public string FirstLevelStorageType { get; set; }

        [JsonPropertyName("flsNumber")]
        public int FlsNumber { get; set; }
    }

    [System.Obsolete("Используйте InventoryAssignmentDetailedWithItemDto вместо этого")]
    public class InventoryAssignmentDetailedDto
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("taskNumber")]
        public string TaskNumber { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonPropertyName("createdDate")]
        public string CreatedDate { get; set; }

        [JsonPropertyName("lines")]
        public List<InventoryAssignmentLineDto> Lines { get; set; } = new();
    }
}
