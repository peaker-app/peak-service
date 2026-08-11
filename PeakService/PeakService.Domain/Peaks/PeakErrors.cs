using Common.Domain.Results;

namespace PeakService.Domain.Peaks;

public static class PeakErrors
{
    public static readonly Error WikidataIdInvalid = Error.Validation(
        "Peak.WikidataIdInvalid",
        $"El identificador de Wikidata es obligatorio y no puede superar los {Peak.MaxWikidataIdLength} caracteres.");

    public static readonly Error NameInvalid = Error.Validation(
        "Peak.NameInvalid",
        $"El nombre del pico es obligatorio y no puede superar los {Peak.MaxNameLength} caracteres.");

    public static readonly Error RegionTooLong = Error.Validation(
        "Peak.RegionTooLong",
        $"La región no puede superar los {Peak.MaxRegionLength} caracteres.");

    public static readonly Error SourceRevisionTooLong = Error.Validation(
        "Peak.SourceRevisionTooLong",
        $"La revisión de origen no puede superar los {Peak.MaxSourceRevisionLength} caracteres.");

    public static readonly Error ImageUrlTooLong = Error.Validation(
        "Peak.ImageUrlTooLong",
        $"La URL de la imagen no puede superar los {Peak.MaxImageUrlLength} caracteres.");

    public static readonly Error LatitudeOutOfRange = Error.Validation(
        "Peak.LatitudeOutOfRange",
        "La latitud debe estar entre -90 y 90 grados.");

    public static readonly Error LongitudeOutOfRange = Error.Validation(
        "Peak.LongitudeOutOfRange",
        "La longitud debe estar entre -180 y 180 grados.");

    public static readonly Error RadiusOutOfRange = Error.Validation(
        "Peak.RadiusOutOfRange",
        $"El radio de búsqueda debe ser mayor que 0 y no superar los {NearbySearchLimits.MaxRadiusMeters:0} metros.");

    public static readonly Error AltitudeRangeInvalid = Error.Validation(
        "Peak.AltitudeRangeInvalid",
        "La altitud mínima no puede ser mayor que la altitud máxima.");

    public static readonly Error CountryCodeInvalid = Error.Validation(
        "Peak.CountryCodeInvalid",
        "El código de país debe ser un código ISO 3166-1 alpha-2 de dos letras.");

    public static Error NotFound(Guid peakId) =>
        Error.NotFound("Peak.NotFound", $"No existe el pico {peakId}.");
}
