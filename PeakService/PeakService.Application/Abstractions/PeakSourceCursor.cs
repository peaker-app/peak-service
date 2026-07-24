namespace PeakService.Application.Abstractions;

public sealed record PeakSourceCursor(DateTime? ModifiedSinceUtc)
{
    public static PeakSourceCursor Full { get; } = new(default(DateTime?));

    public bool IsIncremental => ModifiedSinceUtc is not null;

    public static PeakSourceCursor Since(DateTime modifiedSinceUtc) => new(modifiedSinceUtc);
}
