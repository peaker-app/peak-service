using System.Net;
using System.Net.Http.Json;
using Polly.CircuitBreaker;
using Polly.Timeout;

namespace PeakService.Infrastructure.ExternalServices;

internal sealed class SparqlEndpoint(HttpClient httpClient)
{
    private const int ErrorExcerptLength = 500;

    public async Task<SparqlOutcome> ExecuteAsync(string query, CancellationToken cancellationToken)
    {
        try
        {
            return await SendAsync(query, cancellationToken);
        }
        catch (Exception exception) when (IsTransient(exception, cancellationToken))
        {
            return SparqlOutcome.Failed($"{exception.GetType().Name}: {exception.Message}", isRetriable: true);
        }
    }

    private async Task<SparqlOutcome> SendAsync(string query, CancellationToken cancellationToken)
    {
        using FormUrlEncodedContent content = new([new KeyValuePair<string, string>("query", query)]);
        using HttpResponseMessage response = await httpClient.PostAsync((Uri?)null, content, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return SparqlOutcome.Failed(
                $"HTTP {(int)response.StatusCode}: {await ReadExcerptAsync(response, cancellationToken)}",
                IsRetriableStatus(response.StatusCode));
        }

        SparqlResponse? payload = await response.Content.ReadFromJsonAsync<SparqlResponse>(cancellationToken);

        return SparqlOutcome.Succeeded(payload?.Results?.Bindings ?? []);
    }

    private static bool IsTransient(Exception exception, CancellationToken cancellationToken) =>
        !cancellationToken.IsCancellationRequested
        && exception is HttpRequestException or TimeoutRejectedException or BrokenCircuitException or TaskCanceledException;

    private static bool IsRetriableStatus(HttpStatusCode statusCode) =>
        statusCode is HttpStatusCode.TooManyRequests or HttpStatusCode.RequestTimeout
            or >= HttpStatusCode.InternalServerError;

    private static async Task<string> ReadExcerptAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        string body = await response.Content.ReadAsStringAsync(cancellationToken);

        return body.Length <= ErrorExcerptLength ? body : body[..ErrorExcerptLength];
    }
}

internal sealed record SparqlOutcome(
    IReadOnlyList<Dictionary<string, SparqlBinding>> Bindings,
    string? FailureReason,
    bool IsRetriable)
{
    public bool IsFailed => FailureReason is not null;

    public static SparqlOutcome Succeeded(IReadOnlyList<Dictionary<string, SparqlBinding>> bindings) =>
        new(bindings, FailureReason: null, IsRetriable: false);

    public static SparqlOutcome Failed(string reason, bool isRetriable) => new([], reason, isRetriable);
}
