using Common.Domain.Abstractions;

namespace PeakService.Domain.Peaks.Events;

public sealed record PeakCreatedDomainEvent(
    Guid PeakId,
    string Name,
    int AltitudeMeters,
    string? CountryCode) : IDomainEvent;
