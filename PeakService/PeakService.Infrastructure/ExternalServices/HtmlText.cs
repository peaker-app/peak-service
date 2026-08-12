using System.Net;
using System.Text.RegularExpressions;

namespace PeakService.Infrastructure.ExternalServices;

internal static partial class HtmlText
{
    public static string? ToPlainText(string? html, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(html))
        {
            return null;
        }

        string text = WebUtility.HtmlDecode(TagPattern().Replace(html, " "));
        string collapsed = WhitespacePattern().Replace(text, " ").Trim();

        if (collapsed.Length == 0)
        {
            return null;
        }

        return collapsed.Length <= maxLength ? collapsed : collapsed[..maxLength].TrimEnd();
    }

    [GeneratedRegex("<[^>]*>", RegexOptions.None, matchTimeoutMilliseconds: 1000)]
    private static partial Regex TagPattern();

    [GeneratedRegex(@"\s+", RegexOptions.None, matchTimeoutMilliseconds: 1000)]
    private static partial Regex WhitespacePattern();
}
