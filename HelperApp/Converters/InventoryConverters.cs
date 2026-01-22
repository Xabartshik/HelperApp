using System.Globalization;

namespace HelperApp.Converters
{
    /// <summary>
    /// Конвертер для определения цвета фактического количества.
    /// Если не указано (null) → жёлтый, если указано → зелёный.
    /// </summary>
    public class ActualQuantityColorConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is int quantity)
                return Color.FromArgb("#38bdf8");  // Голубой - введено

            return Color.FromArgb("#909090");  // Серый - не введено
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    /// <summary>
    /// Конвертер для определения цвета расхождения.
    /// 0 → зелёный (совпадает)
    /// > 0 → оранжевый (излишек)
    /// &lt; 0 → красный (недостаток)
    /// </summary>
    public class VarianceColorConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is int variance)
            {
                if (variance == 0)
                    return Color.FromArgb("#22c55e");  // Зелёный - совпадает

                if (variance > 0)
                    return Color.FromArgb("#f59e0b");  // Оранжевый - излишек

                return Color.FromArgb("#ef4444");  // Красный - недостаток
            }

            return Color.FromArgb("#909090");  // Серый - неизвестно
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    /// <summary>
    /// Конвертер для определения прогресса выполнения позиции.
    /// Возвращает 0-1 для ProgressBar.
    /// </summary>
    public class CompletionProgressConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return value is int q && q > 0 ? 1.0 : 0.0;
        }


        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    /// <summary>
    /// Конвертер для форматирования текста о расхождении.
    /// Если ActualQuantity не указано → "🟡 Не отсчитано"
    /// Если совпадает → "✅ Совпадает"
    /// Если излишек → "⬆️ Излишек: +X"
    /// Если недостаток → "⬇️ Недостаток: -X"
    /// </summary>
    public class VarianceTextConverter : IMultiValueConverter
    {
        public object Convert(object?[] values, Type targetType, object? parameter, CultureInfo culture)
        {
            if (values.Length < 2)
                return "?";

            var actualQuantity = values[0] as int?;
            var expectedQuantity = values[1] as int? ?? 0;

            if (!actualQuantity.HasValue)
                return "🟡 Не отсчитано";

            var variance = actualQuantity.Value - expectedQuantity;

            if (variance == 0)
                return "✅ Совпадает";

            if (variance > 0)
                return $"⬆️ Излишек: +{variance}";

            return $"⬇️ Недостаток: {variance}";
        }

        public object[] ConvertBack(object? value, Type[] targetTypes, object? parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
