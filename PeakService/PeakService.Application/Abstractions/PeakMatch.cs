using PeakService.Domain.Peaks;

namespace PeakService.Application.Abstractions;

public sealed record PeakMatch(Peak Peak, PeakMatchKind Kind)
{
    public static PeakMatch SameSource(Peak peak) => new(peak, PeakMatchKind.SameSource);

    public static PeakMatch NearDuplicate(Peak peak) => new(peak, PeakMatchKind.NearDuplicate);
}
