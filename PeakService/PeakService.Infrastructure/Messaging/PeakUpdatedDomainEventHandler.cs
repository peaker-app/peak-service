using Common.Application.Abstractions;
using Common.Contracts.Peaks;
using MassTransit;
using PeakService.Domain.Peaks.Events;

namespace PeakService.Infrastructure.Messaging;

internal sealed class PeakUpdatedDomainEventHandler(
    IPublishEndpoint publishEndpoint,
    IDateTimeProvider dateTimeProvider) : IDomainEventHandler<PeakUpdatedDomainEvent>
{
    public Task Handle(PeakUpdatedDomainEvent domainEvent, CancellationToken cancellationToken) =>
        publishEndpoint.Publish(
            new PeakUpdated
            {
                PeakId = domainEvent.PeakId,
                Name = domainEvent.Name,
                AltitudeM = domainEvent.AltitudeMeters,
                CountryCode = domainEvent.CountryCode,
                OccurredAtUtc = dateTimeProvider.UtcNow
            },
            cancellationToken);
}
