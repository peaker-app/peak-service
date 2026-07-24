using Common.Domain.Results;

namespace PeakService.Domain.MountainRanges;

public static class MountainRangeErrors
{
    public static readonly Error NameInvalid = Error.Validation(
        "MountainRange.NameInvalid",
        $"El nombre de la cordillera es obligatorio y no puede superar los {MountainRange.MaxNameLength} caracteres.");
}
