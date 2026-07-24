using Common.Application.Pagination;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using NpgsqlTypes;
using PeakService.Application.Abstractions;
using PeakService.Application.Peaks;
using PeakService.Application.Peaks.ListPeaks;
using PeakService.Application.Peaks.NearbyPeaks;
using PeakService.Domain.Peaks;
using PeakService.Infrastructure.Persistence.ReadModels;

namespace PeakService.Infrastructure.Persistence.Repositories;

internal sealed class PeakCatalogReader(PeakDbContext context) : IPeakCatalogReader
{
    private const string SearchSql =
        """
        SELECT p.id AS "Id",
               p.name AS "Name",
               p.altitude_m AS "AltitudeMeters",
               p.prominence_m AS "ProminenceMeters",
               ST_Y(p.location::geometry) AS "Latitude",
               ST_X(p.location::geometry) AS "Longitude",
               p.country_code AS "CountryCode",
               p.region AS "Region",
               count(*) OVER() AS "TotalCount"
        FROM peaks p, plainto_tsquery('simple', immutable_unaccent(@q)) AS q(query)
        WHERE (p.search_vector @@ q.query
               OR EXISTS (SELECT 1 FROM peak_names pn
                          WHERE pn.peak_id = p.id
                            AND to_tsvector('simple', immutable_unaccent(pn.name)) @@ q.query))
          AND (@country IS NULL OR p.country_code = @country)
          AND (@region IS NULL OR p.region ILIKE @region)
          AND (@min_alt IS NULL OR p.altitude_m >= @min_alt)
          AND (@max_alt IS NULL OR p.altitude_m <= @max_alt)
        ORDER BY ts_rank(p.search_vector, q.query) DESC, p.name ASC, p.id ASC
        OFFSET @skip LIMIT @size
        """;

    private const string NearbySql =
        """
        WITH origin AS (
            SELECT ST_SetSRID(ST_MakePoint(@lon, @lat), 4326)::geography AS g
        )
        SELECT p.id AS "Id",
               p.name AS "Name",
               p.altitude_m AS "AltitudeMeters",
               ST_Y(p.location::geometry) AS "Latitude",
               ST_X(p.location::geometry) AS "Longitude",
               p.country_code AS "CountryCode",
               p.region AS "Region",
               ST_Distance(p.location, origin.g) AS "DistanceMeters",
               count(*) OVER() AS "TotalCount"
        FROM peaks p, origin
        WHERE ST_DWithin(p.location, origin.g, @radius)
        ORDER BY p.location <-> origin.g
        OFFSET @skip LIMIT @size
        """;

    public async Task<PagedResult<PeakListItemResponse>> ListAsync(
        PeakFilter filter,
        PageRequest page,
        CancellationToken cancellationToken)
    {
        IQueryable<Peak> query = ApplyFilter(context.Peaks.AsNoTracking(), filter);

        int total = await query.CountAsync(cancellationToken);
        List<Peak> peaks = await query
            .OrderBy(peak => peak.Name)
            .ThenBy(peak => peak.Id)
            .Skip(page.Skip)
            .Take(page.Size)
            .ToListAsync(cancellationToken);

        return new PagedResult<PeakListItemResponse>([.. peaks.Select(ToListItem)], page.Page, page.Size, total);
    }

    public async Task<PagedResult<PeakListItemResponse>> SearchAsync(
        string query,
        PeakFilter filter,
        PageRequest page,
        CancellationToken cancellationToken)
    {
        List<PeakSearchRow> rows = await context.Database
            .SqlQueryRaw<PeakSearchRow>(SearchSql, SearchParameters(query, filter, page))
            .ToListAsync(cancellationToken);

        int total = rows.Count == 0 ? 0 : (int)rows[0].TotalCount;

        return new PagedResult<PeakListItemResponse>([.. rows.Select(ToListItem)], page.Page, page.Size, total);
    }

    public async Task<PagedResult<NearbyPeakResponse>> NearbyAsync(
        Coordinates origin,
        double radiusMeters,
        PageRequest page,
        CancellationToken cancellationToken)
    {
        List<PeakNearbyRow> rows = await context.Database
            .SqlQueryRaw<PeakNearbyRow>(NearbySql, NearbyParameters(origin, radiusMeters, page))
            .ToListAsync(cancellationToken);

        int total = rows.Count == 0 ? 0 : (int)rows[0].TotalCount;

        return new PagedResult<NearbyPeakResponse>([.. rows.Select(ToNearby)], page.Page, page.Size, total);
    }

    private static IQueryable<Peak> ApplyFilter(IQueryable<Peak> query, PeakFilter filter)
    {
        if (!string.IsNullOrWhiteSpace(filter.CountryCode))
        {
            string country = filter.CountryCode.Trim().ToUpperInvariant();
            query = query.Where(peak => peak.CountryCode == country);
        }

        if (!string.IsNullOrWhiteSpace(filter.Region))
        {
            string region = filter.Region;
            query = query.Where(peak => peak.Region != null && EF.Functions.ILike(peak.Region, region));
        }

        if (filter.MinAltitudeMeters is { } min)
        {
            query = query.Where(peak => peak.AltitudeMeters >= min);
        }

        return filter.MaxAltitudeMeters is { } max
            ? query.Where(peak => peak.AltitudeMeters <= max)
            : query;
    }

    private static NpgsqlParameter[] SearchParameters(string query, PeakFilter filter, PageRequest page) =>
    [
        new("q", NpgsqlDbType.Text) { Value = query },
        new("country", NpgsqlDbType.Text) { Value = NormalizeCountry(filter.CountryCode) },
        new("region", NpgsqlDbType.Text) { Value = NullableToDb(filter.Region) },
        new("min_alt", NpgsqlDbType.Integer) { Value = NullableToDb(filter.MinAltitudeMeters) },
        new("max_alt", NpgsqlDbType.Integer) { Value = NullableToDb(filter.MaxAltitudeMeters) },
        new("skip", NpgsqlDbType.Integer) { Value = page.Skip },
        new("size", NpgsqlDbType.Integer) { Value = page.Size }
    ];

    private static NpgsqlParameter[] NearbyParameters(Coordinates origin, double radiusMeters, PageRequest page) =>
    [
        new("lon", NpgsqlDbType.Double) { Value = origin.Longitude },
        new("lat", NpgsqlDbType.Double) { Value = origin.Latitude },
        new("radius", NpgsqlDbType.Double) { Value = radiusMeters },
        new("skip", NpgsqlDbType.Integer) { Value = page.Skip },
        new("size", NpgsqlDbType.Integer) { Value = page.Size }
    ];

    private static object NormalizeCountry(string? country) =>
        string.IsNullOrWhiteSpace(country) ? DBNull.Value : country.Trim().ToUpperInvariant();

    private static object NullableToDb(int? value) => value.HasValue ? value.Value : DBNull.Value;

    private static object NullableToDb(string? value) => value ?? (object)DBNull.Value;

    private static PeakListItemResponse ToListItem(Peak peak) => new(
        peak.Id,
        peak.Name,
        peak.AltitudeMeters,
        peak.ProminenceMeters,
        peak.Coordinates.Latitude,
        peak.Coordinates.Longitude,
        peak.CountryCode,
        peak.Region);

    private static PeakListItemResponse ToListItem(PeakSearchRow row) => new(
        row.Id,
        row.Name,
        row.AltitudeMeters,
        row.ProminenceMeters,
        row.Latitude,
        row.Longitude,
        row.CountryCode,
        row.Region);

    private static NearbyPeakResponse ToNearby(PeakNearbyRow row) => new(
        row.Id,
        row.Name,
        row.AltitudeMeters,
        row.Latitude,
        row.Longitude,
        row.CountryCode,
        row.Region,
        row.DistanceMeters);
}
