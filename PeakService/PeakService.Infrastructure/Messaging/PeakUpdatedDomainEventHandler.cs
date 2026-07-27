using Common.Application.Abstractions;
using Common.Contracts.Peaks;
using MassTransit;
using PeakService.Domain.Peaks.Events;

namespace PeakService.Infrastructure.Messaging;

internal sealed class PeakUpdatedDomainEventHandler(IPublishEndpoint publishEndpoint)
    : IDomainEventHandler<PeakUpdatedDomainEvent>
{
    public Task Handle(
        PeakUpdatedDomainEvent domainEvent,
        DomainEventContext context,
        CancellationToken cancellationToken) =>
        publishEndpoint.Publish(
            new PeakUpdated
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
