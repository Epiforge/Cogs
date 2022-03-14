namespace Cogs.Wpf.Controls;

/// <summary>
/// Provides a lightweight control for displaying small amounts of flow content which finds URLs and makes them clickable hyperlinks
/// </summary>
public class UrlAwareTextBlock : TextBlock
{
    /// <summary>
    /// Gets or sets the text that is parsed for URLs
    /// </summary>
    public string? ParsedText
    {
        get => (string?)GetValue(ParsedTextProperty);
        set => SetValue(ParsedTextProperty, value);
    }

    static UrlAwareTextBlock() =>
        TextProperty.OverrideMetadata(typeof(UrlAwareTextBlock), new FrameworkPropertyMetadata((sender, e) => sender.SetCurrentValue(ParsedTextProperty, e.NewValue)));

    /// <summary>
    /// Identifies the <see cref="ParsedText"/> dependency property
    /// </summary>
    public static readonly DependencyProperty ParsedTextProperty = DependencyProperty.Register(nameof(ParsedText), typeof(string), typeof(UrlAwareTextBlock), new PropertyMetadata() { CoerceValueCallback = ParsedTextCoerceValue });
    static readonly Regex textAndUrlPattern = new(@"((?<text>.*?)(?<url>(?:(?:https?|ftp):\/\/)(?:\S+(?::\S*)?@)?(?:(?!10(?:\.\d{1,3}){3})(?!127(?:\.\d{1,3}){3})(?!169\.254(?:\.\d{1,3}){2})(?!192\.168(?:\.\d{1,3}){2})(?!172\.(?:1[6-9]|2\d|3[0-1])(?:\.\d{1,3}){2})(?:[1-9]\d?|1\d\d|2[01]\d|22[0-3])(?:\.(?:1?\d{1,2}|2[0-4]\d|25[0-5])){2}(?:\.(?:[1-9]\d?|1\d\d|2[0-4]\d|25[0-4]))|(?:(?:[a-z\u00a1-\uffff0-9]+-?)*[a-z\u00a1-\uffff0-9]+)(?:\.(?:[a-z\u00a1-\uffff0-9]+-?)*[a-z\u00a1-\uffff0-9]+)*(?:\.(?:[a-z\u00a1-\uffff]{2,})))(?::\d{2,5})?(?:\/[^\s]*)?)|(?<remainder>.*))", RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    static void HyperlinkRequestNavigateHandler(object sender, RequestNavigateEventArgs e)
    {
        Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { Verb = "open", UseShellExecute = true });
        e.Handled = true;
    }

    static object ParsedTextCoerceValue(DependencyObject sender, object value)
    {
        if (sender is UrlAwareTextBlock textBlock && value is string strValue)
        {
            var inlines = textAndUrlPattern.Matches(strValue).Cast<Match>().SelectMany(TextAndUrlPatternMatchesSelector).ToImmutableArray();
            textBlock.Inlines.Clear();
            textBlock.Inlines.AddRange(inlines);
        }
        return value;
    }

    static IEnumerable<Inline> TextAndUrlPatternMatchesSelector(Match match)
    {
        if (match.Groups["remainder"] is Group remainderGroup && remainderGroup.Success)
            yield return new Run(remainderGroup.Value);
        else
        {
            if (match.Groups["text"] is Group textGroup && textGroup.Success)
                yield return new Run(textGroup.Value);
            if (match.Groups["url"] is Group urlGroup && urlGroup.Success)
            {
                var hyperlink = new Hyperlink(new Run(urlGroup.Value)) { NavigateUri = new Uri(urlGroup.Value) };
                hyperlink.RequestNavigate += HyperlinkRequestNavigateHandler;
                yield return hyperlink;
            }
        }
    }
}
