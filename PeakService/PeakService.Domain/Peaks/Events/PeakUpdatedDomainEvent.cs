using Common.Domain.Abstractions;

namespace PeakService.Domain.Peaks.Events;

public sealed record PeakUpdatedDomainEvent(
    Guid PeakId,
    string Name,
    int AltitudeMeters,
    string? CountryCode) : IDomainEvent;
