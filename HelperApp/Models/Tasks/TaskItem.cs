using System;
using System.Collections.Generic;
using System.Linq;

namespace HelperApp.Models.Tasks
{
    /// <summary>
    /// Человекочитаемый номер/адрес складской позиции
    /// </summary>
    public class PositionCodeInfo
    {
        /// <summary>
        /// Идентификатор филиала.
        /// </summary>
        public int BranchId { get; set; }

        /// <summary>
        /// Код зоны хранения.
        /// </summary>
        public string ZoneCode { get; set; }

        /// <summary>
        /// Тип хранилища первого уровня (стеллаж, пол, ячейка и т.п.).
        /// </summary>
        public string FirstLevelStorageType { get; set; }

        /// <summary>
        /// Номер хранилища первого уровня.
        /// </summary>
        public string FLSNumber { get; set; }

        /// <summary>
        /// Номер хранилища второго уровня (опционально).
        /// </summary>
        public string? SecondLevelStorage { get; set; }

        /// <summary>
        /// Номер хранилища третьего уровня (опционально).
        /// </summary>
        public string? ThirdLevelStorage { get; set; }

        /// <summary>
        /// Человекочитаемое представление позиции.
        /// Пример: "1-ZA-RACK-A1-S1-C3"
        /// </summary>
        public string FullDescription =>
            $"{BranchId}-{ZoneCode}-{FirstLevelStorageType}-{FLSNumber}" +
            (!string.IsNullOrEmpty(SecondLevelStorage) ? $"-{SecondLevelStorage}" : string.Empty) +
            (!string.IsNullOrEmpty(ThirdLevelStorage) ? $"-{ThirdLevelStorage}" : string.Empty);

        /// <summary>
        /// Краткое описание (для отображения в UI).
        /// Пример: "ZA-RACK-A1"
        /// </summary>
        public string ShortDescription =>
            $"{ZoneCode}-{FirstLevelStorageType}-{FLSNumber}";
    }

    /// <summary>
    /// Строка инвентаризации
    /// </summary>
    public class InventoryLineItem
    {
        public int Id { get; set; }
        public int ItemPositionId { get; set; }
        public int PositionId { get; set; }
        public int ExpectedQuantity { get; set; }
        public int? ActualQuantity { get; set; }
        public int ItemId { get; set; }
        public string ItemName { get; set; }

        /// <summary>
        /// Отображаемое название товара.
        /// Если нет названия, использует ItemId.
        /// </summary>
        public string DisplayName => string.IsNullOrEmpty(ItemName)
            ? $"Item {ItemId}"
            : ItemName;

        // Структурированная информация о расположении
        public PositionCodeInfo PositionCode { get; set; }
    }

    /// <summary>
    /// Задача инвентаризации
    /// </summary>
    public class InventoryTaskItem
    {
        public int Id { get; set; }
        public string TaskNumber { get; set; }
        public string Description { get; set; }
        public DateTime CreatedDate { get; set; }
        public List<InventoryLineItem> Lines { get; set; } = new();

        /// <summary>
        /// Количество обработанных (отсканированных) позиций
        /// </summary>
        public int ProcessedCount => Lines.Count(l => l.ActualQuantity.HasValue);

        /// <summary>
        /// Количество позиций с расхождениями (ожидаемое != фактическое)
        /// </summary>
        public int DiscrepancyCount => Lines.Count(l =>
            l.ActualQuantity.HasValue && l.ActualQuantity != l.ExpectedQuantity);

        /// <summary>
        /// Процент завершения инвентаризации
        /// </summary>
        public double CompletionPercentage
        {
            get
            {
                if (Lines.Count == 0) return 0;
                return (ProcessedCount / (double)Lines.Count) * 100;
            }
        }

        /// <summary>
        /// Статус задачи (Not Started / In Progress / Completed)
        /// </summary>
        public string Status
        {
            get
            {
                if (ProcessedCount == 0) return "Not Started";
                if (ProcessedCount == Lines.Count) return "Completed";
                return "In Progress";
            }
        }

        /// <summary>
        /// Краткое описание для отображения
        /// </summary>
        public string ShortDescription =>
            $"{TaskNumber}: {Description} ({ProcessedCount}/{Lines.Count})";

        /// <summary>
        /// Детальное описание для отображения
        /// </summary>
        public string DetailedDescription =>
            $"{TaskNumber}: {Description}\n" +
            $"Обработано: {ProcessedCount}/{Lines.Count} ({CompletionPercentage:F1}%)\n" +
            $"Расхождений: {DiscrepancyCount}\n" +
            $"Статус: {Status}";
    }
}
