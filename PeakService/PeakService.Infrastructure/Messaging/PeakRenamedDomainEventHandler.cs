using Common.Application.Abstractions;
using Common.Contracts.Peaks;
using MassTransit;
using PeakService.Domain.Peaks.Events;

namespace PeakService.Infrastructure.Messaging;

internal sealed class PeakRenamedDomainEventHandler(IPublishEndpoint publishEndpoint)
    : IDomainEventHandler<PeakRenamedDomainEvent>
{
    public Task Handle(
        PeakRenamedDomainEvent domainEvent,
        DomainEventContext context,
        CancellationToken cancellationToken) =>
        publishEndpoint.Publish(
            new PeakRenamed
            {
                MessageId = context.MessageId,
                OccurredAtUtc = context.OccurredAtUtc,
                PeakId = domainEvent.PeakId,
                Name = domainEvent.Name,
                AltitudeM = domainEvent.AltitudeMeters,
                CountryCode = domainEvent.CountryCode
            },
            cancellationToken);
}
