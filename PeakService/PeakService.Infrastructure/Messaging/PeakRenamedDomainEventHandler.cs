using Common.Application.Abstractions;
using Common.Contracts.Peaks;
using MassTransit;
using PeakService.Domain.Peaks.Events;

namespace PeakService.Infrastructure.Messaging;

internal sealed class PeakRenamedDomainEventHandler(
    IPublishEndpoint publishEndpoint,
    IDateTimeProvider dateTimeProvider) : IDomainEventHandler<PeakRenamedDomainEvent>
{
    public Task Handle(PeakRenamedDomainEvent domainEvent, CancellationToken cancellationToken) =>
        publishEndpoint.Publish(
            new PeakRenamed
            {
                PeakId = domainEvent.PeakId,
                Name = domainEvent.Name,
                AltitudeM = domainEvent.AltitudeMeters,
                CountryCode = domainEvent.CountryCode,
                OccurredAtUtc = dateTimeProvider.UtcNow
            },
            cancellationToken);
}
