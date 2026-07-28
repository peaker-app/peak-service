using System.Net;
using System.Net.Http.Headers;

namespace PeakService.IntegrationTests.Fakes;

internal sealed class StubHttpMessageHandler(params StubbedResponse[] responses) : HttpMessageHandler
{
    private const string EmptyResults = """{"results":{"bindings":[]}}""";

    private int _calls;

    public List<string> ReceivedQueries { get; } = [];

    public List<string?> ReceivedUserAgents { get; } = [];

    public static StubHttpMessageHandler WithBodies(params string[] bodies) =>
        new([.. bodies.Select(StubbedResponse.Ok)]);

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        ReceivedUserAgents.Add(request.Headers.UserAgent.ToString());
        ReceivedQueries.Add(request.Content is null
            ? string.Empty
            : await request.Content.ReadAsStringAsync(cancellationToken));

        StubbedResponse stubbed = _calls < responses.Length
            ? responses[_calls]
            : StubbedResponse.Ok(EmptyResults);

        _calls++;

        HttpResponseMessage response = new(stubbed.StatusCode)
        {
            Content = new StringContent(stubbed.Body)
        };
        response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/sparql-results+json");

        return response;
    }
}

internal sealed record StubbedResponse(HttpStatusCode StatusCode, string Body)
{
    public static StubbedResponse Ok(string body) => new(HttpStatusCode.OK, body);

    public static StubbedResponse Status(HttpStatusCode statusCode) => new(statusCode, string.Empty);
}
