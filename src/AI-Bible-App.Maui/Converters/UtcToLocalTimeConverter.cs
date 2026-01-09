namespace AI_Bible_App.Maui.Converters;

/// <summary>
/// Converts UTC DateTime to local time for display
/// </summary>
public class UtcToLocalTimeConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
    {
        if (value is DateTime utcDateTime)
        {
            // Convert UTC to local time
            var localTime = utcDateTime.Kind == DateTimeKind.Utc 
                ? utcDateTime.ToLocalTime() 
                : DateTime.SpecifyKind(utcDateTime, DateTimeKind.Utc).ToLocalTime();
            
            return localTime;
        }
        return value;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
    {
        if (value is DateTime localDateTime)
        {
            return localDateTime.ToUniversalTime();
        }
        return value;
    }
}
