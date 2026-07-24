using Common.Application.Messaging;

namespace PeakService.Application.Peaks.GetPeakById;

public sealed record GetPeakByIdQuery(Guid PeakId) : IQuery<PeakDetailResponse>;
