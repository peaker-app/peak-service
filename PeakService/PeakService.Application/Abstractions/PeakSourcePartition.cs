namespace PeakService.Application.Abstractions;

public sealed record PeakSourcePartition(IReadOnlyList<PeakSourceRecord> Records, string? FailureReason)
{
    public bool IsFailed => FailureReason is not null;

    public static PeakSourcePartition Loaded(IReadOnlyList<PeakSourceRecord> records) => new(records, FailureReason: null);

    public static PeakSourcePartition Failed(string reason) => new([], reason);
}
