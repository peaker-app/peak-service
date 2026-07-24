using PeakService.Domain.Peaks;

namespace PeakService.Application.UnitTests.TestData;

internal static class PeakFactory
{
    public static Peak Create(Guid? rangeId = null, IReadOnlyList<PeakNameDraft>? alternativeNames = null) =>
        Peak.Create(new PeakDraft(
            new PeakSourceData(
                "Q192580",
                "Aneto",
                3404,
                2812,
                Coordinates.Create(42.6316, 0.6577).Value,
                "ES",
                "Huesca",
                SourceRevision: null),
            rangeId,
            alternativeNames ?? [])).Value;
}
