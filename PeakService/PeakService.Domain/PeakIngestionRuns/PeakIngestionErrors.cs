using Common.Domain.Results;

namespace PeakService.Domain.PeakIngestionRuns;

public static class PeakIngestionErrors
{
    public static readonly Error RunAlreadyFinished = Error.Conflict(
        "PeakIngestionRun.AlreadyFinished",
        "La ejecución de ingesta ya ha finalizado y no admite más cambios.");
}
