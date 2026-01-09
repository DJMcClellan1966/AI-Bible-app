namespace AI_Bible_App.Maui.Converters;

/// <summary>
/// Converts IsListening bool to microphone icon
/// </summary>
public class BoolToMicIconConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
    {
        if (value is bool isListening)
        {
            return isListening ? "⏹" : "🎤";
        }
        return "🎤";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts IsListening bool to button color
/// </summary>
public class BoolToMicColorConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
    {
        if (value is bool isListening)
        {
            return isListening ? Colors.Red : Color.FromArgb("#512BD4"); // Primary color when not listening
        }
        return Color.FromArgb("#512BD4");
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts IsListening bool to placeholder text
/// </summary>
public class BoolToPlaceholderConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
    {
        if (value is bool isListening)
        {
            return isListening ? "Listening... tap 🎤 to stop" : "Type or tap 🎤 to speak...";
        }
        return "Type or tap 🎤 to speak...";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
