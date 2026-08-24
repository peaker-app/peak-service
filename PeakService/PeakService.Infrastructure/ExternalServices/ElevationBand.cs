using System.Globalization;

namespace PeakService.Infrastructure.ExternalServices;

internal sealed record ElevationBand(double? From, double? To)
{
    public static ElevationBand Unbounded { get; } = new(From: null, To: null);

    public bool IsUnbounded => From is null && To is null;

    public bool CanSplit(double minimumWidth) => From is { } from && To is { } to && to - from > minimumWidth;

    public (ElevationBand Lower, ElevationBand Upper) Split()
    {
        double midpoint = From!.Value + ((To!.Value - From.Value) / 2);

        return (new ElevationBand(From, midpoint), new ElevationBand(midpoint, To));
    }

    public string FilterClause() => (From, To) switch
    {
        (null, null) => string.Empty,
        (null, { } to) => $"FILTER(?elevation < {Format(to)})",
        ({ } from, null) => $"FILTER(?elevation >= {Format(from)})",
        ({ } from, { } to) => $"FILTER(?elevation >= {Format(from)} && ?elevation < {Format(to)})"
    };

    public override string ToString() => (From, To) switch
    {
        (null, null) => "(-inf, +inf)",
        (null, { } to) => $"(-inf, {Format(to)})",
        ({ } from, null) => $"[{Format(from)}, +inf)",
        ({ } from, { } to) => $"[{Format(from)}, {Format(to)})"
    };

    private static string Format(double value) => value.ToString("0.###", CultureInfo.InvariantCulture);
}
