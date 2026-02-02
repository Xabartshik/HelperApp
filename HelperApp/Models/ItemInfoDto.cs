namespace HelperApp.Models.Inventory;

/// <summary>
/// DTO для получения информации о товаре по его ID
/// Соответствует ItemDto на сервере
/// </summary>
public class ItemInfoDto
{
    public int ItemId { get; set; }
    public string Name { get; set; } = string.Empty;

    // Дополнительные поля с сервера (на потом)
    public double? Weight { get; set; }
    public double? Length { get; set; }
    public double? Width { get; set; }
    public double? Height { get; set; }
}
