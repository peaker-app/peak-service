using Common.API.Caching;

namespace PeakService.API;

internal static class CatalogCache
{
    public static readonly CatalogCacheOptions Policy =
        new(TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(5));
}
