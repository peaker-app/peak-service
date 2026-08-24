namespace PeakService.Domain.Peaks;

public sealed record PeakImageAttribution(
    string? Author,
    string? License,
    string? LicenseUrl,
    string? CreditUrl)
{
    public static readonly PeakImageAttribution None = new(null, null, null, null);

    public bool IsComplete => !string.IsNullOrWhiteSpace(Author) && !string.IsNullOrWhiteSpace(License);

    internal PeakImageAttribution Normalized() =>
        new(PeakText.Normalize(Author), PeakText.Normalize(License), LicenseUrl, CreditUrl);

    internal bool IsWithinLimits() =>
        Author is not { Length: > Peak.MaxImageAuthorLength }
        && License is not { Length: > Peak.MaxImageLicenseLength }
        && LicenseUrl is not { Length: > Peak.MaxImageUrlLength }
        && CreditUrl is not { Length: > Peak.MaxImageUrlLength };
}
