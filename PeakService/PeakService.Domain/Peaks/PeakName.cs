namespace PeakService.Domain.Peaks;

public sealed class PeakName
{
    public const int MaxLanguageCodeLength = 12;
    public const int MaxNameLength = 200;

    private PeakName()
    {
    }

    private PeakName(Guid id, string languageCode, string name, bool isOfficial)
    {
        Id = id;
        LanguageCode = languageCode;
        Name = name;
        IsOfficial = isOfficial;
    }

    public Guid Id { get; private set; }

    public string LanguageCode { get; private set; } = null!;

    public string Name { get; private set; } = null!;

    public bool IsOfficial { get; private set; }

    internal static PeakName Create(string languageCode, string name, bool isOfficial) =>
        new(Guid.CreateVersion7(), languageCode.Trim(), name.Trim(), isOfficial);

    internal static bool IsAcceptable(PeakNameDraft draft) =>
        IsWithin(draft.LanguageCode, MaxLanguageCodeLength) && IsWithin(draft.Name, MaxNameLength);

    private static bool IsWithin(string value, int maxLength)
    {
        string? trimmed = PeakText.Normalize(value);

        return !string.IsNullOrEmpty(trimmed) && trimmed.Length <= maxLength && PeakText.IsPrintable(trimmed);
    }
}
