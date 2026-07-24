using System.Net.Http.Json;
using System.Text.Json;

namespace PeakService.IntegrationTests;

internal static class JsonHttp
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    public static Task<T?> ReadAsync<T>(this HttpResponseMessage response) =>
        response.Content.ReadFromJsonAsync<T>(Options);
}
