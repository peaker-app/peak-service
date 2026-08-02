using Common.Domain.Results;
using PeakService.Application.Abstractions;
using PeakService.Domain.PeakIngestionRuns;
using PeakService.Domain.Peaks;

namespace PeakService.Ingestion.RunPeakIngestion;

internal static class PeakIngestionMappings
{
    public static Result<IngestedPeak> ToIngestedPeak(this PeakSourceRecord record)
    {
        Result<Coordinates> coordinates = Coordinates.Create(record.Latitude, record.Longitude);

        if (coordinates.IsFailure)
        {
            return Result.Failure<IngestedPeak>(coordinates.Error);
        }

        PeakSourceData source = new(
            record.WikidataId,
            record.Name,
            record.AltitudeMeters,
            record.ProminenceMeters,
            coordinates.Value,
            record.CountryCode,
            record.Region,
            record.SourceRevision,
            record.ImageUrl);

        return new IngestedPeak(source, record.AlternativeNames);
    }

    public static PeakIngestionRunResponse ToResponse(this PeakIngestionRun run) => new(
        run.Id,
        run.Status.ToString(),
        run.PeaksCreated,
        run.PeaksUpdated,
        run.PeaksFailed,
        run.StartedAtUtc,
        run.FinishedAtUtc,
        run.Error);
}
