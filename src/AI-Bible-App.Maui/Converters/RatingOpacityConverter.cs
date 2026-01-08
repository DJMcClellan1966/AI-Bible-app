using System.Globalization;

namespace AI_Bible_App.Maui.Converters;

/// <summary>
/// Converts a rating value to opacity for visual feedback on rating buttons.
/// Returns 1.0 if the rating matches the parameter (selected), 0.4 if not (unselected).
/// </summary>
public class RatingOpacityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is int rating && parameter is string paramStr && int.TryParse(paramStr, out int targetRating))
        {
            // Return full opacity if rating matches, otherwise dimmed
            return rating == targetRating ? 1.0 : 0.4;
        }
        return 0.4; // Default to dimmed
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
