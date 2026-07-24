using System.Net;
using System.Net.Http.Headers;

namespace PeakService.IntegrationTests.Fakes;

internal sealed class StubHttpMessageHandler(params string[] responses) : HttpMessageHandler
{
    private int _calls;

    public List<string> ReceivedQueries { get; } = [];

    public List<string?> ReceivedUserAgents { get; } = [];

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        ReceivedUserAgents.Add(request.Headers.UserAgent.ToString());
        ReceivedQueries.Add(request.Content is null
            ? string.Empty
            : await request.Content.ReadAsStringAsync(cancellationToken));

        string body = _calls < responses.Length ? responses[_calls] : EmptyResults;
        _calls++;

        HttpResponseMessage response = new(HttpStatusCode.OK)
        {
            Content = new StringContent(body)
        };
        response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/sparql-results+json");

        return response;
    }

    private const string EmptyResults = """{"results":{"bindings":[]}}""";
}
