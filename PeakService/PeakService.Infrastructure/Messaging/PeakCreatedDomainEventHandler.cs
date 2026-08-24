using Common.Application.Abstractions;
using Common.Contracts.Peaks;
using MassTransit;
using PeakService.Domain.Peaks.Events;

namespace PeakService.Infrastructure.Messaging;

internal sealed class PeakCreatedDomainEventHandler(IPublishEndpoint publishEndpoint)
    : IDomainEventHandler<PeakCreatedDomainEvent>
{
    public Task Handle(
        PeakCreatedDomainEvent domainEvent,
        DomainEventContext context,
        CancellationToken cancellationToken) =>
        publishEndpoint.Publish(
            new PeakCreated
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
