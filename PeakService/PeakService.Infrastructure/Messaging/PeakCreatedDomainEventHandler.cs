using Common.Application.Abstractions;
using Common.Contracts.Peaks;
using MassTransit;
using PeakService.Domain.Peaks.Events;

namespace PeakService.Infrastructure.Messaging;

internal sealed class PeakCreatedDomainEventHandler(
    IPublishEndpoint publishEndpoint,
    IDateTimeProvider dateTimeProvider) : IDomainEventHandler<PeakCreatedDomainEvent>
{
    public Task Handle(PeakCreatedDomainEvent domainEvent, CancellationToken cancellationToken) =>
        publishEndpoint.Publish(
            new PeakCreated
            {
                PeakId = domainEvent.PeakId,
                Name = domainEvent.Name,
                AltitudeM = domainEvent.AltitudeMeters,
                CountryCode = domainEvent.CountryCode,
                OccurredAtUtc = dateTimeProvider.UtcNow
            },
            cancellationToken);
}
