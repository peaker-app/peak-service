using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using PeakService.Domain.Peaks;
using Point = NetTopologySuite.Geometries.Point;

namespace PeakService.Infrastructure.Persistence;

internal sealed class CoordinatesConverter : ValueConverter<Coordinates, Point>
{
    private const int Wgs84Srid = 4326;

    public CoordinatesConverter()
        : base(
            coordinates => new Point(coordinates.Longitude, coordinates.Latitude) { SRID = Wgs84Srid },
            point => Coordinates.Create(point.Y, point.X).Value)
    {
    }
}
