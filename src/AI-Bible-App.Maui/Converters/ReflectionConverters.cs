using AI_Bible_App.Core.Models;

namespace AI_Bible_App.Maui.Converters;

/// <summary>
/// Converts ReflectionType enum to emoji
/// </summary>
public class ReflectionTypeToEmojiConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
    {
        if (value is ReflectionType type)
        {
            return type switch
            {
                ReflectionType.Chat => "💬",
                ReflectionType.Prayer => "🙏",
                ReflectionType.BibleVerse => "📖",
                ReflectionType.Custom => "✏️",
                _ => "📝"
            };
        }
        return "📝";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts boolean to star emoji for favorites
/// </summary>
public class BoolToStarConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
    {
        if (value is bool isFavorite)
        {
            return isFavorite ? "⭐" : "☆";
        }
        return "☆";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts string to bool (true if not empty)
/// </summary>
public class StringToBoolConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
    {
        return !string.IsNullOrEmpty(value as string);
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts int to bool (true if > 0, or inverted if parameter is "invert")
/// </summary>
public class IntToBoolConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
    {
        var result = value is int count && count > 0;
        
        if (parameter?.ToString() == "invert")
        {
            return !result;
        }
        
        return result;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
