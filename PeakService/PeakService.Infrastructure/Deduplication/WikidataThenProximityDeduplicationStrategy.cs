using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Npgsql;
using NpgsqlTypes;
using PeakService.Application.Abstractions;
using PeakService.Domain.Peaks;
using PeakService.Infrastructure.ExternalServices;
using PeakService.Infrastructure.Persistence;

namespace PeakService.Infrastructure.Deduplication;

internal sealed class WikidataThenProximityDeduplicationStrategy(
    PeakDbContext context,
    IPeakRepository peakRepository,
    IOptions<IngestionOptions> options) : IPeakDeduplicationStrategy
{
    private const string NearDuplicateSql =
        """
        WITH candidate AS (
            SELECT ST_SetSRID(ST_MakePoint(@lon, @lat), 4326)::geography AS g
        )
        SELECT p.id AS "Value"
        FROM peaks p, candidate
        WHERE ST_DWithin(p.location, candidate.g, @radius)
          AND similarity(immutable_unaccent(p.name), immutable_unaccent(@name)) >= @threshold
        ORDER BY p.location <-> candidate.g
        LIMIT 1
        """;

    public async Task<PeakMatch?> FindMatchAsync(PeakSourceData candidate, CancellationToken cancellationToken)
    {
        Peak? sameSource = await peakRepository.GetByWikidataIdAsync(candidate.WikidataId, cancellationToken);

        if (sameSource is not null)
        {
            return PeakMatch.SameSource(sameSource);
        }

        Guid nearDuplicateId = await FindNearDuplicateIdAsync(candidate, cancellationToken);

        if (nearDuplicateId == Guid.Empty)
        {
            return null;
        }

        Peak? nearDuplicate = await peakRepository.GetByIdAsync(nearDuplicateId, cancellationToken);

        return nearDuplicate is null ? null : PeakMatch.NearDuplicate(nearDuplicate);
    }

    private Task<Guid> FindNearDuplicateIdAsync(PeakSourceData candidate, CancellationToken cancellationToken) =>
        context.Database
            .SqlQueryRaw<Guid>(NearDuplicateSql, Parameters(candidate))
            .FirstOrDefaultAsync(cancellationToken);

    private NpgsqlParameter[] Parameters(PeakSourceData candidate) =>
    [
        new("lon", NpgsqlDbType.Double) { Value = candidate.Coordinates.Longitude },
        new("lat", NpgsqlDbType.Double) { Value = candidate.Coordinates.Latitude },
        new("radius", NpgsqlDbType.Double) { Value = options.Value.DuplicateRadiusMeters },
        new("name", NpgsqlDbType.Text) { Value = candidate.Name },
        new("threshold", NpgsqlDbType.Double) { Value = options.Value.DuplicateNameSimilarity }
    ];
}
