using System.Collections.Generic;

namespace HelperApp.Models.Inventory
{
    /// <summary>
    /// DTO для завершения задания инвентаризации
    /// </summary>
    public class CompleteAssignmentDto
    {
        public int AssignmentId { get; set; }
        public int WorkerId { get; set; }
        public List<CompleteAssignmentLineDto> Lines { get; set; } = new();
    }

    /// <summary>
    /// DTO для обновления строки назначения
    /// </summary>
    public class CompleteAssignmentLineDto
    {
        /// <summary>
        /// ID существующей линии (null для неожиданных товаров)
        /// </summary>
        public int? LineId { get; set; }

        /// <summary>
        /// ID товара (обязательно для новых товаров)
        /// </summary>
        public int ItemId { get; set; }

        /// <summary>
        /// Код позиции, где обнаружен товар
        /// </summary>
        public string PositionCode { get; set; } = string.Empty;

        /// <summary>
        /// Фактическое количество (может быть 0 для отсутствующих)
        /// </summary>
        public int? ActualQuantity { get; set; }
    }

    /// <summary>
    /// Результат завершения инвентаризации
    /// </summary>
    public class CompleteAssignmentResultDto
    {
        /// <summary>
        /// ID назначения
        /// </summary>
        public int AssignmentId { get; set; }  // ← Это поле должно быть

        /// <summary>
        /// Сообщение о результате
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Статистика после завершения
        /// </summary>
        public InventoryStatisticsDto? Statistics { get; set; }

        /// <summary>
        /// Отчёт о расхождениях
        /// </summary>
        public DiscrepancyReportDto? DiscrepancyReport { get; set; }

        /// <summary>
        /// Время завершения
        /// </summary>
        public DateTime CompletedAt { get; set; }
    }

    /// <summary>
    /// Статистика инвентаризации
    /// </summary>
    public class InventoryStatisticsDto
    {
        public int Id { get; set; }
        public int InventoryAssignmentId { get; set; }
        public int TotalPositions { get; set; }
        public int CountedPositions { get; set; }
        public decimal CompletionPercentage { get; set; }
        public int DiscrepancyCount { get; set; }
        public int SurplusCount { get; set; }
        public int ShortageCount { get; set; }
        public int TotalSurplusQuantity { get; set; }
        public int TotalShortageQuantity { get; set; }
        public DateTime StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
    }

    /// <summary>
    /// Отчет о расхождениях
    /// </summary>
    public class DiscrepancyReportDto
    {
        public int InventoryAssignmentId { get; set; }
        public int TotalDiscrepancies { get; set; }
        public int SurplusCount { get; set; }
        public int ShortageCount { get; set; }
        public decimal DiscrepancyPercentage { get; set; }
        public List<DiscrepancyDto> Discrepancies { get; set; } = new();
    }

    /// <summary>
    /// Расхождение
    /// </summary>
    public class DiscrepancyDto
    {
        public int Id { get; set; }
        public int InventoryAssignmentLineId { get; set; }
        public int ItemPositionId { get; set; }
        public int ExpectedQuantity { get; set; }
        public int ActualQuantity { get; set; }
        public int Variance { get; set; }
        public int Type { get; set; } // 0 = Surplus, 1 = Shortage
        public string? Note { get; set; }
        public DateTime IdentifiedAt { get; set; }
        public int ResolutionStatus { get; set; }
    }
}
