using System.Net;
using System.Text;

namespace VehicleExplorer.Tests.Fakes;

/// <summary>Stands in for the network: answers every request with the given response and records the URLs.</summary>
internal sealed class StubHttpMessageHandler(Func<HttpResponseMessage> createResponse) : HttpMessageHandler
{
    public List<Uri> RequestUris { get; } = [];

    public static StubHttpMessageHandler RespondingWithJson(string json) =>
        new(() => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json"),
        });

    public static StubHttpMessageHandler RespondingWithStatus(HttpStatusCode statusCode) =>
        new(() => new HttpResponseMessage(statusCode));

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        RequestUris.Add(request.RequestUri!);
        return Task.FromResult(createResponse());
    }
}
