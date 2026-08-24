using System.Globalization;

namespace PeakService.Domain.Peaks;

internal static class PeakText
{
    public static string? Normalize(string? value) => value?.Trim();

    public static bool IsPrintable(string? value) => value is null || !value.Any(IsForbiddenCharacter);

    private static bool IsForbiddenCharacter(char value) =>
        char.GetUnicodeCategory(value) is UnicodeCategory.Control
            or UnicodeCategory.LineSeparator
            or UnicodeCategory.ParagraphSeparator;
}
