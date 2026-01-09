using System.Text.RegularExpressions;
using AI_Bible_App.Maui.Services;

namespace AI_Bible_App.Maui.Controls;

/// <summary>
/// A Label that automatically converts Bible references to clickable links
/// that display the passage in-app with optional AI summary
/// </summary>
public class BibleLinkLabel : Label
{
    // Bible reference pattern: matches "Book Chapter:Verse" or "Book Chapter:Verse-Verse"
    // Examples: John 3:16, 1 Corinthians 13:4-7, Psalm 23:1-6, Genesis 1:1
    private static readonly Regex BibleRefRegex = new Regex(
        @"\b((?:1|2|3|I|II|III)?\s*[A-Z][a-z]+(?:\s+[A-Z][a-z]+)?)\s+(\d{1,3}):(\d{1,3})(?:-(\d{1,3}))?\b",
        RegexOptions.Compiled);

    public static readonly BindableProperty LinkedTextProperty =
        BindableProperty.Create(
            nameof(LinkedText),
            typeof(string),
            typeof(BibleLinkLabel),
            string.Empty,
            propertyChanged: OnLinkedTextChanged);

    public string LinkedText
    {
        get => (string)GetValue(LinkedTextProperty);
        set => SetValue(LinkedTextProperty, value);
    }

    private static void OnLinkedTextChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is BibleLinkLabel label && newValue is string text)
        {
            label.UpdateFormattedText(text);
        }
    }

    private void UpdateFormattedText(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            FormattedText = null;
            return;
        }

        var formattedString = new FormattedString();
        int lastIndex = 0;

        foreach (Match match in BibleRefRegex.Matches(text))
        {
            // Add text before the match
            if (match.Index > lastIndex)
            {
                formattedString.Spans.Add(new Span
                {
                    Text = text.Substring(lastIndex, match.Index - lastIndex)
                });
            }

            // Add the Bible reference as a clickable link
            var reference = match.Value;
            var span = new Span
            {
                Text = reference,
                TextColor = Application.Current?.RequestedTheme == AppTheme.Dark 
                    ? Colors.LightBlue 
                    : Colors.Blue,
                TextDecorations = TextDecorations.Underline
            };

            // Create tap gesture for in-app display
            var tapGesture = new TapGestureRecognizer();
            var book = match.Groups[1].Value.Trim();
            var chapter = int.Parse(match.Groups[2].Value);
            var verseStart = int.Parse(match.Groups[3].Value);
            var verseEnd = match.Groups[4].Success ? int.Parse(match.Groups[4].Value) : (int?)null;

            tapGesture.Tapped += async (s, e) =>
            {
                await ShowPassagePopupAsync(book, chapter, verseStart, verseEnd, reference);
            };
            span.GestureRecognizers.Add(tapGesture);

            formattedString.Spans.Add(span);
            lastIndex = match.Index + match.Length;
        }

        // Add remaining text after last match
        if (lastIndex < text.Length)
        {
            formattedString.Spans.Add(new Span
            {
                Text = text.Substring(lastIndex)
            });
        }

        FormattedText = formattedString;
    }

    private async Task ShowPassagePopupAsync(string book, int chapter, int verseStart, int? verseEnd, string reference)
    {
        try
        {
            var lookupService = Application.Current?.Handler?.MauiContext?.Services.GetService<IBibleLookupService>();
            
            if (lookupService == null)
            {
                System.Diagnostics.Debug.WriteLine("[DEBUG] BibleLinkLabel: lookupService is null, falling back to browser");
                // Fallback to browser if service not available
                await OpenInBrowserAsync(book, chapter, verseStart, verseEnd);
                return;
            }

            System.Diagnostics.Debug.WriteLine($"[DEBUG] BibleLinkLabel: Looking up {book} {chapter}:{verseStart}");
            var result = await lookupService.LookupPassageAsync(book, chapter, verseStart, verseEnd);
            System.Diagnostics.Debug.WriteLine($"[DEBUG] BibleLinkLabel: Lookup result Found={result.Found}, Text length={result.Text?.Length ?? 0}");

            if (result.Found && Shell.Current?.CurrentPage != null)
            {
                // Show verse directly in-app (user preference)
                await Shell.Current.CurrentPage.DisplayAlert(
                    $"📖 {result.Reference} ({result.Translation})",
                    result.Text,
                    "Close");
            }
            else
            {
                // Passage not found locally - offer to open in browser
                System.Diagnostics.Debug.WriteLine($"[DEBUG] BibleLinkLabel: Passage not found locally for '{reference}'");
                if (Shell.Current?.CurrentPage != null)
                {
                    var openBrowser = await Shell.Current.CurrentPage.DisplayAlert(
                        "Passage Not Found",
                        $"'{reference}' wasn't found in local Bible data.\n\nWould you like to view it on BibleGateway?",
                        "Open Browser",
                        "Cancel");

                    if (openBrowser)
                    {
                        await OpenInBrowserAsync(book, chapter, verseStart, verseEnd);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[DEBUG] BibleLinkLabel: Error showing passage: {ex.Message}");
            // Fallback to browser
            await OpenInBrowserAsync(book, chapter, verseStart, verseEnd);
        }
    }

    private static async Task OpenInBrowserAsync(string book, int chapter, int verseStart, int? verseEnd)
    {
        try
        {
            var url = GetBibleGatewayUrl(book, chapter, verseStart, verseEnd);
            await Launcher.OpenAsync(new Uri(url));
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to open Bible link: {ex.Message}");
        }
    }

    private static string GetBibleGatewayUrl(string book, int chapter, int verseStart, int? verseEnd)
    {
        // Normalize book name for URL
        var normalizedBook = book
            .Replace("1 ", "1+")
            .Replace("2 ", "2+")
            .Replace("3 ", "3+")
            .Replace("I ", "1+")
            .Replace("II ", "2+")
            .Replace("III ", "3+")
            .Replace(" ", "+");
        
        var searchTerm = $"{normalizedBook}+{chapter}:{verseStart}";
        if (verseEnd.HasValue && verseEnd != verseStart)
        {
            searchTerm += $"-{verseEnd}";
        }

        return $"https://www.biblegateway.com/passage/?search={Uri.EscapeDataString(searchTerm)}&version=WEB";
    }
}
