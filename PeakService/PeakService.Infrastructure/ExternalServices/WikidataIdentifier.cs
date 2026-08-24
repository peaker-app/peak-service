using System.Text.RegularExpressions;

namespace PeakService.Infrastructure.ExternalServices;

internal static partial class WikidataIdentifier
{
    public static bool IsIdentifier(string value) => Pattern().IsMatch(value);

    [GeneratedRegex("^Q[1-9][0-9]*$")]
    private static partial Regex Pattern();
}
